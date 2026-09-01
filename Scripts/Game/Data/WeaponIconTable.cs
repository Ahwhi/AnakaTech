using UnityEngine;

/// <summary>
/// 무기 타입별 아이콘 스프라이트를 담는 공용 ScriptableObject 테이블.
/// 여러 UI(덱 팝업, 도감 상세 등)가 동일한 매핑을 공유하도록 프로젝트 전역에서 이 에셋 하나를 참조한다.
/// icons 는 WeaponType 순서(None,Sword,Bow,Gun,Knuckle,Staff)대로 채운다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Weapon Icon Table", fileName = "WeaponIconTable")]
public sealed class WeaponIconTable : ScriptableObject {
    [Tooltip("WeaponType 순서대로 아이콘(None,Sword,Bow,Gun,Knuckle,Staff). 비우면 해당 타입은 아이콘 없음.")]
    [SerializeField] private Sprite[] icons;

    /// <summary>무기 타입에 대응하는 아이콘. 매핑이 없거나 범위를 벗어나면 null.</summary>
    public Sprite Get(WeaponType type) {
        int i = (int)type;
        return icons != null && i >= 0 && i < icons.Length ? icons[i] : null;
    }
}
