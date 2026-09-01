using System;
using UnityEngine;

/// <summary>
/// 각인 효과의 발동 조건. 조건 판정은 전투 중 CharacterUnit 이 하고, 여기서는 "무슨 조건인지"만 표현한다.
/// 새 조건을 추가할 때는 이 열거형 + CharacterUnit 의 조건 갱신부 한 곳만 손보면 된다.
/// </summary>
public enum EngravingTrigger {
    /// <summary>항상 적용. 전투 밖(전투력 표시/상세 스탯)에도 그대로 반영된다.</summary>
    Always,
    /// <summary>전투 시작 후 Param 초 동안만 적용(선공/초반 화력 컨셉).</summary>
    BattleStart,
    /// <summary>자신의 체력 비율이 Param % 이하일 때 적용(위기 대응/생존 컨셉).</summary>
    SelfHpBelow
}

/// <summary>각인 계열. 표시 분류와 필터링에만 쓰이며 효과 계산에는 관여하지 않는다.</summary>
public enum EngravingCategory {
    Offense,   // 공격
    Defense,   // 생존
    Tempo      // 속도/재사용
}

/// <summary>
/// 각인 1종의 정적 데이터. 각인은 "캐릭터 슬롯에 끼우는 특성"이며, 보유/레벨은 계정 공용(서버 권위)이고
/// 어느 캐릭터의 어느 슬롯에 끼웠는지만 캐릭터별로 다르다.
///
/// 효과 값은 레벨에 따라 선형으로 오른다: 레벨 L 의 값 = ValueAtLevel1 + ValuePerLevel * (L - 1).
/// 조건이 Always 인 효과는 성장 스탯처럼 합산되어 전투력/상세 스탯에도 보이고,
/// 조건부 효과는 전투 중에만 붙었다 떨어진다(표시에는 설명 문구로만 나온다).
/// </summary>
[CreateAssetMenu(fileName = "EngravingData", menuName = "Anaka/Data/Engraving")]
public sealed class EngravingData : ScriptableObject {
    /// <summary>각인 효과 1줄. 스탯 1종 + 조건 1개의 조합이라 하나의 각인이 여러 줄을 가질 수 있다.</summary>
    [Serializable]
    public struct Effect {
        [Tooltip("올릴 스탯. 단위는 CombatStats 주석의 규약을 따른다(예: 공격속도는 %p).")]
        public StatType Stat;
        [Tooltip("각인 레벨 1일 때의 값.")]
        public float ValueAtLevel1;
        [Tooltip("레벨이 1 오를 때마다 추가되는 값.")]
        public float ValuePerLevel;
        [Tooltip("발동 조건.")]
        public EngravingTrigger Trigger;
        [Tooltip("조건 인자. BattleStart=지속 시간(초), SelfHpBelow=체력 비율(%). Always 는 사용하지 않는다.")]
        public float TriggerParam;

        /// <summary>지정 레벨에서의 효과 값(레벨 1 미만은 1로 취급).</summary>
        public float ValueAt(int level) => ValueAtLevel1 + ValuePerLevel * (Mathf.Max(1, level) - 1);
    }

    [Header("Identity")]
    [Tooltip("서버와 공유하는 고정 id. 서버 EngravingCatalog.AllIds 와 일치해야 한다.")]
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private EngravingCategory category = EngravingCategory.Offense;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [Tooltip("각인 카드/노드의 테두리 색. 계열을 한눈에 구분하는 용도.")]
    [SerializeField] private Color themeColor = Color.white;

    [Header("Effects")]
    [Tooltip("이 각인이 주는 효과들. 같은 스탯을 조건만 다르게 여러 줄로 둘 수도 있다.")]
    [SerializeField] private Effect[] effects = Array.Empty<Effect>();

    [Header("Description")]
    [Tooltip("설명 문구. {0} 자리에 레벨별 효과 요약이 들어간다. 비우면 효과 목록으로 자동 생성한다.")]
    [TextArea(2, 4)]
    [SerializeField] private string descriptionFormat;

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public EngravingCategory Category => category;
    public Sprite Icon => icon;
    public Color ThemeColor => themeColor;
    public Effect[] Effects => effects;

    /// <summary>지정 레벨 기준 설명 문구. 형식 문자열이 없으면 효과 목록을 사람이 읽을 수 있게 조립한다.</summary>
    public string Description(int level) {
        string summary = EffectSummary(level);
        return string.IsNullOrEmpty(descriptionFormat) ? summary : string.Format(descriptionFormat, summary);
    }

    /// <summary>레벨별 효과 요약(조건 + 스탯 + 값). 각인 카드/툴팁이 공통으로 쓴다.</summary>
    public string EffectSummary(int level) {
        if (effects == null || effects.Length == 0) return "";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < effects.Length; i++) {
            if (i > 0) sb.Append('\n');
            var e = effects[i];
            string condition = EngravingRules.TriggerText(e.Trigger, e.TriggerParam);
            if (!string.IsNullOrEmpty(condition)) sb.Append(condition).Append(' ');
            sb.Append(GrowthRules.StatName(e.Stat))
              .Append(" +")
              .Append(StatFormat.FormatValue(e.Stat, e.ValueAt(level)));
        }
        return sb.ToString();
    }
}
