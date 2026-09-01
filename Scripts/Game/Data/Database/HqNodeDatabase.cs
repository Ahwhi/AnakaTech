using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 용병단 본부 노드 레지스트리. MainLifetimeScope 인스펙터에 할당해 DI 로 주입한다(규칙 5).
/// 등록된 노드 id/최대레벨/선행은 서버 HeadquartersCatalog 와 일치해야 한다.
/// </summary>
[CreateAssetMenu(fileName = "HqNodeDatabase", menuName = "Anaka/Database/Hq Node Database")]
public sealed class HqNodeDatabase : ScriptableObject {
    [SerializeField] private List<HqNodeData> nodes = new();

    private Dictionary<int, HqNodeData> _byId;

    public IReadOnlyList<HqNodeData> All => nodes;

    public HqNodeData Get(int id) {
        EnsureIndex();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>보유 레벨 표에 따른 계정 전체 스탯 보너스 합계(전 캐릭터에 동일 적용).</summary>
    public CombatStats TotalBonus(IReadOnlyDictionary<int, int> levelsByNodeId) {
        var sum = new CombatStats();
        if (levelsByNodeId == null) return sum;
        foreach (var kv in levelsByNodeId) {
            var node = Get(kv.Key);
            if (node != null) sum += node.StatsAt(kv.Value);
        }
        return sum;
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, HqNodeData>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++) {
            var n = nodes[i];
            if (n == null) continue;
            _byId[n.Id] = n;
        }
    }

    private void OnValidate() => _byId = null;
}
