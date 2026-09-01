#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// 한 시점의 메모리 사용량 스냅샷. 값 타입이라 기록/비교/보관에 부담이 없다.
///
/// iOS 의 Jetsam(메모리 강제 종료)은 관리 힙이 아니라 "프로세스 전체 물리 메모리"로 판단한다.
/// 그래서 Mono 힙만 봐서는 원인을 알 수 없고, 엔진 예약량과 그래픽 드라이버 할당을 합친
/// Footprint 를 봐야 실제 종료 기준에 가까운 값이 된다.
///
/// 프로파일러 API 는 에디터와 Development Build 에서만 값을 돌려준다(ENABLE_PROFILER).
/// 그래서 릴리스 빌드에서도 값이 나오는 Texture.currentTextureMemory 와 GC.GetTotalMemory 를
/// 함께 담아, 어떤 빌드에서든 최소한의 판단은 가능하게 한다.
/// </summary>
public readonly struct MemoryUsage : IEquatable<MemoryUsage> {
    /// <summary>엔진이 실제로 쓰고 있는 네이티브 메모리.</summary>
    public long TotalAllocated { get; }

    /// <summary>엔진이 OS 로부터 예약한 네이티브 메모리. 반납이 늦어도 프로세스 점유로 잡힌다.</summary>
    public long TotalReserved { get; }

    /// <summary>그래픽 드라이버 할당(텍스처/버퍼). 모바일에서 가장 크게 튀는 항목이다.</summary>
    public long GraphicsDriver { get; }

    /// <summary>Mono 가 사용 중인 관리 힙.</summary>
    public long MonoUsed { get; }

    /// <summary>Mono 가 예약한 관리 힙.</summary>
    public long MonoHeap { get; }

    /// <summary>GPU 에 올라가 있는 텍스처 메모리. 프로파일러가 없는 릴리스 빌드에서도 읽을 수 있다.</summary>
    public long TextureMemory { get; }

    /// <summary>로드된 모든 텍스처가 요구하는 메모리(밉 스트리밍 미적용 기준).</summary>
    public long DesiredTextureMemory { get; }

    /// <summary>GC 기준 관리 힙. 프로파일러가 없는 릴리스 빌드의 대체 지표.</summary>
    public long ManagedHeap { get; }

    /// <summary>프로파일러 값이 유효한 빌드인지. false 면 Footprint 가 근사치다.</summary>
    public bool ProfilerAvailable { get; }

    /// <summary>측정 시각(앱 시작 후 경과 초).</summary>
    public float Time { get; }

    /// <summary>
    /// Jetsam 판단 기준에 가장 가까운 값. 프로파일러가 있으면 엔진 예약량 + 그래픽 드라이버,
    /// 없으면 텍스처 + 관리 힙으로 근사한다(근사치는 실제보다 작게 나온다).
    /// </summary>
    public long Footprint => ProfilerAvailable ? TotalReserved + GraphicsDriver : TextureMemory + ManagedHeap;

    private MemoryUsage(long totalAllocated, long totalReserved, long graphicsDriver,
                        long monoUsed, long monoHeap, long textureMemory, long desiredTextureMemory,
                        long managedHeap, bool profilerAvailable, float time) {
        TotalAllocated = totalAllocated;
        TotalReserved = totalReserved;
        GraphicsDriver = graphicsDriver;
        MonoUsed = monoUsed;
        MonoHeap = monoHeap;
        TextureMemory = textureMemory;
        DesiredTextureMemory = desiredTextureMemory;
        ManagedHeap = managedHeap;
        ProfilerAvailable = profilerAvailable;
        Time = time;
    }

    /// <summary>지금 이 순간을 측정한다. 매 프레임 불러도 될 만큼 가볍다.</summary>
    public static MemoryUsage Capture() {
        long texture = (long)Texture.currentTextureMemory;
        long desired = (long)Texture.desiredTextureMemory;
        long managed = GC.GetTotalMemory(false);
        float now = UnityEngine.Time.realtimeSinceStartup;

#if ENABLE_PROFILER
        return new MemoryUsage(
            Profiler.GetTotalAllocatedMemoryLong(),
            Profiler.GetTotalReservedMemoryLong(),
            Profiler.GetAllocatedMemoryForGraphicsDriver(),
            Profiler.GetMonoUsedSizeLong(),
            Profiler.GetMonoHeapSizeLong(),
            texture, desired, managed, true, now);
#else
        return new MemoryUsage(0, 0, 0, 0, 0, texture, desired, managed, false, now);
#endif
    }

    // 매 프레임 ReactiveProperty 로 흘려보내므로, 기본 비교자의 박싱을 피하려고 직접 구현한다.
    public bool Equals(MemoryUsage other) =>
        Time.Equals(other.Time) && TotalReserved == other.TotalReserved && GraphicsDriver == other.GraphicsDriver &&
        TextureMemory == other.TextureMemory && ManagedHeap == other.ManagedHeap;

    public override bool Equals(object obj) => obj is MemoryUsage other && Equals(other);

    public override int GetHashCode() => Time.GetHashCode() ^ TotalReserved.GetHashCode() ^ TextureMemory.GetHashCode();

    /// <summary>오버레이 한 줄 요약.</summary>
    public string ToShortString() =>
        $"{DownloadInfo.FormatBytes(Footprint)}  (Tex {DownloadInfo.FormatBytes(TextureMemory)} / Mono {DownloadInfo.FormatBytes(ProfilerAvailable ? MonoUsed : ManagedHeap)})";

    /// <summary>로그용 상세 표기.</summary>
    public string ToDetailString() {
        if (!ProfilerAvailable) {
            return $"Footprint(근사) {DownloadInfo.FormatBytes(Footprint)}" +
                   $" | Texture {DownloadInfo.FormatBytes(TextureMemory)}" +
                   $" | TextureDesired {DownloadInfo.FormatBytes(DesiredTextureMemory)}" +
                   $" | ManagedHeap {DownloadInfo.FormatBytes(ManagedHeap)}" +
                   " | 프로파일러 비활성(Development Build 로 빌드하면 정확한 값이 나옵니다)";
        }

        return $"Footprint {DownloadInfo.FormatBytes(Footprint)}" +
               $" | Reserved {DownloadInfo.FormatBytes(TotalReserved)}" +
               $" | Allocated {DownloadInfo.FormatBytes(TotalAllocated)}" +
               $" | Gfx {DownloadInfo.FormatBytes(GraphicsDriver)}" +
               $" | Texture {DownloadInfo.FormatBytes(TextureMemory)}" +
               $" | Mono {DownloadInfo.FormatBytes(MonoUsed)}/{DownloadInfo.FormatBytes(MonoHeap)}";
    }
}

/// <summary>
/// 이름표가 붙은 측정 지점. 부트 단계/씬 로드/팝업 오픈처럼 "무엇을 하기 전후"를 기록해
/// 어느 구간에서 메모리가 늘었는지 구간별로 귀속시킨다.
/// </summary>
public readonly struct MemoryCheckpoint {
    public string Label { get; }
    public MemoryUsage Usage { get; }

    /// <summary>직전 체크포인트 대비 Footprint 증가량(바이트). 첫 체크포인트는 0.</summary>
    public long Delta { get; }

    public MemoryCheckpoint(string label, MemoryUsage usage, long delta) {
        Label = label;
        Usage = usage;
        Delta = delta;
    }

    public override string ToString() {
        string sign = Delta >= 0 ? "+" : "-";
        return $"{Usage.Time,7:0.00}s  {DownloadInfo.FormatBytes(Usage.Footprint),12}  " +
               $"{sign}{DownloadInfo.FormatBytes(Math.Abs(Delta)),10}  {Label}";
    }
}
#endif
