using System.Collections.Generic;

/// <summary>
/// 파견 규칙의 클라 미러. 조건 판정과 보상 지급은 전부 서버가 하므로, 여기에는 표시 문구와
/// "보내기 버튼을 켤 수 있는가"를 미리 보는 판정만 둔다(서버가 다시 확인하므로 이 판정은 안내용이다).
///
/// 조건 수치와 보상 구성은 서버가 조건표로 내려주므로 여기에 표를 두지 않는다.
/// 조건이 늘어도 종류(DispatchConditionKind)가 그대로면 이 파일은 수정할 필요가 없다.
/// </summary>
public static class DispatchRules {
    /// <summary>파견 작업 실패 사유(서버 코드) -> 안내 문구. 서버 DispatchFail 과 짝을 이룬다.</summary>
    public static string FailMessage(int fail) => fail switch {
        1 => "로그인이 필요합니다.",
        2 => "잘못된 파견 슬롯입니다.",
        3 => "이미 파견 중인 슬롯입니다.",
        4 => "파견 중인 슬롯이 아닙니다.",
        5 => "아직 파견이 끝나지 않았습니다.",
        6 => "편성이 올바르지 않습니다.",
        7 => "다른 슬롯에 편성된 소녀가 있습니다.",
        8 => "파견 조건을 만족하지 않습니다.",
        9 => "파견 갱신 횟수가 부족합니다.",
        _ => "파견을 처리하지 못했습니다."
    };

    /// <summary>무기 타입 표시명. 값은 서버 GameRoster 의 직업 순서(0=전사..4=격투가)와 같다.</summary>
    public static string WeaponName(int job) => job switch {
        0 => "검",
        1 => "활",
        2 => "총",
        3 => "스태프",
        4 => "너클",
        _ => "무기"
    };

    /// <summary>만능조각/캐릭터 등급 표시명. 조건 문구에서만 쓴다.</summary>
    public static string GradeName(int grade) => grade switch {
        0 => "N",
        1 => "R",
        2 => "SR",
        3 => "SSR",
        4 => "XR",
        _ => "?"
    };

    /// <summary>조건 문구. 화면 어디서든 같은 표기를 쓰도록 이 한 곳에서 만든다.</summary>
    public static string ConditionText(DispatchConditionDto condition) {
        if (condition == null) return "";
        return condition.Kind switch {
            DispatchConditionKind.WeaponType =>
                $"{WeaponName(condition.Param)} 타입 소녀 {condition.Threshold}명 이상",
            DispatchConditionKind.StarTotal =>
                $"별레벨 총합 {condition.Threshold:N0} 이상",
            DispatchConditionKind.GradeCount =>
                $"{GradeName(condition.Param)} 등급 소녀 {condition.Threshold}명 이상",
            DispatchConditionKind.LevelTotal =>
                $"레벨 총합 {condition.Threshold:N0} 이상",
            _ => ""
        };
    }

    /// <summary>조건 만족 시 켜는 문구.</summary>
    public const string ConditionMetText = "파견 조건 만족";

    /// <summary>조건 미달 시 켜는 문구(뒤에 진행도 "(n/n)" 이 붙는다).</summary>
    public const string ConditionUnmetText = "파견 조건 불만족";

    /// <summary>조건 수치는 넘겼지만 인원이 덜 찼을 때의 문구.</summary>
    public const string ConditionShortHandedText = "추가 인원 필요";

    // 슬롯의 실행 버튼 문구. 버튼은 하나이고 슬롯 상태에 따라 뜻이 바뀐다
    // (대기 = 보내기, 파견 중 = 진행 안내, 완료 = 수령).
    public const string ActionSendText = "파견";
    public const string ActionRunningText = "파견중";
    public const string ActionClaimText = "완료";

    // 파견을 보낸 슬롯의 상태 문구. 남은 시간과 달리 끝난 뒤에도 켜져 있어야 하므로 따로 둔다.
    public const string RunningText = "파견중";
    public const string RunningDoneText = "파견 종료";

