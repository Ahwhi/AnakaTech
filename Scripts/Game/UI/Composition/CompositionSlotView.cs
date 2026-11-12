using UnityEngine;

/// <summary>
/// 편성 슬롯 1칸(진형 자리). 드래그로 캐릭터를 받아 넣고, 여기서 다시 끌어내 해제/맞바꿈하는 자리다.
///
/// 이 컴포넌트는 "자리"일 뿐 캐릭터를 그리지 않는다. 슬롯에 놓인 캐릭터는 덱 카드(DeckCharacterCard)를
/// 그대로 붙여 보여 준다. 슬롯이 일러/이름/레벨을 따로 그리면 같은 카드가 목록과 슬롯에서 다르게 보이고,
/// 카드 표시를 고칠 때마다 두 곳을 맞춰야 한다. 카드를 그대로 옮겨 담으면 그럴 일이 없다.
///
/// 슬롯은 5칸 모두 같은 프리팹/같은 규칙이다. 제일 왼쪽(0번)만 "대표 슬롯"으로 표식이 켜지며,
/// 거기 놓인 캐릭터가 곧 대표가 된다. 대표 전용 슬롯을 따로 두지 않은 이유는, 그렇게 하면 그 칸만
/// 드래그 규칙이 달라져 장착/해제/맞바꿈이 두 벌이 되기 때문이다. 슬롯을 균일하게 두면
/// "왼쪽 끝으로 끌어다 놓으면 대표" 라는 조작 하나로 대표 지정까지 자연히 해결된다.
///
/// 빈 슬롯도 드롭 대상이라 항상 살아 있어야 한다(꺼 두면 드롭을 받을 수 없다).
/// </summary>
public sealed class CompositionSlotView : MonoBehaviour {
    [Tooltip("덱 카드가 들어갈 자리(빈 RectTransform). 카드는 이 사각형에 꽉 채워 붙는다. " +
             "비우면 슬롯 자신에 붙이므로, 여백을 주고 싶으면 안쪽에 자식을 하나 만들어 지정한다.")]
    [SerializeField] private RectTransform cardContainer;
    [Tooltip("빈 슬롯 표시 묶음(점선 테두리/+ 아이콘 등). 캐릭터가 있으면 꺼진다.")]
    [SerializeField] private GameObject emptyGroup;
    [Tooltip("대표 슬롯 표식(왕관/'대표' 라벨 등). 0번 슬롯에서만 켜진다. 선택.")]
    [SerializeField] private GameObject representativeMark;
    [Tooltip("드래그한 카드가 이 슬롯 위에 올라왔을 때 켜지는 강조 표시. 선택.")]
    [SerializeField] private GameObject dropHighlight;
    [Tooltip("이 자리의 소녀가 고정되어 빼거나 옮길 수 없을 때 켜지는 표식(전선 등). 선택. 쓰지 않는 화면은 비워 둔다.")]
    [SerializeField] private GameObject lockedMark;

    /// <summary>이 슬롯의 번호(0 = 대표 슬롯). 편성 화면이 생성하며 정해 준다.</summary>
    public int SlotIndex { get; private set; }

    /// <summary>카드가 붙을 자리. 지정하지 않았으면 슬롯 자신이다.</summary>
    public RectTransform CardContainer => cardContainer != null ? cardContainer : (RectTransform)transform;

    /// <summary>슬롯 번호를 정하고 표식을 초기화한다(생성 직후 1회).</summary>
    public void Initialize(int slotIndex) {
        SlotIndex = slotIndex;
        if (representativeMark != null) representativeMark.SetActive(slotIndex == 0);
        SetDropHighlight(false);
        SetLocked(false);
    }

    /// <summary>
    /// 고정 표식. 표시만 하며, 실제로 드래그/드롭을 막는 판단은 이 슬롯을 쓰는 화면이 한다
    /// (고정 규칙은 화면마다 다르므로 슬롯은 규칙을 알지 않는다).
    /// </summary>
    public void SetLocked(bool locked) {
        if (lockedMark != null && lockedMark.activeSelf != locked) lockedMark.SetActive(locked);
    }

    /// <summary>이 슬롯이 채워졌는지 표시한다(빈 칸 안내를 켜고 끈다).</summary>
    public void SetFilled(bool filled) {
        if (emptyGroup != null) emptyGroup.SetActive(!filled);
    }

    /// <summary>덱 카드를 이 슬롯에 붙인다(자리를 꽉 채운다).</summary>
    public void Attach(RectTransform card) {
        if (card == null) return;

        var container = CardContainer;
        card.SetParent(container, false);
        card.anchorMin = Vector2.zero;
        card.anchorMax = Vector2.one;
        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;
        card.localScale = Vector3.one;
        card.localRotation = Quaternion.identity;
    }

    /// <summary>드래그한 카드가 이 슬롯 위에 있을 때의 강조 표시.</summary>
    public void SetDropHighlight(bool on) {
        if (dropHighlight != null) dropHighlight.SetActive(on);
    }
}
