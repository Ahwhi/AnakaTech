using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 각인 정적 데이터 레지스트리. MainLifetimeScope 인스펙터에 할당해 DI 로 주입한다(규칙 5).
/// 여기 등록된 id 집합은 서버 EngravingCatalog.AllIds 와 일치해야 한다(서버가 도면을 랜덤 지급할 때 쓰는 목록).
/// </summary>
[CreateAssetMenu(fileName = "EngravingDatabase", menuName = "Anaka/Database/Engraving Database")]
public sealed class EngravingDatabase : ScriptableObject {
    [SerializeField] private List<EngravingData> engravings = new();

    private Dictionary<int, EngravingData> _byId;

    public IReadOnlyList<EngravingData> All => engravings;

    public EngravingData Get(int id) {
        EnsureIndex();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, EngravingData>(engravings.Count);
        for (int i = 0; i < engravings.Count; i++) {
            var e = engravings[i];
            if (e == null) continue;
            _byId[e.Id] = e;
        }
    }

    private void OnValidate() => _byId = null;
}
