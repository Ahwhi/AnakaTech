using System.Collections.Generic;

/// <summary>
/// 원정대 씬으로 넘기는 입장 정보(길드 레이드 GuildRaidSession 과 같은 규약). 씬 전환은 인자를 실어 보낼 수 없으므로
/// 진입 직전에 여기 담아 두고 원정대 씬이 시작할 때 읽어 간다.
///
/// 장소 체력은 "입장 시점의 값"이다. 다른 파티원이 그 사이 깎을 일은 점유(서버 잠금)로 막혀 있지만,
/// 실제 반영은 어디까지나 보고를 받은 서버가 현재 체력에서 다시 뺀다. 여기 값은 게이지의 시작점일 뿐이다.
/// </summary>
public static class ExpeditionSession {
    public static ExpeditionLocation Location { get; private set; }
    public static int Level { get; private set; }
    public static long Hp { get; private set; }
    public static long MaxHp { get; private set; }

    /// <summary>이번 주 내게 묶인 캐릭터. 내 파티로 이 캐릭터 1명만 선다(지금 대표가 바뀌었어도 이 캐릭터다).</summary>
    public static int MyCharDataId { get; private set; }

    /// <summary>함께 싸울 파티원(나 제외). 편성은 씬이 슬롯으로 서버 스냅샷을 받아 재현한다.</summary>
    public static IReadOnlyList<ExpeditionMemberDto> Teammates => _teammates;
    private static readonly List<ExpeditionMemberDto> _teammates = new();

    /// <summary>입장 정보가 준비돼 있는지. 씬을 직접 열어 본 경우(에디터 테스트) false 다.</summary>
    public static bool HasEntry => MaxHp > 0 && MyCharDataId > 0;

    public static void Set(ExpeditionEnterDto entry, ExpeditionHomeDto home) {
        Location = entry.Location;
        Level = entry.Level;
        Hp = entry.Hp;
        MaxHp = entry.MaxHp;
        MyCharDataId = home.Me?.CharDataId ?? 0;

        _teammates.Clear();
        foreach (var m in home.Members)
            if (!(m.AccountId == home.MyAccountId && !m.Mercenary)) _teammates.Add(m);
    }

    /// <summary>원정대 씬을 나올 때 비운다(다음 입장이 옛 값을 쓰지 않도록).</summary>
    public static void Clear() {
        Level = 0;
        Hp = 0;
        MaxHp = 0;
        MyCharDataId = 0;
        _teammates.Clear();
    }
}
