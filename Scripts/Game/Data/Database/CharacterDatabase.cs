using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 캐릭터 정적 데이터 레지스트리. MainLifetimeScope 인스펙터에 할당해 DI 로 주입한다(키 방식 아님, 규칙 5).
/// id -> CharacterData 조회를 제공한다.
/// </summary>
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Anaka/Database/Character Database")]
public sealed class CharacterDatabase : ScriptableObject {
    [SerializeField] private List<CharacterData> characters = new();

    private Dictionary<int, CharacterData> _byId;

    public IReadOnlyList<CharacterData> All => characters;

    public CharacterData Get(int id) {
        EnsureIndex();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    public bool TryGet(int id, out CharacterData data) {
        EnsureIndex();
        return _byId.TryGetValue(id, out data);
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, CharacterData>(characters.Count);
        for (int i = 0; i < characters.Count; i++) {
            var c = characters[i];
            if (c == null) continue;
            _byId[c.Id] = c;
        }
    }

    // 도메인 데이터가 에디터에서 바뀌면 캐시를 무효화(플레이 중 재빌드 대비).
    private void OnValidate() => _byId = null;
}
