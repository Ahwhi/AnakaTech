using UnityEngine;

/// <summary>
/// 수련 효과 종류. 값은 서버 Server.Data.TrainingEffect 와 일치해야 한다(패킷/DB 에 int 로 실린다).
/// 어떤 효과가 몇 % 확률로 나오고 수치 범위가 어디까지인지는 서버가 정해 내려주므로(S_TrainingInfo.rate)
/// 클라는 "효과를 스탯으로 해석하는 규칙"과 "표시 문구"만 갖는다.
/// </summary>
public enum TrainingEffect {
    None = -1,
    Range = 0,                  // 사거리 증가
    DoubleAttack = 1,           // 기본공격 2회 발동 확률
    Evasion = 2,                // 피격 회피 확률
    FinalDamage = 3,            // 최종 데미지 증가
    DamageTakenReduction = 4    // 피격 데미지 감소
}

/// <summary>
/// 수련 효과의 해석/표시 규칙(클라 전용).
///
/// 서버는 "효과 종류 + 수치(1/10 단위 정수)"만 저장하고, 그 수치가 어느 스탯을 얼마나 올리는지는 여기서 정한다
/// (장비 포인트를 EquipmentStatLayout 이 해석하는 것과 같은 규약). 덕분에 밸런스 조정이 클라 한 곳에서 끝나고,
/// 내 파티와 대전 상대 파티가 같은 함수를 통과하므로 재현이 어긋나지 않는다.
/// </summary>
public static class TrainingRules {
    /// <summary>서버가 수치를 1/10 단위 정수로 보내는 것에 대응하는 나눗수.</summary>
    public const float TenthScale = 10f;

    /// <summary>수련 효과 1개가 주는 스탯 보정. 유효하지 않은 효과는 0 보정(빈 값)이다.</summary>
    public static CombatStats BonusOf(TrainingEffect effect, int valueTenth) {
        var stats = new CombatStats();
        if (valueTenth <= 0) return stats;

        var stat = StatOf(effect);
        if (stat == null) return stats;
        return stats.WithAdded(stat.Value, valueTenth / TenthScale);
    }

    /// <summary>서버가 보낸 정수 효과값으로 스탯 보정을 만든다(패킷/DTO 를 그대로 넘길 때 사용).</summary>
    public static CombatStats BonusOf(int effect, int valueTenth) => BonusOf((TrainingEffect)effect, valueTenth);

    /// <summary>
    /// 효과 종류가 올리는 스탯. 매핑을 여기 한 곳에만 두어 전투/전투력/표시가 같은 결론을 쓴다.
    /// 알 수 없는 효과(서버가 새 효과를 추가했는데 클라가 아직 모르는 경우)는 null 이라 보정 없이 안전하게 넘어간다.
    /// </summary>
    public static StatType? StatOf(TrainingEffect effect) => effect switch {
        TrainingEffect.Range => StatType.Range,
        TrainingEffect.DoubleAttack => StatType.DoubleAttackChance,
        TrainingEffect.Evasion => StatType.EvasionChance,
        TrainingEffect.FinalDamage => StatType.FinalDamage,
        TrainingEffect.DamageTakenReduction => StatType.DamageTakenReduction,
        _ => null
    };

    // ── 표시 ────────────────────────────────────────
    // 발동형 효과(2회 공격/회피)는 이름에 "확률"을 붙인다. 수치가 증가량이 아니라 발동 확률이라
    // 이름만으로 "2회 공격 30%" 를 보면 무엇이 30% 인지 알 수 없기 때문이다.
    public static string EffectName(TrainingEffect effect) => effect switch {
        TrainingEffect.Range => "사거리",
        TrainingEffect.DoubleAttack => "2회 공격 확률",
        TrainingEffect.Evasion => "회피 확률",
        TrainingEffect.FinalDamage => "최종 데미지",
        TrainingEffect.DamageTakenReduction => "피격 데미지 감소",
        _ => "새로운 효과"
    };

    public static string EffectName(int effect) => EffectName((TrainingEffect)effect);

    /// <summary>
    /// 효과 수치 1개의 표시 문구. 사거리만 거리(소수 1자리)이고 나머지는 백분율이다.
    ///
    /// 다섯 효과 모두 "그 스탯에 이만큼 더한다"는 같은 성질이라 + 를 붙인다. 2회 공격/회피도 기본값이 0인
    /// 스탯에 더하는 것이라 예외가 아니다(수련 전에는 0%, 수련으로 +30% 면 30%). 어떤 효과는 + 가 붙고
    /// 어떤 효과는 안 붙으면 그 차이가 규칙처럼 보여, 실제로는 없는 구분을 읽는 사람이 만들어 내게 된다.
    /// </summary>
    public static string ValueText(TrainingEffect effect, int valueTenth) {
        float v = valueTenth / TenthScale;
        return effect == TrainingEffect.Range
            ? "+" + v.ToString("0.0")
            : "+" + v.ToString("0.#") + "%";
    }

    public static string ValueText(int effect, int valueTenth) => ValueText((TrainingEffect)effect, valueTenth);

