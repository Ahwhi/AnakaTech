using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 토벌 의뢰: 제한 시간 안에 목표 수만큼 처치한다.
/// 광역 처리 속도(광역 스킬/스킬가속/이동속도)를 검증하는 컨텐츠라, 큐를 동시 상한까지 계속 채워 넣어
/// 전장이 비지 않게 한다. 상단 게이지는 처치 진행도이며, 시간이 다 되면 실패다.
/// </summary>
public sealed class SubjugationCommissionMode : CommissionMode {
    private Queue<MonsterData> _queue;
    private int _target;
    private int _killed;
    private float _spawnTimer;

    protected override void OnBegin() {
        _queue = new Queue<MonsterData>(Data.BuildShuffledSpawnQueue());
        _target = _queue.Count;
        _killed = 0;
        _spawnTimer = 0f;

        if (_target <= 0) {
            Debug.LogError($"[SubjugationCommissionMode] 등장 몬스터가 없습니다: {Data.name}");
            Result = CommissionResult.Fail;
            return;
        }
        FillSpawns();
    }

    protected override void OnTick(float dt) {
        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        _spawnTimer = Director.SpawnInterval;
        FillSpawns();
    }

    // 동시 상한까지 큐에서 꺼내 스폰한다. 큐가 비면 남은 적을 처치할 때까지 기다린다.
    private void FillSpawns() {
        while (_queue.Count > 0 && Director.ActiveMonsters.Count < Director.MaxConcurrent)
            Director.SpawnCommissionMonster(_queue.Dequeue());
    }

    public override void OnMonsterDied(BattleUnit unit) {
        _killed++;
        if (_killed >= _target) Result = CommissionResult.Success;
    }

    // 주 게이지는 남은 적의 비율이다(가득 찬 상태로 시작해 마지막 한 기를 잡으면 0 이 된다).
    // 레이드의 보스 체력, 호위의 의뢰인 체력과 같은 방향이라 어느 의뢰든 게이지가 비면 상황이 끝난다.
    public override CommissionHudState BuildHudState() {
        int remaining = Mathf.Max(0, _target - _killed);
        float ratio = _target > 0 ? Mathf.Clamp01((float)remaining / _target) : 0f;
        return new CommissionHudState(ratio, $"{_killed} / {_target}", "토벌 진행", TimeRatio, TimeText);
    }
}
