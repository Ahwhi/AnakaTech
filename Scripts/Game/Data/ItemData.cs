using UnityEngine;

/// <summary>
/// 소모품 아이템의 사용 방식(서버 ItemKind 와 값이 일치해야 한다).
/// 대상 캐릭터가 필요한지 여부를 UI 가 이 값으로 판단한다. SO 가 값을 직렬화하므로 끝에만 추가할 것.
/// </summary>
public enum ItemKind {
    /// <summary>대상 캐릭터에게 경험치를 준다(현재 레벨의 필요 경험치에 비례).</summary>
    ExpCoupon = 0,
    /// <summary>대상 캐릭터의 누적 경험치를 전부 뽑아 경험치 캡슐로 바꾼다(1레벨, 투자 스탯 0 으로 초기화).</summary>
    ExpExtract = 1,
    /// <summary>추출한 경험치. 보유 개수가 곧 경험치 양이다.</summary>
    ExpCapsule = 2,
    /// <summary>성장 초기화. 승급 조각/다이아, 한계돌파권, 누적 경험치(캡슐)를 돌려받고 1성 1레벨로 되돌린다.</summary>
    GrowthReset = 3
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
    public const int ExpCoupon100 = 1005;
    public const int ExpExtractTicket = 2001;
    public const int ExpCapsule = 2002;
    public const int ExpCapsuleLarge = 2003;

    /// <summary>경험치 캡슐(대) 1개의 경험치. 서버 ItemCatalog.LargeCapsuleExp 와 일치해야 한다(표시용 미러).</summary>
    public const int LargeCapsuleExp = 100000;
    public const int GrowthResetTicket = 3001;
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
        ItemKind.ExpExtract => true,
        ItemKind.ExpCapsule => true,
        ItemKind.GrowthReset => true,
        _ => false
    };

    /// <summary>한 번에 여러 개를 쓸 수 있는지(서버 ItemCatalog.AllowsMultiUse 와 같은 규칙). 되돌릴 수 없는 아이템은 하나씩만 쓴다.</summary>
    public bool AllowsMultiUse => kind switch {
        ItemKind.ExpExtract => false,
        ItemKind.GrowthReset => false,
        _ => true
    };

    /// <summary>보유 개수가 곧 수량(경험치 양 등)인 아이템인지. 이런 아이템은 1개 단위가 아니라 보유량 전체를 한 번에 쓴다.</summary>
    public bool CountIsAmount => kind == ItemKind.ExpCapsule;

    /// <summary>쓰기 전에 결과(서버 미리보기)를 보여 주고 확인을 받아야 하는지. 되돌릴 수 없거나 한 번에 많이 쓰이는 아이템이다.</summary>
    public bool RequiresConfirm =>
        kind == ItemKind.ExpExtract || kind == ItemKind.ExpCapsule || kind == ItemKind.GrowthReset;
}
