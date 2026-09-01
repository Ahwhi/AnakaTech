using System;
using UnityEngine;

/// <summary>
/// 길드 마크 id -> 스프라이트 매핑 에셋. 마크 id 는 서버 GuildCatalog 가 정하고(1~FreeMarkMaxId 무료, 그 위는 유료)
/// 이 에셋은 그 id 를 그림으로 옮기기만 한다(ProfileFrameCatalog 와 같은 패턴).
///
/// 마크를 늘릴 때: 서버 GuildCatalog 의 상한을 올리고 여기 목록에 같은 id 로 스프라이트를 추가한다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Guild Mark Catalog", fileName = "GuildMarkCatalog")]
public sealed class GuildMarkCatalog : ScriptableObject {
    [Serializable]
    public sealed class Entry {
        [Tooltip("서버와 약속된 마크 id. 1~5 는 무료, 6 이상은 다이아 해금 대상.")]
        public int markId = 1;
        public Sprite icon;
        [Tooltip("마크 선택 화면에 표시할 이름(선택).")]
        public string displayName = "";
    }

    [Tooltip("마크 목록. id 순서대로 넣어 두면 선택 화면도 그 순서로 나온다.")]
    [SerializeField] private Entry[] entries;

    [Tooltip("id 를 못 찾았을 때 쓸 대체 아이콘(비우면 표시 없음).")]
    [SerializeField] private Sprite fallbackIcon;

    /// <summary>등록된 마크 목록(선택 화면이 그대로 순회한다).</summary>
    public Entry[] Entries => entries ?? Array.Empty<Entry>();

    public Sprite Icon(int markId) {
        foreach (var e in Entries)
            if (e != null && e.markId == markId) return e.icon != null ? e.icon : fallbackIcon;
        return fallbackIcon;
    }

    public string DisplayName(int markId) {
        foreach (var e in Entries)
            if (e != null && e.markId == markId && !string.IsNullOrEmpty(e.displayName)) return e.displayName;
        return $"마크 {markId}";
    }
}
