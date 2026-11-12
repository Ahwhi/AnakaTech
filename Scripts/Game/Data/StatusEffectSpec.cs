using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 상태이상 종류. 값이 인스펙터에 직렬화되므로 순서를 바꾸지 말고 새 종류는 항상 아래에 추가한다.
/// </summary>
public enum StatusEffectType {
    /// <summary>기절. 지속시간 동안 이동도 공격도 하지 못한다.</summary>
    Stun = 0,
    /// <summary>출혈. 지속시간 동안 일정 간격으로 시전자의 공격력에 비례한 피해를 입는다.</summary>
    Bleed = 1,
    /// <summary>냉각. 지속시간 동안 스킬 쿨타임이 흐르지 않는다(스킬 발동이 그만큼 밀린다).</summary>
    Chill = 2,
    /// <summary>취약. 지속시간 동안 받는 피해가 배수만큼 늘어난다.</summary>
    Vulnerable = 3,
}

/// <summary>
/// 공격/스킬이 대상에게 거는 상태이상 1건의 설정. 기본공격 SO(BasicAttack)와 스킬 SO(UnitSkill)의
/// 인스펙터에 배열로 붙어 있어, 원하는 공격/스킬에만 원하는 상태이상을 달 수 있다.
///
/// 지속시간은 대상의 상태이상저항(CombatStats.StatusResist)으로 점근 감쇄된다(CombatMath.StatusDuration).
/// 저항이 아무리 높아도 0 이 되지는 않으므로, 저항 스탯이 상태이상을 무력화하지 못한다.
///
/// Value 의 의미는 종류마다 다르다. 종류별로 필드를 따로 두면 인스펙터에 쓰지 않는 칸이 잔뜩 생기므로
/// 하나로 두고 아래 프로퍼티로 의미를 붙인다(툴팁에 종류별 의미를 적어 둔다).
/// </summary>
[Serializable]
public struct StatusEffectSpec {
    [Tooltip("걸 상태이상 종류.")]
    public StatusEffectType Type;

    [Tooltip("지속시간(초). 대상의 상태이상저항으로 줄어든다(0 이 되지는 않는다).")]
    public float Duration;

    [Tooltip("적중 시 걸릴 확률(0~1). 0 이면 1(항상)로 본다.")]
    [Range(0f, 1f)]
    public float Chance;

    [Tooltip("종류별 수치. 출혈 = 틱당 시전자 공격력 배수(0.1 = 10%), 취약 = 받는 피해 배수(1.5 = 1.5배). " +
             "기절/냉각은 쓰지 않는다. 0 이면 종류별 기본값을 쓴다.")]
    public float Value;

    [Tooltip("출혈 전용: 피해가 들어가는 간격(초). 0 이면 기본값 1초.")]
    public float TickInterval;

    // 종류별 기본값. 인스펙터를 비워 둔 채로도 뜻이 통하는 상태이상이 되도록 한다.
    private const float DefaultBleedRatio = 0.1f;      // 시전자 공격력의 10%
    // 받는 피해 1.5배. 2배는 취약 하나로 전투가 결정될 만큼 세서 다른 상태이상이 설 자리가 없어진다.
    private const float DefaultVulnerableScale = 1.5f;
    private const float DefaultTickInterval = 1f;

    /// <summary>이번 적중에서 실제로 걸리는지(확률 판정).</summary>
    public bool Roll() => Chance <= 0f || UnityEngine.Random.value < Chance;

    /// <summary>출혈 틱당 피해 배수(시전자 공격력 대비).</summary>
    public float BleedRatio => Value > 0f ? Value : DefaultBleedRatio;

    /// <summary>취약으로 늘어나는 받는 피해 배수.</summary>
    public float VulnerableScale => Value > 0f ? Value : DefaultVulnerableScale;

    /// <summary>출혈 피해 간격(초).</summary>
    public float Interval => TickInterval > 0f ? TickInterval : DefaultTickInterval;

    /// <summary>설정으로서 의미가 있는지(지속시간이 없으면 걸 것이 없다).</summary>
    public bool IsValid => Duration > 0f;
}

