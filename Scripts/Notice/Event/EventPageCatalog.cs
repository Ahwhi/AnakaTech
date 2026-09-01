using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 이벤트 키(event_key) -> 전용 페이지 프리팹(Addressable) 매핑 카탈로그(ScriptableObject).
/// 문자열 주소를 코드에 박지 않고, 인스펙터에서 AssetReference 로 프리팹을 드래그 할당한다.
/// 새 이벤트를 추가할 때는 페이지 프리팹을 만들어 여기 한 줄(키 + 프리팹)만 등록하면 된다.
/// </summary>
[CreateAssetMenu(fileName = "EventPageCatalog", menuName = "Anaka/Event Page Catalog")]
public sealed class EventPageCatalog : ScriptableObject {
    [Serializable]
    public sealed class Entry {
        [Tooltip("DB notices.event_key 와 일치하는 식별 키.")]
        public string eventKey;
        [Tooltip("이 이벤트의 전용 페이지 프리팹(Addressable).")]
        public AssetReferenceGameObject page;
    }

    [Tooltip("이벤트 키별 페이지 프리팹 목록.")]
    [SerializeField] private List<Entry> entries = new();

    /// <summary>이벤트 키에 해당하는 페이지 프리팹 참조를 찾는다. 없으면 null.</summary>
    public AssetReferenceGameObject Resolve(string eventKey) {
        if (string.IsNullOrEmpty(eventKey)) return null;

        foreach (var entry in entries) {
            if (entry != null && entry.eventKey == eventKey)
                return entry.page;
        }
        return null;
    }
}
