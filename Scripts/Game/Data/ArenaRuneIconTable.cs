using UnityEngine;

/// <summary>
/// 쟁탈전 룬 계열별 아이콘 묶음. 룬 장착 팝업과 쟁탈전 화면이 같은 에셋을 참조해 같은 아이콘을 쓴다.
/// 순서는 ArenaRuneRules.Family 순서(공격력 / 공격속도 / 방어구관통력 / 최종데미지 / 피격데미지감소 / 방어력 / 체력).
/// </summary>
[CreateAssetMenu(fileName = "ArenaRuneIconTable", menuName = "Anaka/Arena/Rune Icon Table")]
public sealed class ArenaRuneIconTable : ScriptableObject {
    [Tooltip("계열 순서대로 7칸. 본부 노드 아이콘 스프라이트를 그대로 넣으면 된다. 비운 칸은 아이콘 없이 그린다.")]
    [SerializeField] private Sprite[] icons = new Sprite[ArenaRuneRules.FamilyCount];

    public Sprite IconOf(ArenaRuneRules.Family family) {
        int i = (int)family;
        return icons != null && i >= 0 && i < icons.Length ? icons[i] : null;
    }
}
