using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 의뢰 단계 데이터 레지스트리. (종류, 단계)로 조회한다. MainLifetimeScope / CommissionLifetimeScope 인스펙터에
/// 할당해 DI 로 주입한다(규칙 5).
///
/// 단계가 데이터로 존재하지 않으면 그 단계는 도전할 수 없다(서버는 MaxStage 까지 허용하지만 실제 전장을 만들 수 없으므로,
/// 클라가 목록에서 막는다). 새 단계를 열려면 에셋만 추가하면 되고 코드 수정은 없다.
/// </summary>
[CreateAssetMenu(fileName = "CommissionDatabase", menuName = "Anaka/Database/Commission Database")]
public sealed class CommissionDatabase : ScriptableObject {
    [SerializeField] private List<CommissionData> commissions = new();

    private Dictionary<int, CommissionData> _byKey;

    public IReadOnlyList<CommissionData> All => commissions;

    public CommissionData Get(CommissionType type, int stage) {
        EnsureIndex();
        return _byKey.TryGetValue(Key(type, stage), out var data) ? data : null;
    }

    /// <summary>해당 종류에 데이터가 존재하는 최고 단계(0=없음). 목록에서 "최대 단계 도달" 표시에 쓴다.</summary>
    public int HighestStageOf(CommissionType type) {
        int max = 0;
        for (int i = 0; i < commissions.Count; i++) {
            var c = commissions[i];
            if (c != null && c.Type == type && c.Stage > max) max = c.Stage;
        }
        return max;
    }

    private void EnsureIndex() {
        if (_byKey != null) return;
        _byKey = new Dictionary<int, CommissionData>(commissions.Count);
        for (int i = 0; i < commissions.Count; i++) {
            var c = commissions[i];
            if (c == null) continue;
            _byKey[Key(c.Type, c.Stage)] = c;
        }
    }

    // 종류/단계를 하나의 정수 키로 합친다(단계는 1~MaxStage 라 1000 배수면 충돌하지 않는다).
    private static int Key(CommissionType type, int stage) => (int)type * 1000 + stage;

    private void OnValidate() => _byKey = null;
}
