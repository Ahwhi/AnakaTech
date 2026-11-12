/// <summary>
/// 전투 완주 보고(C_BattleFinished)의 컨텐츠 구분. 값은 서버 Server.Data.BattleFinishContent 와 같아야 한다.
///
/// 멸망전/길드 레이드는 도중에 나가도 그때까지의 점수/피해를 보고하므로, 결과 보고만으로는 끝까지 쳤는지 알 수 없다.
/// 그래서 끝까지 친 판에서만(나가기 경로가 아닌 전투 종료 경로에서만) 이 보고를 한 번 더 보낸다.
/// 이벤트 일일 미션(멸망전/길드 레이드 1회 하기)이 이 보고로만 인정된다.
/// </summary>
public static class BattleFinishContent {
    public const int Doom = 1;
    public const int GuildRaid = 2;
    public const int Tower = 3;
}
