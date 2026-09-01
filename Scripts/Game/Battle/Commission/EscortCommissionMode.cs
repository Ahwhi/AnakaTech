using UnityEngine;

/// <summary>
/// 호위 의뢰: 전장 중앙의 의뢰인을 지키며 제한 시간을 버틴다.
/// 방어/생존(방어력, 피격데미지감소, 치명타저항)과 분산 대응을 검증하는 컨텐츠다.
///
/// 다른 두 의뢰와 반대로 "시간이 다 되면 성공"이며, 실패 조건이 둘이다(파티 전멸 또는 의뢰인 사망).
/// 몬스터는 끝없이 웨이브로 들어오고, 그중 돌격병(MonsterData.Targeting = PriorityTarget)은 캐릭터를 무시하고
/// 의뢰인에게 직행한다. 그래서 딜러만 편성하면 돌격병을 끊지 못해 실패한다.
/// </summary>
public sealed class EscortCommissionMode : CommissionMode {
    // 의뢰인의 마스크 정렬 슬롯. 캐릭터(0..4)와 몬스터 대역(100~) 어디와도 겹치지 않는 값.
    private const int EscortMaskSlot = 90;

    private EscortUnit _escort;
    private float _spawnTimer;

    public override BattleUnit PriorityTarget => _escort != null && _escort.IsAlive ? _escort : null;

    protected override void OnBegin() {
        if (Data.EscortPrefab == null) {
            Debug.LogError($"[EscortCommissionMode] 의뢰인 프리팹(EscortPrefab)이 없습니다: {Data.name}");
            Result = CommissionResult.Fail;
            return;
        }

        _escort = Director.SpawnEscort(Data.EscortPrefab, Data.EscortHealth, Data.EscortDefense, EscortMaskSlot);
        if (_escort == null) {
            Result = CommissionResult.Fail;
            return;
        }

        _spawnTimer = 0f;
        FillWave();
    }

    protected override void OnTick(float dt) {
        // 의뢰인이 쓰러지면 즉시 실패(전멸과 별개의 실패 조건).
        if (_escort == null || !_escort.IsAlive) {
            Result = CommissionResult.Fail;
            return;
        }

        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        _spawnTimer = Director.SpawnInterval;
        FillWave();
    }

    // 웨이브는 끝이 없다. 동시 상한까지 가중치로 뽑아 채운다(토벌과 달리 소진되는 큐가 없다).
    private void FillWave() {
        while (Director.ActiveMonsters.Count < Director.MaxConcurrent) {
            var monster = Data.PickWeightedMonster();
            if (monster == null) return;   // 스폰 목록이 비어 있으면 더 시도하지 않는다(무한 루프 방지).
            Director.SpawnCommissionMonster(monster);
        }
    }

    /// <summary>버티는 컨텐츠이므로 제한 시간을 다 채우면 성공이다.</summary>
    protected override CommissionResult OnTimeExpired() => CommissionResult.Success;

    public override CommissionHudState BuildHudState() {
        float ratio = _escort != null && _escort.IsAlive ? Mathf.Clamp01(_escort.HpRatio) : 0f;
        return new CommissionHudState(ratio, $"{ratio * 100f:0.0}%", "의뢰인", TimeRatio, TimeText);
    }
}
