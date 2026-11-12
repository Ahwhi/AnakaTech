using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 1명에게 걸려 있는 상태이상 전부를 관리한다. 전투 유닛(BattleUnit)이 하나 들고 있으며,
/// 지속시간/틱/VFX 를 여기서 끝낸다.
///
/// 유닛은 "지금 기절인가 / 받는 피해 배수가 얼마인가"만 물어보면 되고, 상태이상의 종류가 늘어도
/// 유닛 코드는 바뀌지 않는다(질의 함수만 늘어난다).
///
/// 같은 종류를 다시 맞으면 중첩하지 않고 "더 센 쪽"으로 덮어쓴다. 중첩을 허용하면 다구리 상황에서
/// 기절이 끝없이 이어지거나 취약 배수가 폭주해 밸런스를 잡을 수 없다.
/// 남은 시간이 더 긴 쪽을 남기고, 수치가 있는 종류(출혈/취약)는 더 높은 수치를 남긴다.
/// </summary>
public sealed class UnitStatusEffects {
    /// <summary>걸려 있는 상태이상 1건의 현재 상태.</summary>
    private sealed class Active {
        public StatusEffectType Type;
        public float Remaining;
        public float Value;          // 출혈 배수 / 취약 배수
        public float Interval;       // 출혈 틱 간격
        public float TickTimer;      // 다음 틱까지 남은 시간
        public CombatStats Source;   // 출혈 피해 계산에 쓸 시전자 스탯 스냅샷
        public ICombatUnit Caster;   // 딜량 집계에 쓸 시전자(중간에 죽어도 참조만 남는다)
        public GameObject Vfx;       // 지속 표시 VFX(끝나면 파괴)
    }

    private readonly BattleUnit _owner;
    private readonly List<Active> _actives = new();

    // 매 프레임 다시 세지 않도록 상태가 바뀔 때만 갱신하는 요약값.
    private bool _stunned;
    private bool _chilled;
    private float _damageTakenScale = 1f;

    public UnitStatusEffects(BattleUnit owner) => _owner = owner;

    /// <summary>지금 행동할 수 없는지(이동/공격 금지).</summary>
    public bool IsStunned => _stunned;

    /// <summary>지금 스킬 쿨타임이 멈춰 있는지.</summary>
    public bool IsChilled => _chilled;

    /// <summary>받는 피해 배수(취약). 걸린 것이 없으면 1.</summary>
    public float DamageTakenScale => _damageTakenScale;

    /// <summary>하나라도 걸려 있는지.</summary>
    public bool Any => _actives.Count > 0;

    /// <summary>
    /// 상태이상을 건다. 지속시간은 대상의 상태이상저항으로 줄어들며, 0 이하가 되면 걸지 않는다.
    /// </summary>
    /// <param name="source">시전자 스탯(출혈 피해 계산 기준). 시전자가 나중에 죽어도 이 값으로 계산한다.</param>
    public void Apply(in StatusEffectSpec spec, in CombatStats source, ICombatUnit caster, float resistSoftK) {
        if (!spec.IsValid || !spec.Roll()) return;

        // 시전자 쪽 지속시간 보정(무한 타워 층 보너스 등)을 먼저 곱한 뒤 대상의 저항으로 줄인다.
        float scale = caster is BattleUnit castUnit && castUnit != null ? castUnit.InflictDurationScale(spec.Type) : 1f;
        float duration = CombatMath.StatusDuration(spec.Duration * scale, _owner.Stats, resistSoftK);
        if (duration <= 0f) return;

        var existing = Find(spec.Type);
        if (existing != null) {
            // 중첩하지 않고 더 센 쪽을 남긴다(시간은 더 긴 쪽, 수치는 더 높은 쪽).
            existing.Remaining = Mathf.Max(existing.Remaining, duration);
            existing.Value = Mathf.Max(existing.Value, ValueOf(spec));
            existing.Source = source;
            existing.Caster = caster;
            Recalculate();
            return;
        }

        var active = new Active {
            Type = spec.Type,
            Remaining = duration,
            Value = ValueOf(spec),
            Interval = spec.Interval,
            TickTimer = spec.Interval,   // 걸리자마자 한 대 더 맞는 느낌이 나지 않도록 한 박자 뒤부터 시작한다.
            Source = source,
            Caster = caster,
            Vfx = SpawnVfx(spec.Type),
        };
        _actives.Add(active);
        Recalculate();
    }

