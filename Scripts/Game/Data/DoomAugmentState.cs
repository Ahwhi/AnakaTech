using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 판 동안 고른 증강의 누적 상태. 멸망전이 끝나면 버려지는 값이라 서버에 남기지 않는다.
///
/// 중첩은 전부 더하기다. 배수 증강은 "배수 - 1" 을 쌓으므로 1.3배와 1.5배를 먹으면 0.3 + 0.5 = 0.8,
/// 즉 최종 1.8배가 된다(기획 의도 그대로). 곱으로 쌓으면 같은 증강을 몇 번 먹었느냐로 값이 폭주한다.
///
/// 스탯에 얹는 계산은 변신 버프와 같은 식을 쓴다. 배수 스탯 중 공속/이속/스킬가속은 "1 + 스탯/100" 이라
/// 스탯 값을 N배 해서는 실효값이 N배가 되지 않으므로, 실효값이 정확히 N배가 되도록 변환한다.
/// </summary>
public sealed class DoomAugmentState {
    // 종류별 누적치. 배수 증강은 "배수 - 1" 의 합, 가산 증강은 값의 합이다.
    private readonly Dictionary<DoomAugmentKind, float> _totals = new();

    // 고른 순서. 화면에 "지금 무엇을 먹었나"를 줄 단위로 보여 줄 때 쓴다.
    private readonly List<DoomAugmentKind> _order = new();

    /// <summary>고른 증강 종류(고른 순서).</summary>
    public IReadOnlyList<DoomAugmentKind> Order => _order;

    /// <summary>
    /// 내용이 바뀔 때마다 오르는 번호. 화면이 "다시 그려야 하는가"를 이 값으로만 판단하면 되므로
    /// 매 프레임 누적치를 비교하거나 문자열을 새로 만들 필요가 없다.
    /// 같은 종류를 또 먹어 줄 수는 그대로이고 수치만 오르는 경우까지 잡힌다.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>종류별 누적치(배수 증강은 "배수 - 1").</summary>
    public float TotalOf(DoomAugmentKind kind) => _totals.TryGetValue(kind, out var v) ? v : 0f;

    /// <summary>이 종류가 더 이상 값을 하지 않는지(상한 도달). 상한이 없는 종류는 항상 false.</summary>
    public bool IsCapped(DoomAugmentKind kind) {
        var def = DoomAugmentRules.Get(kind);
        return def.Cap > 0f && TotalOf(kind) >= def.Cap;
    }

    public void Add(DoomAugmentKind kind, float value) {
        var def = DoomAugmentRules.Get(kind);
        // 배수는 "1.5배" 가 아니라 "+0.5" 로 쌓아야 여러 번 먹었을 때 더하기가 성립한다.
        float amount = def.Mode == DoomAugmentMode.Multiply ? value - 1f : value;

        if (!_totals.ContainsKey(kind)) _order.Add(kind);
        float next = TotalOf(kind) + amount;
        _totals[kind] = def.Cap > 0f ? Mathf.Min(next, def.Cap) : next;
        Version++;
    }

    public void Clear() {
        _totals.Clear();
        _order.Clear();
        Version++;
    }

    /// <summary>
    /// 기준 스탯 위에 지금까지의 증강을 얹은 결과. 항상 기준 스탯에서 다시 계산하므로
    /// 여러 번 불러도 중첩되지 않는다(변신 버프가 쓰는 방식과 같다).
    /// </summary>
    public CombatStats Apply(CombatStats baseStats) {
        if (_totals.Count == 0) return baseStats;

        var stats = baseStats;
        foreach (var kv in _totals) {
            var def = DoomAugmentRules.Get(kv.Key);

            // 즉시 효과(회복/부활)는 고른 순간 무대가 처리했다. 누적치는 표시용 기록일 뿐이라
            // 스탯에 얹으면 체력이 먹은 횟수만큼 불어나는 엉뚱한 일이 벌어진다.
            if (def.Mode == DoomAugmentMode.Instant) continue;

            if (def.Mode == DoomAugmentMode.Add) {
                stats = stats.WithAdded(def.Stat, kv.Value);
                continue;
            }

            // 배수: 기준값을 보고 "실효 N배" 가 되도록 더할 양을 구한다.
            float baseValue = baseStats.Get(def.Stat);
            // 변신 버프와 같은 식(CombatStats.EffectiveScaled)을 써야 같은 "N배" 가 스킬과 증강에서 똑같이 동작한다.
            float target = CombatStats.EffectiveScaled(def.Stat, baseValue, 1f + kv.Value);
            stats = stats.WithAdded(def.Stat, target - baseValue);
        }
        return stats;
    }
}
