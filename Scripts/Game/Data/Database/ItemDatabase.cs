using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소모품 아이템 정적 데이터 레지스트리. LifetimeScope 인스펙터에 할당해 DI 로 주입한다(규칙 5).
/// 여기 등록된 id 집합은 서버 ItemCatalog 의 id 집합과 일치해야 한다.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Anaka/Database/Item Database")]
public sealed class ItemDatabase : ScriptableObject {
    [SerializeField] private List<ItemData> items = new();

    private Dictionary<int, ItemData> _byId;

    public IReadOnlyList<ItemData> All => items;

    public ItemData Get(int id) {
        EnsureIndex();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, ItemData>(items.Count);
        for (int i = 0; i < items.Count; i++) {
            var e = items[i];
            if (e == null) continue;
            _byId[e.Id] = e;
        }
    }

    private void OnValidate() => _byId = null;
}
