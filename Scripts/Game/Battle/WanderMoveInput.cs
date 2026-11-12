using System;
using UnityEngine;

/// <summary>
/// 정해진 사각 영역 안을 떠도는 이동 입력(IUnitMoveInput). 일정 시간마다 무작위 방향으로 틀고,
/// 영역 가장자리에 닿으면 안쪽으로 튕겨 돌아온다. 대상을 쫓지 않으므로 유닛들이 한곳으로 몰리지 않는다.
/// 유닛은 이 클래스를 모르고 IUnitMoveInput 만 읽는다(직접 조작 입력과 같은 자리).
/// </summary>
public sealed class WanderMoveInput : IUnitMoveInput {
    /// <summary>떠도는 방식. 컨텐츠 데이터(SO)에 그대로 둔다.</summary>
    [Serializable]
    public sealed class Pattern {
        [Tooltip("방향을 바꾸는 최소 간격(초).")]
        [Min(0.1f)] public float MinTurnSeconds = 1f;
        [Tooltip("방향을 바꾸는 최대 간격(초).")]
        [Min(0.1f)] public float MaxTurnSeconds = 2.5f;
        [Tooltip("이동 속도 비율(0~1). 1 이면 유닛의 이동 속도 그대로다.")]
        [Range(0f, 1f)] public float SpeedScale = 0.7f;
        [Tooltip("가장자리에서 이 거리 안으로 들어오면 안쪽으로 방향을 돌린다(월드 units).")]
        [Min(0f)] public float EdgeMargin = 1f;
    }

    private readonly Transform _self;
    private readonly Vector2 _center;
    private readonly Vector2 _halfSize;
    private readonly Pattern _pattern;

    private Vector2 _heading;
    private float _nextTurnAt = float.MinValue;

    public WanderMoveInput(Transform self, Vector2 center, Vector2 halfSize, Pattern pattern) {
        _self = self;
        _center = center;
        _halfSize = halfSize;
        _pattern = pattern ?? new Pattern();
    }

    public Vector2 Direction {
        get {
            if (_self == null) return Vector2.zero;

            if (Time.time >= _nextTurnAt) {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                _heading = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float min = Mathf.Min(_pattern.MinTurnSeconds, _pattern.MaxTurnSeconds);
                float max = Mathf.Max(_pattern.MinTurnSeconds, _pattern.MaxTurnSeconds);
                _nextTurnAt = Time.time + UnityEngine.Random.Range(min, max);
            }

            // 가장자리 근처에서 바깥으로 향하면 그 축만 뒤집는다(벽에 붙어 멈추지 않고 튕겨 나온다).
            Vector2 p = _self.position;
            float m = _pattern.EdgeMargin;
            if ((p.x >= _center.x + _halfSize.x - m && _heading.x > 0f) ||
                (p.x <= _center.x - _halfSize.x + m && _heading.x < 0f)) _heading.x = -_heading.x;
            if ((p.y >= _center.y + _halfSize.y - m && _heading.y > 0f) ||
                (p.y <= _center.y - _halfSize.y + m && _heading.y < 0f)) _heading.y = -_heading.y;

            return _heading * _pattern.SpeedScale;
        }
    }
}
