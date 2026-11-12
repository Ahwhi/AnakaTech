using System.Collections.Generic;

/// <summary>무한 타워 편성의 출처. 팝업의 드롭다운 항목과 1:1이다.</summary>
public enum TowerDeckSource {
    /// <summary>메인 스쿼드(덱 팝업에서 편성한 지금 덱 그대로).</summary>
    Current,
    /// <summary>저장해 둔 편성 프리셋.</summary>
    Preset,
    /// <summary>이 화면에서 직접 끌어다 짠 편성.</summary>
    Custom,
}

/// <summary>
/// 무한 타워 씬으로 넘기는 입장 정보(원정대 ExpeditionSession 과 같은 규약). 씬 전환은 인자를 실어 보낼 수 없으므로
/// 진입 직전에 여기 담아 두고 타워 씬이 시작할 때 읽어 간다.
///
/// 편성 선택(출처 + 커스텀 편성)의 창구도 여기다. 팝업을 닫거나 전투를 다녀오거나 앱을 다시 켜도 고른 편성이
/// 그대로 보여야 하므로 계정별 로컬 세이브(AccountLocalData)에 둔다. 로그인 전이면 기본값(메인 스쿼드)으로 읽힌다.
/// </summary>
public static class TowerSession {
    /// <summary>오를 층. 0 이면 입장 정보가 없다.</summary>
    public static int Floor { get; private set; }

    public static TowerBonus Bonus { get; private set; }

    /// <summary>합류 순번(앞에서부터 1, 2, 3, 4, 5번).</summary>
    public static IReadOnlyList<int> Members => _members;
    private static readonly List<int> _members = new();

    /// <summary>입장 정보가 준비돼 있는지. 씬을 직접 열어 본 경우(에디터 테스트) false 다.</summary>
    public static bool HasEntry => Floor > 0 && _members.Count > 0;

    // 팝업 복원용 편성 선택. 입장 정보와 달리 씬을 나와도 지우지 않는다.
    public static TowerDeckSource DeckSource {
        get {
            var a = UserDataManager.Account;
            return a != null && System.Enum.IsDefined(typeof(TowerDeckSource), a.towerDeckSource)
                ? (TowerDeckSource)a.towerDeckSource : TowerDeckSource.Current;
        }
    }

    public static int PresetSlot => UserDataManager.Account?.towerPresetSlot ?? -1;

    public static IReadOnlyList<int> CustomMembers =>
        (IReadOnlyList<int>)UserDataManager.Account?.towerCustomMembers ?? System.Array.Empty<int>();

    public static void SetEntry(int floor, TowerBonus bonus, IReadOnlyList<int> members) {
        Floor = floor;
        Bonus = bonus;
        Copy(members, _members);
    }

    /// <summary>타워 씬을 나올 때 비운다(다음 입장이 옛 값을 쓰지 않도록). 편성 선택은 남긴다.</summary>
    public static void ClearEntry() {
        Floor = 0;
        Bonus = default;
        _members.Clear();
    }

    /// <summary>편성 선택을 기록한다. 커스텀이 아니면 커스텀 편성은 남기지 않는다. 디스크 쓰기는 오토세이버에 맡긴다.</summary>
    public static void RememberDeck(TowerDeckSource source, int presetSlot, IReadOnlyList<int> customMembers) {
        var a = UserDataManager.Account;
        if (a == null) return;

        a.towerDeckSource = (int)source;
        a.towerPresetSlot = source == TowerDeckSource.Preset ? presetSlot : -1;
        a.towerCustomMembers ??= new List<int>();
        Copy(source == TowerDeckSource.Custom ? customMembers : null, a.towerCustomMembers);
        UserDataManager.RequestSave();
    }

    private static void Copy(IReadOnlyList<int> from, List<int> to) {
        to.Clear();
        if (from == null) return;
        for (int i = 0; i < from.Count; i++) to.Add(from[i]);
    }
}