    /// <summary>슬롯 상태에 맞는 실행 버튼 문구.</summary>
    public static string ActionText(bool running, bool finished) =>
        !running ? ActionSendText : finished ? ActionClaimText : ActionRunningText;

    /// <summary>슬롯의 조건 충족 상태(표시용). 조건 수치와 인원은 서로 다른 이유로 막히므로 따로 알린다.</summary>
    public enum ConditionState {
        Met,          // 조건 수치도 인원도 채웠다(보낼 수 있다)
        ShortHanded,  // 조건 수치는 넘겼지만 인원이 덜 찼다
        Unmet         // 조건 수치가 모자란다
    }

    /// <summary>
    /// 지금 편성이 조건에 대해 얼마를 채웠는지. 무기/등급 조건은 해당 인원 수, 별레벨/레벨 조건은 총합이다.
    /// 화면의 "(n/n)" 표시와 아래 판정이 같은 값을 쓰도록 여기 한 곳에서 센다.
    /// </summary>
    public static int Progress(DispatchConditionDto condition, IReadOnlyList<int> members, PlayerStateStore store) {
        if (condition == null || members == null || store == null) return 0;

        int value = 0;
        foreach (int id in members) {
            if (id == 0) continue;
            var progress = store.GetProgress(id);
            if (progress == null) continue;   // 보유하지 않은 소녀는 서버가 거절하므로 세지 않는다

            switch (condition.Kind) {
                case DispatchConditionKind.WeaponType:
                    if (JobOfChar(id) == condition.Param) value++;
                    break;
                case DispatchConditionKind.GradeCount:
                    if (GradeOfChar(id) == condition.Param) value++;
                    break;
                case DispatchConditionKind.StarTotal:
                    value += progress.StarLevel;
                    break;
                case DispatchConditionKind.LevelTotal:
                    value += progress.Level;
                    break;
            }
        }
        return value;
    }

    /// <summary>편성에 실제로 들어가 있는 인원 수.</summary>
    public static int FilledCount(IReadOnlyList<int> members) {
        if (members == null) return 0;
        int filled = 0;
        foreach (int id in members) if (id != 0) filled++;
        return filled;
    }

    /// <summary>
    /// 지금 편성의 상태를 가른다. 조건 수치를 먼저 보는 이유는, 수치가 모자라면 인원을 채워도 못 보내므로
    /// "인원을 더 넣으라"는 안내가 오히려 길을 잘못 알려 주기 때문이다.
    /// </summary>
    public static ConditionState StateOf(DispatchConditionDto condition, IReadOnlyList<int> members,
                                         PlayerStateStore store, int membersPerSlot) {
        if (condition == null) return ConditionState.Unmet;
        if (Progress(condition, members, store) < condition.Threshold) return ConditionState.Unmet;
        return FilledCount(members) >= membersPerSlot ? ConditionState.Met : ConditionState.ShortHanded;
    }

    /// <summary>
    /// 편성이 조건을 만족하는지 미리 본다(서버 MeetsCondition 의 미러).
    /// 판정에 쓰는 값은 전부 스토어의 보유 정보라, 서버가 보는 값과 같은 것을 본다.
    /// </summary>
    public static bool MeetsCondition(DispatchConditionDto condition, IReadOnlyList<int> members,
                                      PlayerStateStore store, int membersPerSlot) =>
        StateOf(condition, members, store, membersPerSlot) == ConditionState.Met;

    // 캐릭터 id 규약(서버 GameRoster 와 동일). id = (등급+1)*1000 + 직업*100 + 순번.
    // 규약을 아는 곳이 이미 여럿이라 여기서도 같은 식을 쓴다(데이터를 따로 들고 다니지 않기 위함).
    private static int GradeOfChar(int charId) => charId / 1000 - 1;
    private static int JobOfChar(int charId) => (charId % 1000) / 100;
}
