using System;
using UnityEngine;

/// <summary>본부 노드 계열. 트리 표시의 열 배치/색 구분과 필터에만 쓰인다(값은 에셋에 저장되므로 순서 고정).</summary>
public enum HqNodeLine {
    Offense,  // 화력
    Guard,    // 생존
    Finesse   // 속도/재사용
}

/// <summary>
/// 계열 표시명. 본부에서 올리는 것이 "시설"이 아니라 "단원 훈련"이라는 점을 문구로 드러내,
/// 건물 메타포와 실제 효과(스탯 상승)가 따로 놀지 않게 한다. 계열이 늘면 여기 한 줄만 추가하면 된다.
/// </summary>
public static class HqNodeLines {
    public static string DisplayName(HqNodeLine line) => line switch {
        HqNodeLine.Offense => "공격 훈련",
        HqNodeLine.Guard => "방어 훈련",
        HqNodeLine.Finesse => "기교 훈련",
        _ => "훈련"
    };
}

/// <summary>
/// 용병단 본부 노드 1개의 정적 데이터(계정 단위 성장). 효과는 "보유한 모든 캐릭터"에게 적용되는 스탯 보너스이며,
/// 장비/각인과 같은 이유로 밸런스 수치는 클라가 갖고 서버는 노드 id 와 선행 목록만 안다(HeadquartersCatalog).
///
/// 최대 레벨과 레벨업 비용은 노드마다 다르지 않으므로 이 에셋이 들고 있지 않다(HqNodeRules).
/// 노드가 늘 때마다 에셋과 서버 표에 같은 숫자를 두 번 적는 일을 없애기 위해서다.
/// 이 에셋에 남은 성장 데이터는 트리 모양을 정하는 선행 목록뿐이다.
///
/// 노드 배치(GridPos)까지 데이터로 갖는 이유: 트리 화면을 프리팹으로 손배치하지 않고 이 좌표로 생성하면
/// 노드를 추가할 때 에셋만 늘리면 되고 UI 프리팹은 손대지 않아도 된다(연결선도 Requires 로 자동으로 그린다).
/// </summary>
[CreateAssetMenu(fileName = "HqNodeData", menuName = "Anaka/Data/Hq Node")]
public sealed class HqNodeData : ScriptableObject {
    /// <summary>노드 1레벨당 오르는 스탯 1종.</summary>
    [Serializable]
    public struct Effect {
        [Tooltip("올릴 스탯. 단위는 CombatStats 주석의 규약을 따른다.")]
        public StatType Stat;
        [Tooltip("레벨 1당 상승치. 노드 레벨 L 이면 이 값 x L 이 적용된다.")]
        public float ValuePerLevel;
    }

    [Header("Identity")]
    [Tooltip("서버와 공유하는 고정 id. 서버 HeadquartersCatalog 의 노드 id 와 일치해야 한다.")]
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private HqNodeLine line = HqNodeLine.Offense;
    [SerializeField] private Sprite icon;

    [Header("Tree Layout")]
    [Tooltip("트리 화면에서의 격자 좌표(x=열, y=행). 실제 픽셀 간격은 트리 패널이 정한다. " +
             "y 가 클수록 화면 아래쪽에 배치된다.")]
    [SerializeField] private Vector2 gridPos;

    [Header("Growth (선행 목록은 서버 HeadquartersCatalog 와 일치해야 한다)")]
    [Tooltip("선행 노드 id. 전부 HqNodeRules.PrerequisiteLevel 이상이어야 이 노드를 열 수 있다. 트리 연결선도 이 목록으로 그린다.")]
    [SerializeField] private int[] requires = Array.Empty<int>();

    [Header("Effects")]
    [SerializeField] private Effect[] effects = Array.Empty<Effect>();

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public HqNodeLine Line => line;
    public Sprite Icon => icon;
    public Vector2 GridPos => gridPos;
    public int[] Requires => requires;
    public Effect[] Effects => effects;

    /// <summary>현재 레벨에서 다음 레벨로 올리는 비용(명성). 최대 레벨이면 0.</summary>
    public int UpgradeCost(int currentLevel) =>
        currentLevel >= HqNodeRules.MaxLevel ? 0 : HqNodeRules.RenownPerLevel * (currentLevel + 1);

    /// <summary>지정 레벨에서 이 노드가 주는 스탯 보너스 합계.</summary>
    public CombatStats StatsAt(int level) {
        var sum = new CombatStats();
        if (level <= 0 || effects == null) return sum;
        for (int i = 0; i < effects.Length; i++)
            sum = sum.WithAdded(effects[i].Stat, effects[i].ValuePerLevel * level);
        return sum;
    }

    /// <summary>레벨별 효과 요약 문구(노드 상세 표시용). 0레벨이면 "다음 레벨" 값을 보여주도록 호출측이 정한다.</summary>
    public string EffectSummary(int level) => StatFormat.DeltaSummary(StatsAt(level));
}
