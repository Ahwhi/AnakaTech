using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등급(N/R/SR/SSR/XR)별 "조각(만능조각) 표현"을 매칭하는 카탈로그(SO). GradeColorCatalog 와 동일 패턴으로 등급으로 조회한다.
/// 조각의 시각 표현은 클라 소유 리소스이므로 서버와 무관하게 이 에셋으로만 관리한다.
///
/// 표현이 두 가지인 이유: 보상 슬롯(우편/패키지/충전보상)은 다른 품목과 똑같이 아이콘 한 장으로 그려야 줄이 맞고,
/// 조각 팝업처럼 조각 자체를 크게 보여주는 화면은 연출이 붙은 프리팹이 필요하다.
///
/// 사용: 1) 등급별 아이콘/프리팹을 Entry 에 등록 2) 컴포지션 루트(MainLifetimeScope)가 RegisterInstance 로 등록
///       3) 보상 슬롯은 Icon(grade), 조각 팝업은 Get(grade). 새 등급이 생기면 Entry 만 추가하면 된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Grade Shard Prefab Catalog", fileName = "GradeShardPrefabCatalog")]
public sealed class GradeShardPrefabCatalog : ScriptableObject {
    [Serializable]
    public struct Entry {
        public Grade grade;
        [Tooltip("조각을 크게 보여주는 화면용 프리팹(조각 팝업 등).")]
        public GameObject prefab;
        [Tooltip("보상 슬롯에 아이콘 한 장으로 그릴 때 쓰는 스프라이트.")]
        public Sprite icon;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<Grade, Entry> _map;

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<Grade, Entry>();
        if (entries == null) return;
        foreach (var e in entries) _map[e.grade] = e;
    }

    /// <summary>등급에 해당하는 조각 프리팹(없으면 null).</summary>
    public GameObject Get(Grade grade) {
        EnsureMap();
        return _map.TryGetValue(grade, out var e) ? e.prefab : null;
    }

    /// <summary>등급에 해당하는 조각 아이콘(없으면 null).</summary>
    public Sprite Icon(Grade grade) {
        EnsureMap();
        return _map.TryGetValue(grade, out var e) ? e.icon : null;
    }

    private void OnValidate() => _map = null;
}
