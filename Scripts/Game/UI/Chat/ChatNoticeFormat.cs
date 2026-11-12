/// <summary>
/// 시스템 알림 문구를 만든다. 서버는 "무슨 일이 누구에게 있었는지"(종류 + 인자)만 보내고 문장은 여기서 조립한다.
///
/// 왜 클라가 만드는가: 캐릭터 이름과 수련 효과 이름은 밸런스/표현 데이터(SO)에 있어서 클라만 알고 있다.
/// 서버가 문장을 완성하려면 같은 이름표를 서버에도 복사해 두어야 하고, 그러면 이름을 고칠 때 두 곳이 어긋난다.
///
/// 새 알림을 추가할 때는 서버에 ChatNoticeKind 를 하나 늘리고 여기 한 줄을 더하면 된다.
/// </summary>
public static class ChatNoticeFormat {
    /// <summary>
    /// 알림 문구. 해석할 수 없는 종류(구버전 클라가 모르는 새 알림 등)는 빈 문자열이라 화면이 그 줄을 건너뛴다.
    /// </summary>
    public static string Build(ChatMessageDto message, BattleHudContext hud) {
        if (message == null) return "";

        return message.NoticeKind switch {
            ChatNoticeKind.GachaHighGrade => CharacterGachaText(message, hud),
            ChatNoticeKind.GachaHighGradeEquip => EquipGachaText(message, hud),
            ChatNoticeKind.TrainingMax => TrainingText(message),
            ChatNoticeKind.StarMaxChar => CharacterStarText(message, hud),
            ChatNoticeKind.StarMaxEquip => EquipStarText(message, hud),
            ChatNoticeKind.LimitBreakChar => LimitBreakText(message, hud),
            _ => ""
        };
    }

    // "[닉네임]님이 SSR [캐릭터]을(를) 모집했습니다."
    private static string CharacterGachaText(ChatMessageDto message, BattleHudContext hud) {
        var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(message.RefId) : null;
        return data == null ? "" : GachaText(message.Nickname, data.Grade, data.DisplayName);
    }

    // "[닉네임]님이 SSR [장비]을(를) 모집했습니다." 장비 이름은 장비 데이터베이스가 갖고 있다.
    private static string EquipGachaText(ChatMessageDto message, BattleHudContext hud) {
        var data = hud != null && hud.EquipDb != null ? hud.EquipDb.Get(message.RefId) : null;
        return data == null ? "" : GachaText(message.Nickname, data.Grade, data.DisplayName);
    }

    // 캐릭터든 장비든 문구는 같다(무엇을 뽑았는지만 다르다). 한 곳에 두어 두 알림의 표기가 어긋나지 않게 한다.
    private static string GachaText(string nickname, Grade grade, string displayName) {
        // 등급 표기는 도감 보상 문구와 같은 규칙을 쓴다(같은 등급이 화면마다 다르게 적히지 않도록).
        string gradeName = EncyclopediaRewardRules.GradeName((int)grade);
        return $"{nickname}님이 {gradeName} {displayName}을(를) 모집했습니다.";
    }

    // "[닉네임]님이 [캐릭터]을(를) 15성으로 각성했습니다."
    private static string CharacterStarText(ChatMessageDto message, BattleHudContext hud) {
        var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(message.RefId) : null;
        return data == null ? "" : StarText(message, data.DisplayName);
    }

    // "[닉네임]님이 [장비]을(를) 15성으로 각성했습니다."
    private static string EquipStarText(ChatMessageDto message, BattleHudContext hud) {
        var data = hud != null && hud.EquipDb != null ? hud.EquipDb.Get(message.RefId) : null;
        return data == null ? "" : StarText(message, data.DisplayName);
    }

    // 별레벨은 서버가 함께 보내 준 값을 그대로 쓴다(클라가 최대치 상수를 따로 들고 있지 않도록).
    private static string StarText(ChatMessageDto message, string displayName) =>
        $"{message.Nickname}님이 {displayName}을(를) {message.RefValue}성으로 각성했습니다.";

    // "[닉네임]님이 [캐릭터]을(를) 한계돌파했습니다."
    private static string LimitBreakText(ChatMessageDto message, BattleHudContext hud) {
        var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(message.RefId) : null;
        return data == null ? "" : $"{message.Nickname}님이 {data.DisplayName}을(를) 한계돌파했습니다.";
    }

    // "[닉네임]님이 [효과 수치] 수련에 성공했습니다."
    private static string TrainingText(ChatMessageDto message) {
        string effect = TrainingRules.Describe(message.RefId, message.RefValue);
        if (string.IsNullOrEmpty(effect)) return "";

        return $"{message.Nickname}님이 {effect} 수련에 성공했습니다.";
    }
}
