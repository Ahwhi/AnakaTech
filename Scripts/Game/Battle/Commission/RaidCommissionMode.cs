using UnityEngine;

/// <summary>
/// 레이드 의뢰: 보스 1기를 제한 시간 안에 쓰러뜨린다.
/// 단일 대상 화력(공격력/치명타/방어관통/최종데미지)을 검증하는 컨텐츠라 잡몹을 섞지 않는다.
/// 상단 게이지는 보스 체력이며, 시간이 다 되면 실패다.
/// </summary>
public sealed class RaidCommissionMode : CommissionMode {
    private BattleUnit _boss;
    private string _bossName = "";

    protected override void OnBegin() {
        var bossData = Data.Boss;
        if (bossData == null) {
            // 데이터 누락을 조용히 삼키면 "아무 일도 안 일어나는 의뢰"가 되므로 즉시 드러낸다.
            Debug.LogError($"[RaidCommissionMode] 보스 몬스터가 없습니다: {Data.name}");
            Result = CommissionResult.Fail;
            return;
        }

        _bossName = bossData.DisplayName;
        // 보스는 1기뿐이고 그 체력을 상단 주 게이지가 그대로 보여 주므로 머리 위 체력바는 붙이지 않는다.
        _boss = Director.SpawnCommissionMonster(bossData, showHealthBar: false);
        if (_boss == null) Result = CommissionResult.Fail;
    }

    protected override void OnTick(float dt) {
        // 보스가 사라졌다(=처치됐다)면 성공. 파괴 지연이 있어 null 체크와 생존 체크를 함께 본다.
        if (_boss == null || !_boss.IsAlive) Result = CommissionResult.Success;
    }

    public override CommissionHudState BuildHudState() {
        float ratio = _boss != null && _boss.IsAlive ? Mathf.Clamp01(_boss.HpRatio) : 0f;
        return new CommissionHudState(ratio, $"{ratio * 100f:0.0}%", _bossName, TimeRatio, TimeText);
    }
}
