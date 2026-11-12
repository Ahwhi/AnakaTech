using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 코스튬 정적 데이터 레지스트리. MainLifetimeScope 인스펙터에 할당해 DI 로 주입한다(키 방식 아님, 규칙 5).
/// id -> CostumeData 조회와, "이 캐릭터가 가질 수 있는 코스튬 목록" 조회를 제공한다.
///
/// 캐릭터마다 코스튬이 없을 수도 여럿일 수도 있으므로 캐릭터별 목록은 이 표에서 도출한다(비어 있어도 정상).
/// 새 코스튬은 CostumeData 를 만들어 여기 목록에 추가하기만 하면 획득/장착/전투 반영이 모두 자동으로 열린다.
/// </summary>
[CreateAssetMenu(fileName = "CostumeDatabase", menuName = "Anaka/Database/Costume Database")]
public sealed class CostumeDatabase : ScriptableObject {
    [SerializeField] private List<CostumeData> costumes = new();
    [Tooltip("아웃핏(전투 유닛 프리팹 교체) 데이터. 꾸미기 데이터라 코스튬과 같은 경로로 주입되도록 여기 둔다(씬마다 배선하지 않는다).")]
    [SerializeField] private UnitSkinCatalog unitSkins;

    private Dictionary<int, CostumeData> _byId;

    /// <summary>아웃핏 데이터(미할당이면 null: 아웃핏 없이 기본 유닛만 쓴다).</summary>
    public UnitSkinCatalog UnitSkins => unitSkins;
    private Dictionary<int, List<CostumeData>> _byCharacter;

    public IReadOnlyList<CostumeData> All => costumes;

    public CostumeData Get(int id) {
        if (id == 0) return null;
        EnsureIndex();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    public bool TryGet(int id, out CostumeData data) {
        EnsureIndex();
        return _byId.TryGetValue(id, out data);
    }

    /// <summary>이 캐릭터의 코스튬 목록(등록 순서 유지). 코스튬이 없는 캐릭터는 빈 목록이다.</summary>
    public IReadOnlyList<CostumeData> ForCharacter(int charDataId) {
        EnsureIndex();
        return _byCharacter.TryGetValue(charDataId, out var list) ? list : System.Array.Empty<CostumeData>();
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, CostumeData>(costumes.Count);
        _byCharacter = new Dictionary<int, List<CostumeData>>();
        for (int i = 0; i < costumes.Count; i++) {
            var c = costumes[i];
            if (c == null) continue;
            _byId[c.Id] = c;

            int owner = c.CharacterId;
            if (owner == 0) continue;
            if (!_byCharacter.TryGetValue(owner, out var list)) {
                list = new List<CostumeData>();
                _byCharacter[owner] = list;
            }
            list.Add(c);
        }
    }

    // 도메인 데이터가 에디터에서 바뀌면 캐시를 무효화(플레이 중 재빌드 대비).
    private void OnValidate() {
        _byId = null;
        _byCharacter = null;
    }
}
