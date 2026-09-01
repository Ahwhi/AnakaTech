#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

/// <summary>
/// 메모리 계측기(에디터 전용). 측정, 구간 기록, 자산 보고서를 담당한다.
///
/// 켜져 있을 때만 인스턴스가 만들어지고, 그 인스턴스를 MemoryProbe 가 정적으로 들고 있다.
/// 계측은 게임 코드 어디에서나 한 줄로 남겨야 하므로(생성자 주입으로는 호출 지점이 제한된다)
/// 진단에 한해 정적 접근을 허용한다. 빌드에는 이 파일 전체가 포함되지 않는다.
///
/// 에디터에서는 에디터 프로세스 자신의 메모리가 함께 잡히므로, 판단은 항상 시작 시점 대비 증가분으로 한다.
/// </summary>
public sealed class MemoryDiagnostics {
    // 훑을 자산 종류. Texture 하나로 Texture2D/RenderTexture/Cubemap 이 모두 잡힌다.
    private static readonly Type[] TrackedTypes = {
        typeof(Texture), typeof(Mesh), typeof(AudioClip), typeof(Material), typeof(Shader), typeof(AnimationClip)
    };

    /// <summary>현재 동작 중인 계측기. 꺼져 있으면 null 이다.</summary>
    public static MemoryDiagnostics Active { get; private set; }

    private readonly List<MemoryCheckpoint> _checkpoints = new();
    private long _previousFootprint;

    public MemoryUsage Current { get; private set; }
    public MemoryUsage Peak { get; private set; }
    public MemoryUsage Baseline { get; }

    /// <summary>기준선 대비 현재 증가분. "이 게임이 실제로 쓰고 있는 양"에 해당한다.</summary>
    public long AppFootprint => Current.Footprint - Baseline.Footprint;

    /// <summary>기준선 대비 최대 증가분.</summary>
    public long PeakAppFootprint => Peak.Footprint - Baseline.Footprint;

    public IReadOnlyList<MemoryCheckpoint> Checkpoints => _checkpoints;

    private MemoryDiagnostics() {
        Current = MemoryUsage.Capture();
        Baseline = Current;
        Peak = Current;
    }

    /// <summary>계측을 시작한다. 이미 동작 중이면 그대로 둔다.</summary>
    public static MemoryDiagnostics Begin() => Active ??= new MemoryDiagnostics();

    /// <summary>계측을 멈추고 기록을 버린다.</summary>
    public static void End() => Active = null;

    public MemoryUsage Sample() {
        var usage = MemoryUsage.Capture();
        Current = usage;
        if (usage.Footprint > Peak.Footprint) Peak = usage;
        return usage;
    }

    public void Mark(string label) {
        var usage = Sample();
        long delta = _checkpoints.Count == 0 ? 0 : usage.Footprint - _previousFootprint;
        _previousFootprint = usage.Footprint;

        var checkpoint = new MemoryCheckpoint(label, usage, delta);
        _checkpoints.Add(checkpoint);
        Debug.Log($"[Memory] {checkpoint}");
    }

    public string BuildTimeline() {
        var sb = new StringBuilder();
        sb.AppendLine("[Memory] 구간별 메모리 변화");
        sb.AppendLine("   시각        누적          증감        구간");
        foreach (var checkpoint in _checkpoints)
            sb.AppendLine("  " + checkpoint);
        sb.AppendLine($"  기준선: {DownloadInfo.FormatBytes(Baseline.Footprint)} (에디터 프로세스 자신의 사용량이 포함된다)");
        sb.AppendLine($"  앱 사용량 최대: {DownloadInfo.FormatBytes(PeakAppFootprint)} (기준선 대비, {Peak.Time:0.00}s)");
        return sb.ToString();
    }