/// <summary>
/// 상태이상 종류별 지속 표시 VFX 배선(BattleSettings 에 배열로 둔다).
/// 유닛에 붙어 상태이상이 끝날 때 함께 사라지므로, 프리팹은 스스로 꺼지지 않는 지속형이어야 한다.
/// </summary>
[Serializable]
public struct StatusEffectVfx {
    public StatusEffectType Type;
    public GameObject Prefab;
}

/// <summary>
/// 상태이상의 표시 규칙(이름 / 색). 스킬 설명의 [취약] 같은 표기와 앞으로 만들 버프 아이콘/툴팁이
/// 같은 이름과 같은 색을 쓰도록 한 곳에 모아 둔다.
///
/// 새 상태이상을 추가하면 여기 두 줄만 늘리면 되고, 설명 문구는 원문에 [이름] 을 적기만 하면 색이 붙는다.
/// </summary>
public static class StatusEffectRules {
    /// <summary>스킬 설명/툴팁에 쓰는 표시 이름. 설명 원문의 [이름] 표기와 일치해야 색이 붙는다.</summary>
    public static string DisplayName(StatusEffectType type) => type switch {
        StatusEffectType.Stun => "스턴",
        StatusEffectType.Bleed => "출혈",
        StatusEffectType.Chill => "냉각",
        StatusEffectType.Vulnerable => "취약",
        _ => ""
    };

    /// <summary>표시 색(TMP color 태그용 16진 코드).</summary>
    public static string ColorHex(StatusEffectType type) => type switch {
        StatusEffectType.Stun => "#C9A227",         // 노랑 - 행동 정지
        StatusEffectType.Bleed => "#C0392B",        // 붉은색 - 지속 피해
        StatusEffectType.Chill => "#2E86C1",        // 파랑 - 냉각
        StatusEffectType.Vulnerable => "#8E44AD",   // 보라 - 받는 피해 증가
        _ => ""
    };

    /// <summary>
    /// 상태이상 설명(설명 팝업용). 수치는 스킬마다 다르므로 여기서는 효과의 성격만 적고,
    /// 구체적인 수치는 스킬 설명 본문이 맡는다.
    /// </summary>
    public static string Description(StatusEffectType type) => type switch {
        StatusEffectType.Stun => "지속시간 동안 이동도 공격도 하지 못합니다.",
        StatusEffectType.Bleed => "지속시간 동안 일정 간격으로 시전자의 공격력에 비례한 피해를 입습니다.",
        StatusEffectType.Chill => "지속시간 동안 스킬 쿨타임이 흐르지 않아 스킬 발동이 그만큼 늦어집니다.",
        StatusEffectType.Vulnerable => "지속시간 동안 받는 피해가 늘어납니다.",
        _ => ""
    };

    /// <summary>
    /// 상태이상 목록을 설명 팝업 본문으로 만든다(색 입힌 이름 + 설명, 종류별 1회).
    /// 같은 종류가 여러 번 걸려도(예: 스킬과 변신 공격이 둘 다 출혈) 한 번만 적는다. 없으면 빈 문자열이다.
    /// </summary>
    public static string DescribeAll(IEnumerable<StatusEffectSpec> effects) {
        if (effects == null) return "";

        var seen = new HashSet<StatusEffectType>();
        var sb = new StringBuilder();
        foreach (var e in effects) {
            if (!seen.Add(e.Type)) continue;
            string desc = Description(e.Type);
            if (string.IsNullOrEmpty(desc)) continue;

            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append($"<color={ColorHex(e.Type)}>[{DisplayName(e.Type)}]</color>\n{desc}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 표시 이름으로 상태이상을 찾는다(스킬 설명의 [이름] 해석용).
    /// "기절" 처럼 같은 뜻으로 자주 쓰는 말도 함께 받아 준다.
    /// </summary>
    public static bool TryFind(string displayName, out StatusEffectType type) {
        string name = (displayName ?? "").Trim();
        foreach (StatusEffectType t in System.Enum.GetValues(typeof(StatusEffectType))) {
            if (name == DisplayName(t)) { type = t; return true; }
        }
        if (name == "기절") { type = StatusEffectType.Stun; return true; }

        type = default;
        return false;
    }
}