    /// <summary>시간을 흘린다(유닛의 Update 에서 호출). 끝난 것은 VFX 와 함께 정리된다.</summary>
    public void Tick(float dt) {
        if (_actives.Count == 0) return;

        bool changed = false;
        for (int i = _actives.Count - 1; i >= 0; i--) {
            var a = _actives[i];
            a.Remaining -= dt;

            if (a.Type == StatusEffectType.Bleed) {
                TickBleed(a, dt);
                // 출혈 피해로 대상이 죽으면 Die 가 Clear 를 불러 이 목록이 통째로 비워진다(재진입).
                // 그 상태로 루프를 이어 가면 이미 사라진 항목을 옛 인덱스로 집게 되므로 여기서 끝낸다.
                // Clear 가 요약값까지 맞춰 두므로 아래 Recalculate 도 필요 없다.
                if (_actives.Count == 0) return;
            }

            if (a.Remaining > 0f) continue;

            Destroy(a);
            _actives.RemoveAt(i);
            changed = true;
        }
        if (changed) Recalculate();
    }

    /// <summary>전부 해제한다(사망/부활/전투 재시작). VFX 도 함께 사라진다.</summary>
    public void Clear() {
        for (int i = 0; i < _actives.Count; i++) Destroy(_actives[i]);
        _actives.Clear();
        Recalculate();
    }

    // 출혈: 간격마다 시전자 공격력에 비례한 피해를 준다.
    // 데미지 파이프라인(방어/치명타/최종데미지)을 그대로 통과시키므로 데미지 팝업과 딜량 집계가 저절로 따라온다.
    // 다만 넉백과 회피는 타지 않는다(도트마다 밀려나거나 빗나가면 상태이상이 아니라 또 다른 공격처럼 보인다).
    private void TickBleed(Active a, float dt) {
        a.TickTimer -= dt;
        if (a.TickTimer > 0f) return;

        a.TickTimer += a.Interval;
        _owner.TakeDamageOverTime(a.Source, a.Value, a.Caster);
    }

    private static float ValueOf(in StatusEffectSpec spec) => spec.Type switch {
        StatusEffectType.Bleed => spec.BleedRatio,
        StatusEffectType.Vulnerable => spec.VulnerableScale,
        _ => 0f
    };

    // 요약값을 다시 계산한다. 질의는 매 프레임 여러 번 일어나는 반면 상태 변화는 드물어서,
    // 물어볼 때마다 목록을 훑지 않고 바뀔 때만 갱신한다.
    private void Recalculate() {
        _stunned = false;
        _chilled = false;
        _damageTakenScale = 1f;

        for (int i = 0; i < _actives.Count; i++) {
            var a = _actives[i];
            switch (a.Type) {
                case StatusEffectType.Stun: _stunned = true; break;
                case StatusEffectType.Chill: _chilled = true; break;
                // 취약이 여럿이면 가장 센 것 하나만 적용한다(곱해 쌓이면 순식간에 즉사 배수가 된다).
                case StatusEffectType.Vulnerable: _damageTakenScale = Mathf.Max(_damageTakenScale, a.Value); break;
            }
        }
    }

    private Active Find(StatusEffectType type) {
        for (int i = 0; i < _actives.Count; i++)
            if (_actives[i].Type == type) return _actives[i];
        return null;
    }

    // 지속 표시 VFX. 유닛에 붙여 두어 유닛이 움직이면 따라오고, 상태이상이 끝나면 함께 사라진다.
    // 프리팹은 전투 설정(BattleSettings)이 종류별로 들고 있어 어느 전장에서든 같은 표현이 나온다.
    private GameObject SpawnVfx(StatusEffectType type) {
        var prefab = _owner.StatusVfxPrefab(type);
        if (prefab == null) return null;

        // worldPositionStays: false 로 붙여 프리팹이 들고 있는 위치/회전/크기를 그대로 쓴다.
        // 기본값(true)은 프리팹의 월드 트랜스폼을 유지하려고 로컬 값을 다시 계산하므로,
        // 유닛에 붙는 순간 작업자가 잡아 둔 각도와 오프셋이 어긋난다.
        return Object.Instantiate(prefab, _owner.StatusVfxAnchor, false);
    }

    private static void Destroy(Active a) {
        if (a.Vfx != null) Object.Destroy(a.Vfx);
        a.Vfx = null;
    }
}
