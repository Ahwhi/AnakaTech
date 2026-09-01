using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리 정적 데이터 레지스트리(키 -> StoryData). LifetimeScope 인스펙터에 할당해 DI 로 주입한다(규칙 5).
/// 여기 등록된 키 집합은 서버 StoryCatalog 의 키 집합과 일치해야 한다.
/// </summary>
[CreateAssetMenu(fileName = "StoryDatabase", menuName = "Anaka/Database/Story Database")]
public sealed class StoryDatabase : ScriptableObject {
    [SerializeField] private List<StoryData> stories = new();

    private Dictionary<string, StoryData> _byKey;

    public IReadOnlyList<StoryData> All => stories;

    public StoryData Get(string key) {
        if (string.IsNullOrEmpty(key)) return null;
        EnsureIndex();
        return _byKey.TryGetValue(key, out var data) ? data : null;
    }

    private void EnsureIndex() {
        if (_byKey != null) return;
        _byKey = new Dictionary<string, StoryData>(stories.Count);
        for (int i = 0; i < stories.Count; i++) {
            var e = stories[i];
            if (e == null || string.IsNullOrEmpty(e.Key)) continue;
            _byKey[e.Key] = e;
        }
    }

    private void OnValidate() => _byKey = null;
}
