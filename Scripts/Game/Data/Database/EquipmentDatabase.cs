using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 장비 정적 데이터 레지스트리. id -> EquipmentData 조회.
///
/// 서버는 이 DB 를 모르고 id 규약(Server.Data.GameRoster)만 안다. 따라서 서버가 지급한 장비의 SO 가
/// 이 목록에 없으면 그 장비는 UI 에서 조용히 사라진다(보유 중인데 안 보이는 사고). 그런 id 를 만나면
/// 에디터/개발 빌드에서 1회 경고를 남겨 SO 누락을 바로 알 수 있게 한다.
/// </summary>
[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Anaka/Database/Equipment Database")]
public sealed class EquipmentDatabase : ScriptableObject {
    [SerializeField] private List<EquipmentData> equipments = new();

    private Dictionary<int, EquipmentData> _byId;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 같은 id 로 매 프레임 경고가 쏟아지지 않도록 이미 경고한 id 를 기억한다.
    private HashSet<int> _warnedMissing;
#endif

    public IReadOnlyList<EquipmentData> All => equipments;

    public EquipmentData Get(int id) {
        EnsureIndex();
        if (_byId.TryGetValue(id, out var data)) return data;
        WarnMissing(id);
        return null;
    }

    private void WarnMissing(int id) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _warnedMissing ??= new HashSet<int>();
        if (!_warnedMissing.Add(id)) return;
        bool signature = EquipmentRules.IsSignature(id);
        string hint = signature
            ? $"전용무기 SO 가 없습니다(주인 캐릭터 id {EquipmentRules.OwnerCharOf(id)}). EquipmentData 를 만들어 EquipmentDatabase 에 등록하세요."
            : "EquipmentData 를 만들어 EquipmentDatabase 에 등록하세요.";
        Debug.LogWarning($"[EquipmentDatabase] 등록되지 않은 장비 id {id} - 보유해도 UI 에 표시되지 않습니다. {hint}");
#endif
    }

    private void EnsureIndex() {
        if (_byId != null) return;
        _byId = new Dictionary<int, EquipmentData>(equipments.Count);
        for (int i = 0; i < equipments.Count; i++) {
            var e = equipments[i];
            if (e == null) continue;
            if (_byId.ContainsKey(e.Id))
                Debug.LogWarning($"[EquipmentDatabase] 장비 id 중복: {e.Id} ({e.name}). 뒤 항목이 앞을 덮어씁니다.");
            _byId[e.Id] = e;
        }
    }

    private void OnValidate() => _byId = null;
}
