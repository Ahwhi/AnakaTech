using DG.Tweening;
using UnityEngine;

/// <summary>
/// 논타겟 직선 공격의 경로 예고. 정해진 시간 동안 공격이 지나갈 길을 바닥에 보여 주고 스스로 사라진다.
///
/// 프리팹 규약: 루트가 시작점, 루트의 +X 가 진행 방향이다. 길이/폭은 루트의 localScale(x=길이, y=폭)로 맞추므로
/// 자식 SpriteRenderer 는 "1 x 1 유닛 크기, 왼쪽 가운데 기준"으로 놓는다(Pivot = Left, 또는 자식을 x=0.5 에 둔다).
/// Fill 을 지정하면 시작점에서 진행 방향으로 차올라 발사까지 남은 시간을 보여 준다(선택).
/// Fill 의 피벗/자식 배치는 상관없다. 프리팹에 놓인 다 찬 모습을 기준으로 왼쪽 끝을 고정해 늘린다.
///
/// 시전자가 사라지면(사망/전장 정리) 예고도 함께 지운다. 그 공격은 나가지 않기 때문이다.
/// </summary>
public sealed class LineTelegraph : MonoBehaviour {
    [Tooltip("선택. 발사까지 남은 시간을 차오르며 보여 줄 자식. 다 찬 모습으로 배치해 두면 시작점에서 진행 방향으로만 차오른다.")]
    [SerializeField] private Transform fill;

    private ICombatUnit _caster;
    private float _remaining;

    // 다 찼을 때의 Fill 로컬 x 위치/스케일과, Fill 피벗에서 그림 왼쪽 끝까지의 거리(스케일 1 기준).
    // 프리팹에서 Fill/자식을 어떻게 배치했든(피벗이 가운데여도) 왼쪽 끝을 시작점에 고정하고 진행 방향으로만 차오르게 한다.
    private float _fillFullX;
    private float _fillFullScaleX;
    private float _fillLeftOffset;

    private void Awake() {
        if (fill == null) return;
        _fillFullX = fill.localPosition.x;
        _fillFullScaleX = fill.localScale.x;

        // 생성 직후(회전 없음)에 그림의 왼쪽 끝을 루트 로컬 좌표로 잰다.
        float left = float.MaxValue;
        foreach (var r in fill.GetComponentsInChildren<Renderer>(true))
            left = Mathf.Min(left, transform.InverseTransformPoint(r.bounds.min).x);
        if (left == float.MaxValue || Mathf.Approximately(_fillFullScaleX, 0f)) left = _fillFullX;
        _fillLeftOffset = (left - _fillFullX) / (Mathf.Approximately(_fillFullScaleX, 0f) ? 1f : _fillFullScaleX);
    }

    // t(0~1)만큼 차오른 모습. 왼쪽 끝은 그대로 두고 오른쪽(진행 방향)으로만 늘어난다.
    private void SetFill(float t) {
        float scaleX = _fillFullScaleX * t;
        float leftEdge = _fillFullX + _fillLeftOffset * _fillFullScaleX;
        var p = fill.localPosition;
        fill.localPosition = new Vector3(leftEdge - _fillLeftOffset * scaleX, p.y, p.z);
        var s = fill.localScale;
        fill.localScale = new Vector3(scaleX, s.y, s.z);
    }

    public void Show(Vector2 origin, Vector2 direction, float length, float width, float duration, ICombatUnit caster) {
        _caster = caster;
        _remaining = Mathf.Max(0.01f, duration);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(new Vector3(origin.x, origin.y, transform.position.z), Quaternion.Euler(0f, 0f, angle));
        transform.localScale = new Vector3(length, width, 1f);

        if (fill != null) {
            SetFill(0f);
            DOVirtual.Float(0f, 1f, _remaining, SetFill).SetEase(Ease.Linear).SetLink(gameObject);
        }
    }

    private void Update() {
        _remaining -= Time.deltaTime;
        if (_remaining <= 0f || !_caster.IsTargetable()) Destroy(gameObject);
    }
}
