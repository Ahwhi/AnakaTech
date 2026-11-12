/// <summary>
/// 유저 대 유저 전투의 진행 규칙. 컨텐츠(길드 대항전/쟁탈전)마다 다른 값은 Tuning 으로 묶어 TuningOf 가 고른다.
/// 승패 판정은 클라 전투가 내리고 서버는 그 결과를 받아 반영한다(기존 전투 결과와 같은 신뢰 경계).
///
/// 쟁탈전의 스탯 보정은 V1.12 부터 고정 체력 배수가 아니라 양 팀 각자의 룬(ArenaRuneRules)이 맡는다.
/// </summary>
public static class PvpBattleRules {
    /// <summary>한 컨텐츠의 전투 조건. 시간이 다 되면 공격자가 이기지 못한 것으로 보고 방어 성공 처리한다.</summary>
    public readonly struct Tuning {
        public readonly int BattleSeconds;

        public Tuning(int battleSeconds) {
            BattleSeconds = battleSeconds;
        }
    }

    /// <summary>기본(길드 대항전 등). 90초.</summary>
    public static readonly Tuning Default = new(90);

    /// <summary>
    /// 쟁탈전. 룬(특히 체력 룬)으로 판이 길어지므로 시간을 줄여 늘어지지 않게 한다.
    /// </summary>
    public static readonly Tuning Arena = new(30);

    /// <summary>진입한 컨텐츠의 전투 조건.</summary>
    public static Tuning TuningOf(PvpBattleSession.OriginType origin) =>
        origin == PvpBattleSession.OriginType.Arena ? Arena : Default;
}
