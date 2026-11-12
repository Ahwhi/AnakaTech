using UnityEngine;

/// <summary>
/// 유닛 발밑에 반경 원을 그려 범위(사거리 등)를 보여 준다. 따라갈 대상과 반경만 받으며 그 값이 무엇인지는 모른다.
/// LineRenderer 하나로 원을 그리고, 반경이 바뀐 프레임에만 점을 다시 계산한다.
///
/// 프리팹/오브젝트 규약: 같은 오브젝트에 LineRenderer 를 둔다(Use World Space 끔, Loop 켬 권장. 코드가 다시 맞춘다).
/// 선 두께/색/머티리얼/정렬 순서는 LineRenderer 인스펙터에서 정한다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class RangeIndicator : MonoBehaviour {
    [Tooltip("원을 이루는 점 개수. 클수록 매끈하다.")]
    [SerializeField, Range(16, 256)] private int segments = 72;

    private LineRenderer _line;
    private Transform _target;
    private float _radius = -1f;


    /// <summary>대상을 따라다니며 반경 원을 그린다. 대상이 null 이면 숨긴다.</summary>
    public void Follow(Transform target, float radius) {
        _target = target;
        SetRadius(radius);
        gameObject.SetActive(target != null);
        LateUpdate();
    }

    public void Hide() {
        _target = null;
        gameObject.SetActive(false);
    }

    /// <summary>반경만 바꾼다(변신/버프로 범위가 달라졌을 때). 같은 값이면 아무것도 하지 않는다.</summary>
    public void SetRadius(float radius) {
        radius = Mathf.Max(0f, radius);
        if (Mathf.Approximately(radius, _radius)) return;
        _radius = radius;
        // 비활성으로 시작한 오브젝트는 Awake 가 늦게 불리므로 여기서 직접 잡는다.
        if (_line == null) _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.loop = true;
        _line.positionCount = segments;
        for (int i = 0; i < segments; i++) {
            float a = i * Mathf.PI * 2f / segments;
            _line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }

    private void LateUpdate() {
        if (_target == null) return;
        Vector3 p = _target.position;
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }
}
