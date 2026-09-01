using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 스탯 현황을 프리팹 행(StatRowView)으로 Vertical Layout Group 아래에 노출한다.
/// 개별 텍스트를 하나하나 할당하던 방식을 대체한다. 행은 재사용(풀링)하며, 표시할 스탯 목록은 인스펙터에서 지정 가능(비우면 전체).
/// </summary>
public sealed class StatListView : MonoBehaviour {
    [Tooltip("한 줄 프리팹(StatRowView). rowParent 아래에 복제된다.")]
    [SerializeField] private StatRowView rowPrefab;
    [Tooltip("행이 붙는 컨테이너(Vertical Layout Group 권장). 비우면 이 오브젝트.")]
    [SerializeField] private Transform rowParent;
    [Tooltip("표시할 스탯/순서. 비우면 기본 순서(전체) 사용.")]
    [SerializeField] private StatType[] statsToShow;
    [Tooltip("행 배경색. 위에서부터 순서대로 반복 적용한다(2색=줄무늬, 스탯 수만큼 넣으면 스탯별 고유색). " +
             "비우면 프리팹 색을 그대로 둔다.")]
    [SerializeField] private Color[] rowColors = {
        new Color(0f, 0f, 0f, 0.6666667f),
        new Color(0f, 0f, 0f, 0.3333333f),
    };

    private readonly List<StatRowView> _rows = new();

    private static readonly StatType[] DefaultOrder = {
        StatType.Attack, StatType.Defense, StatType.Health, StatType.Range,
        StatType.AttackSpeed, StatType.MoveSpeed, StatType.CritRate, StatType.CritDamage,
        StatType.MinDamageRatio, StatType.MaxDamageRatio, StatType.ArmorPen,
        StatType.FinalDamage, StatType.SkillAccel, StatType.SkillCooldownReduction
    };

    /// <summary>주어진 스탯 묶음으로 목록을 갱신한다(행 부족분만 생성, 나머지는 재사용).</summary>
    public void Populate(CombatStats s) {
        if (rowPrefab == null) return;
        var parent = rowParent != null ? rowParent : transform;
        var order = (statsToShow != null && statsToShow.Length > 0) ? statsToShow : DefaultOrder;

        for (int i = _rows.Count; i < order.Length; i++)
            _rows.Add(Instantiate(rowPrefab, parent));

        for (int i = 0; i < _rows.Count; i++) {
            bool used = i < order.Length;
            _rows[i].gameObject.SetActive(used);
            if (!used) continue;
            _rows[i].Set(order[i], StatFormat.Value(s, order[i]));
            // 표시 순서 기준으로 색을 순환 적용한다(행을 재사용해도 위치별 색이 항상 같다).
            if (rowColors != null && rowColors.Length > 0)
                _rows[i].SetBackgroundColor(rowColors[i % rowColors.Length]);
        }
    }
}
