using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재화 표시 정보(아이콘/이름) 카탈로그. CurrencyView 가 CurrencyType 만으로 아이콘과 이름을 얻어오게 하는
/// 데이터 소스(규칙 5: 씬/프리팹이 아닌 데이터로 관리). 새 재화가 생기면 이 에셋에 항목만 추가하면 모든
/// 재화 UI 에 자동 반영된다. UIPopupCatalog 와 동일하게 컴포지션 루트가 RegisterInstance 로 등록한다.
///
/// 새 재화 추가 시: 1) CurrencyType 에 값 추가 2) 이 카탈로그 에셋에 Entry 추가 3) UI 에서 타입만 지정.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Currency Catalog", fileName = "CurrencyCatalog")]
public sealed class CurrencyCatalog : ScriptableObject {
    [Serializable]
    public struct Entry {
        public CurrencyType type;
        public string displayName;
        public Sprite icon;
        [Tooltip("재화 설명. 아이콘을 눌렀을 때 뜨는 설명 팝업(CurrencyInfoPopup)의 본문이다. 비우면 팝업이 뜨지 않는다.")]
        [TextArea] public string description;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<CurrencyType, Entry> _map;

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<CurrencyType, Entry>();
        if (entries == null) return;
        foreach (var e in entries) _map[e.type] = e;
    }

    public bool TryGet(CurrencyType type, out Entry entry) {
        EnsureMap();
        return _map.TryGetValue(type, out entry);
    }

    public Sprite Icon(CurrencyType type) => TryGet(type, out var e) ? e.icon : null;

    public string DisplayName(CurrencyType type) =>
        TryGet(type, out var e) && !string.IsNullOrEmpty(e.displayName) ? e.displayName : type.ToString();

    /// <summary>재화 설명(없으면 빈 문자열). 설명이 없는 재화는 설명 팝업을 띄우지 않는다.</summary>
    public string Description(CurrencyType type) =>
        TryGet(type, out var e) && e.description != null ? e.description : "";

#if UNITY_EDITOR
    // 조회용 맵은 한 번 만들고 계속 쓰므로, 에셋을 고쳐도 그 세션 동안 반영되지 않는다
    // (새 재화를 추가했는데 이름/아이콘이 안 나오는 형태로만 드러나 원인을 찾기 어렵다).
    // 인스펙터에서 항목이 바뀌면 버려서 다음 조회에 다시 만들게 한다.
    private void OnValidate() => _map = null;
#endif
}
