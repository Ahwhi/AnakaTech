/// <summary>
/// 메인 퀘스트 표시 규칙(클라 전용). 퀘스트의 순서/조건/보상은 전부 서버가 정하므로 여기에는 정의가 없고,
/// 서버가 내려 준 목표(objective) + 인자(param)를 사람이 읽는 문구로 바꾸는 일만 한다.
///
/// 문구를 패킷으로 내려받지 않는 이유는 같은 목표가 여러 레벨에서 반복되기 때문이다(모집/의뢰/쟁탈전 등).
/// 문구를 클라가 만들면 표현을 고치는 데 서버 배포가 필요 없다.
/// </summary>
public static class MainQuestRules {
    /// <summary>
    /// 퀘스트 목표 안내 문구. 스테이지는 던전 이름(예: 1-5)이 필요하므로 데이터베이스를 함께 받는다.
    /// 등록되지 않은 던전이면 인덱스를 그대로 보여 준다(문구가 비어 화면이 텅 비는 것보다 낫다).
    /// </summary>
    public static string Describe(MainQuestObjective objective, int param, DungeonDatabase dungeonDb) => objective switch {
        MainQuestObjective.StageClear => $"스테이지 {StageName(param, dungeonDb)} 클리어",
        MainQuestObjective.CharacterGacha => "소녀 모집 1회",
        MainQuestObjective.DeckChange => "스쿼드 구성 해보기",
        MainQuestObjective.StatAllocate => "스탯 올리기",
        MainQuestObjective.AutoStatSet => "스탯 자동 투자 설정하기",
        MainQuestObjective.EquipGacha => "장비 모집 1회",
        MainQuestObjective.EquipEquip => "장비 장착 해보기",
        MainQuestObjective.EquipEnhance => "장비 강화 해보기",
        MainQuestObjective.StaminaBoost => "기력 부스트 켜보기",
        MainQuestObjective.AutoBattleToggle => "자동 전투 모드 바꿔보기",
        MainQuestObjective.Training => "소녀 수련 하기",
        MainQuestObjective.CommissionRaid => "레이드 의뢰 1회 도전",
        MainQuestObjective.CommissionSubjugation => "토벌 의뢰 1회 클리어",
        MainQuestObjective.EngravingEquip => "각인 장착 해보기",
        MainQuestObjective.CommissionEscort => "호위 의뢰 1회 클리어",
        MainQuestObjective.HeadquartersUpgrade => "본부 훈련 1회",
        MainQuestObjective.FriendMade => "친구 1명 만들기",
        MainQuestObjective.ArenaEntry => "쟁탈전 1회",
        MainQuestObjective.GuildJoin => "길드 가입 하기",
        MainQuestObjective.CharacterStarUp => "캐릭터 별레벨 승급 1회",
        MainQuestObjective.EquipAwaken => "장비 각성 1회",
        MainQuestObjective.CharacterItemUse => "캐릭터 아이템 사용하기",
        MainQuestObjective.GuildRaidEntry => "길드 레이드 1회",
        MainQuestObjective.NicknameChange => "닉네임 변경 하기",
        MainQuestObjective.StageSweep => "스테이지 소탕 1회",
        MainQuestObjective.Dispatch => "파견 1회 보내기",
        _ => "메인 퀘스트"
    };

    /// <summary>퀘스트 레벨 표시(예: 메인 퀘스트 Lv.7).</summary>
    public static string LevelLabel(int level) => $"메인 퀘스트 Lv.{level}";

    /// <summary>진행도 표시(예: 0/1). 목표가 1회뿐이어도 표시해 두면 여러 번짜리 퀘스트가 생겨도 화면이 그대로다.</summary>
    public static string ProgressLabel(int progress, int threshold) {
        int max = threshold < 1 ? 1 : threshold;
        int cur = progress < 0 ? 0 : (progress > max ? max : progress);
        return $"{cur}/{max}";
    }

    /// <summary>준비된 퀘스트를 전부 끝냈을 때의 문구.</summary>
    public const string AllClearedMessage = "모든 메인 퀘스트를 완료했습니다.";

    /// <summary>완료 알림 토스트 문구(다른 화면에 있어도 이 문구로 알린다).</summary>
    public const string CompletedToast = "메인 퀘스트가 완료되었습니다.";

    /// <summary>수령 실패 사유(서버 MainQuestRepository.ClaimFail) -> 안내 문구.</summary>
    public static string ClaimFailMessage(int fail) => fail switch {
        1 => AllClearedMessage,
        2 => "퀘스트 정보가 갱신되었습니다. 다시 확인해 주세요.",
        3 => "아직 목표를 달성하지 않았습니다.",
        _ => "보상을 받지 못했습니다."
    };

    // 던전 인덱스 -> 표시 이름. 이름은 던전 에셋이 갖고 있으므로 규칙으로 계산하지 않는다
    // (장별 스테이지 수 같은 값을 클라가 따로 들고 있으면 던전 구성을 바꿀 때 두 곳이 어긋난다).
    private static string StageName(int dungeonIndex, DungeonDatabase dungeonDb) {
        var data = dungeonDb != null ? dungeonDb.Get(dungeonIndex) : null;
        return data != null ? data.DisplayName : dungeonIndex.ToString();
    }
}