    public string BuildAssetReport(int topCount) {
        var entries = new List<AssetEntry>(1024);
        var totals = new Dictionary<string, TypeTotal>();
        long excludedBytes = 0;
        int excludedCount = 0;

        foreach (var type in TrackedTypes) {
            foreach (var obj in Resources.FindObjectsOfTypeAll(type)) {
                long size = Profiler.GetRuntimeMemorySizeLong(obj);

                if (!IsProjectOwned(obj)) {
                    excludedBytes += size;
                    excludedCount++;
                    continue;
                }

                string typeName = obj.GetType().Name;
                totals.TryGetValue(typeName, out var total);
                totals[typeName] = new TypeTotal(total.Count + 1, total.Bytes + size);

                if (size > 0) entries.Add(new AssetEntry(typeName, Describe(obj), size));
            }
        }

        entries.Sort((a, b) => b.Bytes.CompareTo(a.Bytes));

        var sb = new StringBuilder();
        sb.AppendLine("[Memory] 로드된 자산 보고서");
        sb.AppendLine($"  앱 사용량 {DownloadInfo.FormatBytes(AppFootprint)} (기준선 {DownloadInfo.FormatBytes(Baseline.Footprint)} 제외)");
        sb.AppendLine("  " + Current.ToDetailString());
        if (excludedCount > 0)
            sb.AppendLine($"  에디터 소유로 제외: {excludedCount} 개 {DownloadInfo.FormatBytes(excludedBytes)}");

        sb.AppendLine("  종류별 합계:");
        foreach (var pair in SortedByBytes(totals))
            sb.AppendLine($"    {pair.Key,-16} {pair.Value.Count,6} 개  {DownloadInfo.FormatBytes(pair.Value.Bytes),12}");

        sb.AppendLine($"  큰 자산 상위 {topCount}:");
        int limit = Mathf.Min(topCount, entries.Count);
        for (int i = 0; i < limit; i++) {
            var entry = entries[i];
            sb.AppendLine($"    {DownloadInfo.FormatBytes(entry.Bytes),12}  [{entry.TypeName}] {entry.Description}");
        }

        return sb.ToString();
    }

    // 종류별 합계를 큰 것부터 정렬. Dictionary 는 순서를 보장하지 않으므로 보고 직전에 정렬한다.
    private static List<KeyValuePair<string, TypeTotal>> SortedByBytes(Dictionary<string, TypeTotal> totals) {
        var list = new List<KeyValuePair<string, TypeTotal>>(totals);
        list.Sort((a, b) => b.Value.Bytes.CompareTo(a.Value.Bytes));
        return list;
    }

    // 이 프로젝트의 자산인지. 에디터에서는 기즈모 아이콘, 에디터 UI 폰트, 에디터 뷰 렌더 타겟까지
    // 함께 잡혀 보고서가 무의미해지므로 걸러낸다.
    private static bool IsProjectOwned(Object obj) {
        string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
            return path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal);

        // 경로가 없으면 런타임 생성물이거나 에디터 내부 오브젝트다. 후자는 저장 대상에서 빠져 있다.
        return (obj.hideFlags & HideFlags.DontSaveInEditor) == 0;
    }

    // 자산 식별 문자열. 텍스처는 해상도와 포맷까지 붙여야 어떤 설정이 문제인지 바로 보인다.
    private static string Describe(Object obj) {
        if (obj is Texture2D texture2D)
            return $"{obj.name} {texture2D.width}x{texture2D.height} {texture2D.format} mip={texture2D.mipmapCount}";
        if (obj is RenderTexture renderTexture)
            return $"{obj.name} {renderTexture.width}x{renderTexture.height} {renderTexture.graphicsFormat}";
        if (obj is Texture texture)
            return $"{obj.name} {texture.width}x{texture.height}";
        return obj.name;
    }

    private readonly struct AssetEntry {
        public string TypeName { get; }
        public string Description { get; }
        public long Bytes { get; }

        public AssetEntry(string typeName, string description, long bytes) {
            TypeName = typeName;
            Description = description;
            Bytes = bytes;
        }
    }

    private readonly struct TypeTotal {
        public int Count { get; }
        public long Bytes { get; }

        public TypeTotal(int count, long bytes) {
            Count = count;
            Bytes = bytes;
        }
    }
}
#endif
