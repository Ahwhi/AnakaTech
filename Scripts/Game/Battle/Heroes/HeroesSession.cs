/// <summary>
/// 영웅전 씬으로 넘기는 입장 정보(무한 타워 TowerSession 과 같은 규약). 씬 전환은 인자를 실어 보낼 수 없으므로
/// 진입 직전에 여기 담아 두고 영웅전 씬이 시작할 때 읽어 간다.
///
/// 내보낼 소녀 선택의 창구도 여기다. 팝업을 닫거나 전투를 다녀오거나 앱을 다시 켜도 고른 소녀가 그대로 보여야 하므로
/// 계정별 로컬 세이브(AccountLocalData)에 둔다. 고른 적이 없으면 0 이고, 그때는 대표 소녀가 나선다.
/// </summary>
public static class HeroesSession {
    /// <summary>도전할 단계. 0 이면 입장 정보가 없다.</summary>
    public static int Stage { get; private set; }

    /// <summary>나설 소녀.</summary>
    public static int CharId { get; private set; }

    /// <summary>입장 정보가 준비돼 있는지. 씬을 직접 열어 본 경우(에디터 테스트) false 다.</summary>
    public static bool HasEntry => Stage > 0 && CharId != 0;

    /// <summary>기억해 둔 소녀(고른 적이 없으면 0).</summary>
    public static int RememberedCharId => UserDataManager.Account?.heroesCharId ?? 0;

    public static void SetEntry(int stage, int charId) {
        Stage = stage;
        CharId = charId;
    }

    /// <summary>클리어해 다음 단계로 이어서 칠 때 단계만 바꾼다.</summary>
    public static void AdvanceTo(int stage) => Stage = stage;

    /// <summary>영웅전 씬을 나올 때 비운다(다음 입장이 옛 값을 쓰지 않도록). 소녀 선택은 남긴다.</summary>
    public static void ClearEntry() {
        Stage = 0;
        CharId = 0;
    }

    /// <summary>소녀 선택을 기록한다. 디스크 쓰기는 오토세이버에 맡긴다.</summary>
    public static void RememberChar(int charId) {
        var a = UserDataManager.Account;
        if (a == null || a.heroesCharId == charId) return;
        a.heroesCharId = charId;
        UserDataManager.RequestSave();
    }
}
