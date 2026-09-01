/// <summary>
/// 레이드 씬으로 넘기는 입장 정보. 씬 전환은 인자를 실어 보낼 수 없으므로, 진입 직전에 여기 담아 두고
/// 레이드 씬이 시작할 때 읽어 간다(의뢰 씬이 쓰는 방식과 같은 규약).
///
/// 값은 "입장 시점의 보스"다. 다른 길드원이 그 사이 체력을 깎을 수 있으므로 절대적인 근거가 아니며,
/// 실제 반영은 도전이 끝난 뒤 서버가 현재 체력에서 다시 뺀다(ReportRaidDamageAsync).
/// 여기 값은 씬에서 체력바를 그리기 위한 시작점일 뿐이다.
/// </summary>
public static class GuildRaidSession {
    public static int BossLevel { get; private set; } = 1;
    public static long BossHp { get; private set; }
    public static long BossMaxHp { get; private set; }

    /// <summary>입장 정보가 준비돼 있는지. 씬을 직접 열어 본 경우(에디터 테스트) false 다.</summary>
    public static bool HasEntry => BossMaxHp > 0;

    public static void Set(int bossLevel, long bossHp, long bossMaxHp) {
        BossLevel = bossLevel;
        BossHp = bossHp;
        BossMaxHp = bossMaxHp;
    }

    /// <summary>레이드 씬을 나올 때 비운다(다음 입장이 옛 값을 쓰지 않도록).</summary>
    public static void Clear() {
        BossLevel = 1;
        BossHp = 0;
        BossMaxHp = 0;
    }
}
