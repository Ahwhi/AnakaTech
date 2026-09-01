using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배지 프리팹 공용 카탈로그(SO). 배지 상태(EventBadge) 하나당 프리팹 하나를 배선한다.
///
/// 스프라이트가 아니라 프리팹인 이유: Claimable(빨간 동그라미)과 Unread(NEW)는 원본 크기와 여백이 서로 달라서
/// 버튼에 미리 잡아둔 한 개의 Image 크기에 억지로 맞추면 어느 한쪽이 반드시 어색해진다. 프리팹이면 각 배지가
/// 자기 크기/여백/애니메이션(두근거림 등)을 스스로 들고 다니므로, 버튼 쪽은 "어디에 놓을지"만 정하면 된다.
///
/// 앱 시작 시 MainLifetimeScope 가 SetDefault 로 1회 등록하면 모든 BadgeView 가 이 값을 쓴다
/// (GradeColorCatalog 와 동일 패턴). 아트 교체는 이 SO 의 프리팹만 바꾸면 전 화면에 반영된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Badge Catalog", fileName = "BadgeCatalog")]
public sealed class BadgeCatalog : ScriptableObject {
    [Serializable]
    public struct Entry {
        public EventBadge state;
        public GameObject prefab;
    }

    [Tooltip("배지 상태별 프리팹. Unread=NEW 뱃지, Claimable=빨간 동그라미. None 은 표시 없음이므로 배선하지 않는다.")]
    [SerializeField] private Entry[] entries;

    private Dictionary<EventBadge, GameObject> _map;

    /// <summary>배지 상태에 대응하는 프리팹(배선이 없으면 null = 표시 없음).</summary>
    public GameObject Prefab(EventBadge state) {
        if (state == EventBadge.None) return null;
        if (_map == null) {
            _map = new Dictionary<EventBadge, GameObject>();
            if (entries != null)
                foreach (var e in entries) _map[e.state] = e.prefab;
        }
        return _map.TryGetValue(state, out var p) ? p : null;
    }

    // 앱 시작 시(MainLifetimeScope) 1회 등록되는 기본 카탈로그. 모든 BadgeView 가 공용으로 쓴다.
    public static BadgeCatalog Default { get; private set; }
    public static void SetDefault(BadgeCatalog c) => Default = c;

    private void OnValidate() => _map = null;
}
