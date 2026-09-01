using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등급(N/R/SR/SSR/XR)별 스프라이트를 매칭하는 카탈로그(SO). GradeColorCatalog 와 동일 패턴으로 등급으로 조회한다.
/// 카드 프레임, 배경 패널, 등급 엠블럼처럼 "등급마다 다른 그림"이 필요한 모든 곳이 이 카탈로그를 참조한다.
///
/// 색 카탈로그와 달리 앱 전역 기본값을 두지 않는다. 프레임용/패널용/엠블럼용처럼 용도마다 그림 세트가 다르므로
/// 세트 하나당 에셋 하나를 만들고, 쓰는 쪽(GradeGraphicStyle)이 필요한 세트를 직접 지정한다.
///
/// 사용: 1) Anaka/Grade Sprite Catalog 로 에셋 생성 후 등급별 스프라이트 등록
///       2) 등급별로 그림이 바뀔 이미지 오브젝트에 GradeGraphicStyle 을 붙이고 이 에셋을 지정
/// 새 등급이 생기면 이 에셋에 항목만 추가하면 된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Grade Sprite Catalog", fileName = "GradeSpriteCatalog")]
public sealed class GradeSpriteCatalog : ScriptableObject {
    [Serializable]
    public struct Entry {
        public Grade grade;
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<Grade, Sprite> _map;

    private void OnEnable() => _map = null;    // 에디터에서 항목 수정 시 캐시 무효화
    private void OnValidate() => _map = null;

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<Grade, Sprite>();
        if (entries == null) return;
        foreach (var e in entries) _map[e.grade] = e.sprite;
    }

    /// <summary>등급에 대응하는 스프라이트. 매핑이 없으면 null.</summary>
    public Sprite Get(Grade grade) {
        EnsureMap();
        return _map.TryGetValue(grade, out var sp) ? sp : null;
    }
}
