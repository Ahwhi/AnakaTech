/// <summary>
/// "공유 체력을 제한 시간 안에 깎는" 전투(길드 레이드, 원정대)가 HUD 에 내주는 값.
/// HUD(GuildRaidHudController)는 이 계약만 보므로, 같은 HUD 프리팹을 어느 컨텐츠 씬에서든 그대로 쓴다.
/// </summary>
public interface IBossGaugeSource {
    /// <summary>상단 게이지 비율(1 -> 0). 끝난 뒤에는 종료 시점 값을 유지한다.</summary>
    float BossHpRatio { get; }
    /// <summary>게이지 위에 쓸 이름(보스 이름 / 장소 이름).</summary>
    string BossName { get; }
    /// <summary>레벨 표기(예: "Lv.3").</summary>
    string BossLevelText { get; }
    /// <summary>남은 시간 비율(1 -> 0).</summary>
    float TimeRatio { get; }
    string TimeText { get; }
    /// <summary>이번 도전에서 지금까지 깎은 피해량.</summary>
    long DealtDamage { get; }
}
