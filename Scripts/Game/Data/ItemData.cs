using UnityEngine;

/// <summary>
/// 소모품 아이템의 사용 방식(서버 ItemKind 와 값이 일치해야 한다).
/// 대상 캐릭터가 필요한지 여부를 UI 가 이 값으로 판단한다.
/// </summary>
public enum ItemKind {
    /// <summary>대상 캐릭터에게 경험치를 준다(현재 레벨의 필요 경험치에 비례).</summary>
    ExpCoupon
}

/// <summary>
/// 서버 ItemCatalog 의 아이템 id 미러. 보상표 같은 코드에서 id 를 직접 쓰지 않도록 이름을 붙여 둔다
/// (값이 어긋나면 지급은 서버가 정답이고 클라 표시만 틀어진다).
/// </summary>
public static class ItemKeys {
    public const int ExpCoupon10 = 1001;
    public const int ExpCoupon20 = 1002;
    public const int ExpCoupon30 = 1003;
    public const int ExpCoupon50 = 1004;
}

/// <summary>
/// 소모품 아이템 1종의 표시 데이터. 효과 수치와 보유 개수는 서버 자산이라 여기 두지 않고,
/// 이 SO 는 "무엇으로 보이는가"(아이콘/이름/설명)만 담당한다.
///
/// 새 아이템을 추가할 때 클라가 할 일은 이 에셋 하나를 만들어 ItemDatabase 에 등록하는 것뿐이다
/// (사용 UI 는 종류를 모르고 목록만 그린다).
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Anaka/Data/Item")]
public sealed class ItemData : ScriptableObject {
    [Header("Identity")]
    [Tooltip("서버와 공유하는 고정 id. 서버 ItemCatalog 의 id 와 일치해야 한다.")]
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private ItemKind kind = ItemKind.ExpCoupon;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [Tooltip("아이콘 위에 겹쳐 표시할 짧은 문구(예: 10%). 비우면 표시하지 않는다.")]
    [SerializeField] private string badgeText;

    [Header("Description")]
    [Tooltip("사용 안내 문구. 실제 지급량은 서버가 정하므로 수치를 단정하지 말고 규칙을 설명한다.")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public ItemKind Kind => kind;
    public Sprite Icon => icon;
    public string BadgeText => badgeText;
    public string Description => description;

    /// <summary>사용할 때 대상 캐릭터를 골라야 하는지(서버 ItemCatalog.RequiresTarget 과 같은 규칙).</summary>
    public bool RequiresTarget => kind switch {
        ItemKind.ExpCoupon => true,
        _ => false
    };
}
