using UnityEngine;

/// <summary>
/// 장비 정적 데이터. 표현(이름/아이콘)과 분류(부위/무기종류/등급)만 담는다.
///
/// 능력치는 이 에셋에 없다. 장비 능력치는 드랍 시 서버가 굴린 포인트 + 성장(각성/레벨업)으로 결정되며,
/// 부위별 스탯 종류/스케일은 EquipmentStatLayout, 성장 규칙은 EquipmentRules 가 담당한다.
/// 즉 같은 장비 id 여도 인스턴스마다 능력치가 다르다(개체별 랜덤 수치).
///
/// 전용무기: id 가 "캐릭터 id + EquipmentRules.SignatureIdOffset" 규약을 따르며, 주인 캐릭터가 장착하면
/// 보너스 스탯을 받는다(SignatureBonusCatalog). 주인이 아닌 캐릭터도 무기 종류만 맞으면 장착은 가능하다(보너스만 없음).
/// </summary>
[CreateAssetMenu(fileName = "EquipmentData", menuName = "Anaka/Data/Equipment")]
public sealed class EquipmentData : ScriptableObject, IIllustrationSource {
    [Header("Identity")]
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private EquipmentSlot slot;
    [Tooltip("slot 이 Weapon 일 때만 사용. 그 외에는 None.")]
    [SerializeField] private WeaponType weaponType = WeaponType.None;
    [SerializeField] private Grade grade;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public EquipmentSlot Slot => slot;
    public WeaponType WeaponType => slot == EquipmentSlot.Weapon ? weaponType : WeaponType.None;
    public Grade Grade => grade;
    public Sprite Icon => icon;

    // IIllustrationSource: 일러스트 슬롯(CharacterIllustrationView)에 캐릭터와 동일한 방식으로 넘기기 위한 구현.
    // 아이콘은 투명 여백이 재단되어 있어 가로세로 비율이 제각각이므로, 잘리지 않게 Contain 으로 잡는다.
    // 전체를 다 보여주는 방식이라 관심점 개념이 없어 중심을 그대로 쓴다(부위별 편차는 아트에서 잡는다).
    Sprite IIllustrationSource.Illustration => icon;
    Vector2 IIllustrationSource.UiFocus => new Vector2(0.5f, 0.5f);
    IllustrationFit IIllustrationSource.Fit => IllustrationFit.Contain;

    /// <summary>보유 옵션 개수(등급이 결정한다). N=0 ... XR=4.</summary>
    public int OptionCount => EquipmentStatLayout.OptionCount(grade);

    /// <summary>이 장비가 전용무기인지.</summary>
    public bool IsSignature => EquipmentRules.IsSignature(id);

    /// <summary>전용무기의 주인 캐릭터 id(전용무기가 아니면 0).</summary>
    public int OwnerCharDataId => EquipmentRules.OwnerCharOf(id);

    /// <summary>지정 캐릭터 타입이 이 장비를 장착할 수 있는지(무기 타입 제한). 무기가 아니면 항상 가능.</summary>
    public bool CanBeEquippedBy(CharacterType charType) {
        if (slot != EquipmentSlot.Weapon) return true;
        return GameEnumRules.CanEquip(charType, WeaponType);
    }

    /// <summary>이 장비가 지정 캐릭터의 "본인" 전용무기인지(장착 보너스 대상).</summary>
    public bool IsOwnSignatureOf(int charDataId) => EquipmentRules.IsOwnSignature(id, charDataId);
}
