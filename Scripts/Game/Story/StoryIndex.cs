using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리 목록 표시용 경량 카탈로그. 스토리 팝업의 카드가 쓰는 "제목 + 대표 이미지"만 담는다.
///
/// StoryDatabase 를 쓰지 않는 이유가 있다. StoryData 는 대사마다 배경과 화자 초상 스프라이트를 물고 있어,
/// 메인 씬에서 그 에셋을 참조하면 19편의 연출 이미지가 전부 메모리에 올라온다. 목록에 필요한 것은
/// 편당 그림 한 장이라, 내용과 목록을 분리해 두면 메인 씬이 가벼워진다.
///
/// 대사 내용은 그대로 StoryDatabase(스토리 씬)가 갖는다. 이 에셋에는 내용이 없다.
/// </summary>
[CreateAssetMenu(fileName = "StoryIndex", menuName = "Anaka/Data/Story Index")]
public sealed class StoryIndex : ScriptableObject {
    [Serializable]
    public struct Entry {
        [Tooltip("장 번호(1~21). 서버가 내려 주는 chapter_N 의 N 이다.")]
        [Min(1)] public int chapter;

        [Tooltip("카드에 표시할 제목(예: 도심).")]
        public string title;

        [Tooltip("카드의 대표 이미지. 비우면 이미지 없이 그려진다.")]
        public Sprite thumbnail;
    }

    [Tooltip("편 목록. 순서는 상관없다(chapter 값으로 찾는다).")]
    [SerializeField] private List<Entry> entries = new();

    private Dictionary<int, Entry> _map;

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<int, Entry>(entries.Count);
        foreach (var e in entries) {
            // 같은 장이 둘이면 앞의 것을 남긴다. 조용히 덮어쓰면 어느 쪽이 쓰였는지 알 수 없다.
            if (_map.ContainsKey(e.chapter)) {
                Debug.LogWarning($"[StoryIndex] 장 번호가 겹칩니다: {e.chapter}", this);
                continue;
            }
            _map[e.chapter] = e;
        }
    }

    public bool TryGet(int chapter, out Entry entry) {
        EnsureMap();
        return _map.TryGetValue(chapter, out entry);
    }

    /// <summary>카드 제목. 등록되지 않은 장은 "N장" 으로 떨어진다(빈 칸보다 낫다).</summary>
    public string Title(int chapter) =>
        TryGet(chapter, out var e) && !string.IsNullOrEmpty(e.title) ? e.title : StoryRules.ChapterLabel(chapter);

    public Sprite Thumbnail(int chapter) => TryGet(chapter, out var e) ? e.thumbnail : null;
}
