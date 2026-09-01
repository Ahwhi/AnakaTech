/// <summary>
/// 유저 대 유저 전투의 진행 규칙. 컨텐츠(길드 대항전/향후 아레나)에 묶이지 않는 값만 둔다.
/// 승패 판정은 클라 전투가 내리고 서버는 그 결과를 받아 반영한다(기존 전투 결과와 같은 신뢰 경계).
/// </summary>
public static class PvpBattleRules {
    /// <summary>한 판의 제한 시간(초). 시간이 다 되면 공격자가 이기지 못한 것으로 보고 방어 성공 처리한다.</summary>
    public const int BattleSeconds = 90;
}
