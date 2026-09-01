using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 상품 이미지 카탈로그. 서버 오퍼의 ImageKey(문자열)로 스프라이트를 조회한다(CurrencyCatalog 와 동일 패턴).
/// 상품 이미지는 표현 리소스라 클라가 소유하며, 서버는 키만 내려준다. 새 상품 이미지가 생기면 이 에셋에 Entry 만 추가하면 된다.
///
/// 사용: 1) 서버 오퍼의 ImageKey 지정 2) 이 카탈로그 에셋에 같은 key + 스프라이트 Entry 추가 3) 컴포지션 루트가 RegisterInstance 로 등록.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Shop Icon Catalog", fileName = "ShopIconCatalog")]
public sealed class ShopIconCatalog : ScriptableObject {
    [Serializable]
    public struct Entry {
        public string key;      // 서버 오퍼의 ImageKey 와 일치
        public Sprite icon;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<string, Sprite> _map;

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<string, Sprite>();
        if (entries == null) return;
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.key)) _map[e.key] = e.icon;
    }

    /// <summary>ImageKey 에 해당하는 상품 스프라이트(없으면 null).</summary>
    public Sprite Get(string key) {
        if (string.IsNullOrEmpty(key)) return null;
        EnsureMap();
        return _map.TryGetValue(key, out var s) ? s : null;
    }

    private void OnValidate() => _map = null;
}
