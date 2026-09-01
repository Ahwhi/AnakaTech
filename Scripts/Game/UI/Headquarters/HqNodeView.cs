using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>본부 노드의 표시 상태. 색/테두리/버튼 활성이 이 값 하나로 결정된다.</summary>
public enum HqNodeState {
    Locked,      // 선행 노드 미해금 - 회색, 클릭 불가
    Available,   // 열 수 있음(레벨 0) - 강조 테두리
    Owned,       // 레벨 1 이상, 더 올릴 수 있음
    Maxed        // 최대 레벨
}

/// <summary>
/// 본부 노드 트리의 노드 1개. 트리 화면(HeadquartersPopup)이 데이터로부터 생성/배치하므로
/// 프리팹은 "노드 한 칸"의 모양만 갖고 있으면 되고, 몇 개가 어디에 놓이는지는 데이터가 정한다.
/// </summary>
public sealed class HqNodeView : MonoBehaviour {
    [Header("Visuals")]
    [SerializeField] private Image icon;
    [Tooltip("상태 색이 입혀지는 배경/테두리.")]
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("레벨 표시(예: 3/10). 잠긴 노드는 숨긴다.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("잠김 표시(자물쇠 등). 선택.")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("지금 올릴 수 있음(재화 충분) 표시 배지. 선택.")]
    [SerializeField] private BadgeView upgradableBadge;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    [Header("State Colors")]
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.4f);
    [SerializeField] private Color availableColor = new Color(0.95f, 0.85f, 0.35f);
    [SerializeField] private Color ownedColor = new Color(0.35f, 0.75f, 1f);
    [SerializeField] private Color maxedColor = new Color(0.45f, 1f, 0.6f);

    public HqNodeData Data { get; private set; }
    public int Level { get; private set; }
    public HqNodeState State { get; private set; }

    /// <summary>연결선이 붙을 기준점. 노드 자신의 RectTransform 을 그대로 쓴다.</summary>
    public RectTransform Rect => (RectTransform)transform;

    public void Bind(HqNodeData data, int level, HqNodeState state, bool upgradable, Action<HqNodeView> onClick) {
        Data = data;
        Level = level;
        State = state;

        if (icon != null) {
            icon.enabled = data != null && data.Icon != null;
            if (data != null && data.Icon != null) icon.sprite = data.Icon;
            icon.color = state == HqNodeState.Locked ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }
        if (frame != null) frame.color = ColorFor(state);
        if (nameText != null) nameText.text = data != null ? data.DisplayName : "";
        if (levelText != null) {
            bool show = state != HqNodeState.Locked;
            levelText.gameObject.SetActive(show);
            if (show) levelText.text = $"{level} / {HqNodeRules.MaxLevel}";
        }
        if (lockedGroup != null) lockedGroup.SetActive(state == HqNodeState.Locked);
        if (upgradableBadge != null) upgradableBadge.SetOn(upgradable);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            // 잠긴 노드도 눌러서 "무엇이 필요한지" 볼 수 있게 둔다(상세 패널이 선행 조건을 알려 준다).
            button.interactable = true;
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    /// <summary>연결선이 쓸 상태 색(선행 노드가 열렸는지에 따라 선도 함께 밝아진다).</summary>
    public Color ColorForLink => ColorFor(State);

    private Color ColorFor(HqNodeState state) => state switch {
        HqNodeState.Locked => lockedColor,
        HqNodeState.Available => availableColor,
        HqNodeState.Owned => ownedColor,
        HqNodeState.Maxed => maxedColor,
        _ => lockedColor
    };
}
