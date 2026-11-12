using UnityEngine;

/// <summary>
/// 전장에 놓이는 회복 아이템 한 개. 자기 자리와 집히는 반경만 알고, 누가 언제 집는지와 얼마나 회복되는지는
/// 아이템을 뿌리는 쪽(전장)이 정한다. 프리팹에는 보이는 모습(SpriteRenderer 등)과 이 컴포넌트만 둔다.
/// </summary>
public sealed class HealPickup : MonoBehaviour {
    [Tooltip("집히는 반경(월드 units). 유닛의 몸통 반경과 더해 판정한다.")]
    [SerializeField, Min(0.05f)] private float pickupRadius = 0.5f;

    public float PickupRadius => pickupRadius;
    public Vector2 Position => transform.position;

    /// <summary>유닛이 이 아이템을 집을 만큼 가까운지.</summary>
    public bool IsTouching(BattleUnit unit) =>
        unit != null && Vector2.Distance(unit.Position, Position) <= pickupRadius + unit.BodyRadius;
}
