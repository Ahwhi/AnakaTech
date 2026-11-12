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

    /// <summary>
    /// 추천 배치. 후보 소녀들 중에서 조건을 만족하는 편성(membersPerSlot 명)을 "가장 아깝지 않게" 고른다.
    /// 만들 수 없으면 null 이다(조건에 맞는 소녀가 모자라거나 인원이 부족하다).
    ///
    /// 아깝지 않게 = 다른 파견에 쓸 수 있는 강한 소녀를 남겨 두는 쪽이다.
    ///   인원 조건(무기/등급) : 조건에 맞는 소녀를 약한 순으로 필요한 만큼만 넣고, 남은 자리는 조건과 무관한
    ///                          약한 소녀로 채운다(조건에 맞는 소녀는 다른 슬롯도 찾을 수 있으므로 아낀다).
    ///   총합 조건(별/레벨)   : 총합이 목표를 넘기는 범위에서 총합이 가장 작아지도록 고른다.
    /// 후보는 부르는 쪽이 정한다(파견 중이거나 다른 슬롯에 놓인 소녀는 넘기지 않는다).
    /// </summary>
    public static int[] Recommend(DispatchConditionDto condition, IReadOnlyList<int> candidates,
                                  PlayerStateStore store, int membersPerSlot) {
        if (condition == null || candidates == null || store == null || membersPerSlot <= 0) return null;

        var pool = new List<int>();
        foreach (int id in candidates)
            if (id != 0 && store.GetProgress(id) != null && !pool.Contains(id)) pool.Add(id);
        if (pool.Count < membersPerSlot) return null;

        // 약한 순(등급 -> 별 -> 레벨 -> id). 같은 값이면 늘 같은 결과가 나오도록 id 까지 본다.
        pool.Sort((a, b) => Strength(a, store).CompareTo(Strength(b, store)));

        var picked = condition.Kind switch {
            DispatchConditionKind.WeaponType => PickByCount(pool, id => JobOfChar(id) == condition.Param,
                                                            condition.Threshold, membersPerSlot),
            DispatchConditionKind.GradeCount => PickByCount(pool, id => GradeOfChar(id) == condition.Param,
                                                            condition.Threshold, membersPerSlot),
            DispatchConditionKind.StarTotal => PickBySum(pool, id => store.GetProgress(id).StarLevel,
                                                         condition.Threshold, membersPerSlot),
            DispatchConditionKind.LevelTotal => PickBySum(pool, id => store.GetProgress(id).Level,
                                                          condition.Threshold, membersPerSlot),
            _ => null
        };
        if (picked == null) return null;

        // 고른 결과가 정말 조건을 만족하는지 화면 판정과 같은 규칙으로 한 번 더 확인한다(서버도 다시 확인한다).
        var members = picked.ToArray();
        return MeetsCondition(condition, members, store, membersPerSlot) ? members : null;
    }

    // 인원 조건: 조건에 맞는 소녀 need 명(약한 순) + 나머지는 조건과 무관한 약한 소녀, 모자라면 맞는 소녀로 채운다.
    private static List<int> PickByCount(List<int> pool, System.Func<int, bool> matches, int need, int size) {
        var matching = pool.FindAll(id => matches(id));
        if (matching.Count < need) return null;

        var picked = matching.GetRange(0, need);
        foreach (int id in pool) {
            if (picked.Count >= size) break;
            if (!matches(id)) picked.Add(id);
        }
        foreach (int id in matching) {
            if (picked.Count >= size) break;
            if (!picked.Contains(id)) picked.Add(id);
        }
        return picked.Count >= size ? picked : null;
    }

    // 총합 조건: 가장 작은 size 명에서 시작해, 목표에 닿을 때까지 한 명씩 바꿔 끼운다.
    //   1) 한 명만 바꿔서 목표에 닿을 수 있으면, 그런 맞바꿈 중 목표를 가장 적게 넘기는 것을 고른다.
    //   2) 안 되면 가장 약한 자리를 남은 후보 중 가장 큰 값으로 바꾸고 다시 본다.
    // 상위 size 명의 총합도 목표에 못 미치면 처음부터 불가능하다.
    private static List<int> PickBySum(List<int> pool, System.Func<int, int> valueOf, int target, int size) {
        var byValue = new List<int>(pool);
        byValue.Sort((a, b) => valueOf(a).CompareTo(valueOf(b)));   // 정렬이 안정적이라 같은 값이면 약한 순이 유지된다

        int best = 0;
        for (int i = byValue.Count - size; i < byValue.Count; i++) best += valueOf(byValue[i]);
        if (best < target) return null;

        var picked = byValue.GetRange(0, size);
        var rest = byValue.GetRange(size, byValue.Count - size);
        int sum = 0;
        foreach (int id in picked) sum += valueOf(id);

        while (sum < target && rest.Count > 0) {
            // picked 와 rest 모두 값 오름차순을 유지한다(rest 에서 처음 찾은 후보가 곧 가장 작은 후보다).
            int bestP = -1, bestR = -1, bestSum = int.MaxValue;
            for (int p = 0; p < picked.Count; p++) {
                int need = target - (sum - valueOf(picked[p]));
                int r = rest.FindIndex(id => valueOf(id) >= need);
                if (r < 0) continue;
                int after = sum - valueOf(picked[p]) + valueOf(rest[r]);
                if (after < bestSum) { bestSum = after; bestP = p; bestR = r; }
            }
            if (bestP >= 0) {
                (picked[bestP], rest[bestR]) = (rest[bestR], picked[bestP]);
                sum = bestSum;
                break;
            }

            int top = rest[rest.Count - 1];
            if (valueOf(top) <= valueOf(picked[0])) return null;
            sum += valueOf(top) - valueOf(picked[0]);
            rest[rest.Count - 1] = picked[0];
            picked[0] = top;
            picked.Sort((a, b) => valueOf(a).CompareTo(valueOf(b)));
            rest.Sort((a, b) => valueOf(a).CompareTo(valueOf(b)));
        }
        return sum >= target ? picked : null;
    }

    // 약한 정도(작을수록 약함). 추천이 강한 소녀를 아끼는 기준이다.
    private static long Strength(int charId, PlayerStateStore store) {
        var p = store.GetProgress(charId);
        long grade = GradeOfChar(charId), star = p != null ? p.StarLevel : 0, level = p != null ? p.Level : 0;
        return ((grade * 100 + star) * 10000 + level) * 100000 + charId;
    }

    // 캐릭터 id 규약(서버 GameRoster 와 동일). id = (등급+1)*1000 + 직업*100 + 순번.
    // 규약을 아는 곳이 이미 여럿이라 여기서도 같은 식을 쓴다(데이터를 따로 들고 다니지 않기 위함).
    private static int GradeOfChar(int charId) => charId / 1000 - 1;
    private static int JobOfChar(int charId) => (charId % 1000) / 100;
}
