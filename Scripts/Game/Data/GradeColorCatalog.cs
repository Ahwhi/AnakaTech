using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등급(N/R/SR/SSR/XR)별 텍스트 색을 매칭하는 카탈로그(SO). 등급을 텍스트로 출력하는 모든 곳(GradeText)이
/// 이 색을 참조한다. 새 등급이 생기면 이 에셋에 항목만 추가하면 모든 등급 표시에 자동 반영된다(규칙 5).
///
/// 사용: 등급 텍스트 오브젝트에 GradeText 컴포넌트를 붙이고 applyColor 를 체크하면 이 카탈로그 색이 적용된다.
/// 카탈로그는 앱 시작 시 GradeText 의 기본값으로 1회 등록되므로, 개별 프리팹마다 SO 를 다시 지정하지 않아도 된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Grade Color Catalog", fileName = "GradeColorCatalog")]
public sealed class GradeColorCatalog : ScriptableObject {
    [Serializable]
    public struct Entry {
        public Grade grade;
        public Color color;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<Grade, Color> _map;

    private void OnEnable() => _map = null;   // 에디터에서 항목 수정 시 캐시 무효화

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<Grade, Color>();
        if (entries == null) return;
        foreach (var e in entries) _map[e.grade] = e.color;
    }

    public bool TryGetColor(Grade grade, out Color color) {
        EnsureMap();
        return _map.TryGetValue(grade, out color);
    }
}
