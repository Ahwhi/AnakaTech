using System.Collections.Generic;
using UnityEngine;

/// <summary>전체 몬스터 정적 데이터 레지스트리. id -> MonsterData 조회.</summary>
[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Anaka/Database/Monster Database")]
public sealed class MonsterDatabase : ScriptableObject {
    [SerializeField] private List<MonsterData> monsters = new();

    private Dictionary<int, MonsterData> _byId;

    public IReadOnlyList<MonsterData> All => monsters;

    public MonsterData Get(int id) {
        EnsureIndex();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, MonsterData>(monsters.Count);
        for (int i = 0; i < monsters.Count; i++) {
            var m = monsters[i];
            if (m == null) continue;
            _byId[m.Id] = m;
        }
    }

    private void OnValidate() => _byId = null;
}