    /// <summary>
    /// 보유한 수련 효과가 없을 때의 표시 문구. 칸을 비워 두면 "값이 없는 것"인지 "아직 안 불러온 것"인지
    /// 구분되지 않으므로, 상세처럼 항상 보이는 자리에는 이 문구를 넣는다
    /// (카드의 효과 뱃지처럼 아예 감추는 자리는 빈 문자열 그대로 쓴다).
    /// </summary>
    public const string NoEffectText = "없음";

    /// <summary>"사거리 +2.3" 형태의 한 줄 요약(카드/슬롯 표시용).</summary>
    public static string Describe(TrainingEffect effect, int valueTenth) =>
        $"{EffectName(effect)} {ValueText(effect, valueTenth)}";

    public static string Describe(int effect, int valueTenth) => Describe((TrainingEffect)effect, valueTenth);

    /// <summary>
    /// 수치 범위 문구("+0.5 ~ +5.0" / "5% ~ 50%"). 확률표가 획득 가능한 수치 칸을 안내할 때 쓴다.
    /// 범위/간격은 서버가 내려준 값이므로 여기서 수치를 만들지 않는다.
    /// </summary>
    public static string RangeText(TrainingEffect effect, int minTenth, int maxTenth) =>
        $"{ValueText(effect, minTenth)} ~ {ValueText(effect, maxTenth)}";

    /// <summary>"(0.1단위)" / "(1%단위)" 형태의 간격 문구. 수치 범위 뒤에 덧붙는 보조 설명이라 괄호로 묶는다.</summary>
    public static string StepText(TrainingEffect effect, int stepTenth) {
        float step = stepTenth / TenthScale;
        return effect == TrainingEffect.Range
            ? $"\n({step.ToString("0.#")}단위)"
            : $"\n({step.ToString("0.#")}%단위)";
    }

    // ── 실패 안내 ────────────────────────────────────
    /// <summary>서버 GameDataRepository.TrainingFail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        1 => "로그인이 필요합니다.",
        2 => "잘못된 수련 슬롯입니다.",
        3 => "아직 해금하지 않은 수련 슬롯입니다.",
        4 => "이미 수련이 진행 중인 슬롯입니다.",
        5 => "보유하지 않은 소녀입니다.",
        6 => "이미 다른 슬롯에서 수련 중인 소녀입니다.",
        7 => "진행 중인 수련이 없습니다.",
        8 => "아직 수련이 끝나지 않았습니다.",
        9 => "이미 완료된 수련입니다.",
        10 => "다이아가 부족합니다.",
        11 => "수련 완료권이 부족합니다.",
        12 => "모든 수련 슬롯을 이미 해금했습니다.",
        _ => "수련 처리에 실패했습니다."
    };

    // ── 확인 문구 ────────────────────────────────────
    // 확인 문구 강조 색(TMP 리치텍스트). 색 규칙을 여기 한 곳에 모아 문구를 고쳐도 색이 흩어지지 않게 한다
    // (스킬 설명 색 규칙을 SkillTextFormat 이 모아 두는 것과 같은 방식). 표시하는 TMP 의 Rich Text 가 켜져 있어야 한다.
    private const string LabelColor = "#3D7BE0";   // 항목 이름(현재 수련 효과) - 파랑
    private const string ValueColor = "#E04C4C";   // 그 항목의 값(대체되어 사라질 효과) - 빨강

    /// <summary>이미 수련 효과를 가진 소녀를 다시 수련시킬 때 묻는 확인 문구(효과가 대체되므로 반드시 묻는다).</summary>
    public static string OverwriteConfirmMessage(string characterName, TrainingEffect effect, int valueTenth) =>
        $"{characterName}은(는) 이미 수련이 된 소녀입니다.\n" +
        $"<color={LabelColor}>현재 수련 효과:</color> <color={ValueColor}>{Describe(effect, valueTenth)}</color>\n\n" +
        "수련 완료시 기존 수련 결과가 삭제되고 새로운 수련 결과로 변경됩니다.\n진행하시겠습니까?";

    /// <summary>진행 중인 수련을 중단할 때 묻는 확인 문구(경과 시간이 사라지므로 반드시 묻는다).</summary>
    public const string CancelConfirmMessage =
        "수련을 중단하면 지금까지 진행된 시간이 사라지고 아무 효과도 얻지 못합니다.\n중단하시겠습니까?";

    /// <summary>수련 슬롯 해금 확인 문구.</summary>
    public static string UnlockConfirmMessage(int cost) =>
        $"다이아 {cost:N0}을(를) 사용해 수련 슬롯을 하나 더 해금하시겠습니까?";

    /// <summary>즉시 완료 확인 문구.</summary>
    public static string InstantConfirmMessage(int ticketCost) =>
        $"수련 완료권 {ticketCost}장을 사용해 남은 시간을 즉시 완료하시겠습니까?";

    /// <summary>진행률(0~1). 시작/완료 시각이 없거나 뒤집혀 있으면 0 이다.</summary>
    public static float Progress01(long startUnix, long endUnix, long nowUnix) {
        if (startUnix <= 0 || endUnix <= startUnix) return 0f;
        return Mathf.Clamp01((float)(nowUnix - startUnix) / (endUnix - startUnix));
    }
}
