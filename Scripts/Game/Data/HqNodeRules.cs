using System.Collections.Generic;

/// <summary>
/// 본부 노드의 해금/강화 판정과 성장 상한(클라 표시용). 서버 HeadquartersCatalog 와 같은 규칙이며,
/// 실제 차감/승인은 서버가 한다.
///
/// 트리 화면과 알림 배지가 "지금 올릴 수 있는 노드가 있는가"를 서로 다른 코드로 판단하면 표시가 어긋나므로
/// 판정을 여기 한 곳에 모은다.
///
/// 최대 레벨과 선행 조건 레벨은 모든 노드가 같은 값을 쓴다. 노드마다 따로 들고 있으면 에셋 16개와
/// 서버 표를 사람이 맞춰야 하고, 한 곳만 어긋나면 "클라는 올릴 수 있다고 하는데 서버가 거절하는" 상태가 된다.
/// 값이 하나뿐이면 맞출 곳도 클라/서버 각각 한 줄뿐이다.
/// </summary>
public static class HqNodeRules {
    /// <summary>노드 하나가 오를 수 있는 최대 레벨. 서버 HeadquartersCatalog.MaxLevel 과 같아야 한다.</summary>
    public const int MaxLevel = 100;

    /// <summary>
    /// 선행 노드가 이 레벨 이상이면 다음 노드가 열린다. 서버 HeadquartersCatalog.PrerequisiteLevel 과 같아야 한다.
    ///
    /// 최대 레벨이 아니라 별도의 낮은 값인 이유: 상한이 100 이라 "선행을 끝까지" 를 요구하면
    /// 두 번째 노드조차 사실상 열리지 않는다. 한 갈래를 어느 정도 밀면 다음이 열리고,
    /// 그 뒤로는 어느 노드를 더 올릴지 고르게 두는 편이 트리로서 굴러간다.
    /// </summary>
    public const int PrerequisiteLevel = 10;

    /// <summary>
    /// 레벨업 비용(명성) = 이 값 x 다음 레벨. 서버 HeadquartersCatalog.RenownPerLevel 과 같아야 한다.
    ///
    /// 노드마다 다르지 않은 이유는 최대 레벨과 같다. 노드별로 두면 에셋과 서버 표 양쪽을 사람이 맞춰야 하고,
    /// 비용은 어긋나도 화면에는 아무 일도 없는 것처럼 보이다가 서버가 다른 금액을 차감해 가장 늦게 발견된다.
    /// </summary>
    public const int RenownPerLevel = 10;

    public static int LevelOf(IReadOnlyDictionary<int, int> levels, int nodeId) =>
        levels != null && levels.TryGetValue(nodeId, out int lv) ? lv : 0;

    /// <summary>선행 노드를 전부 PrerequisiteLevel 이상으로 올렸는지(하나라도 미달이면 잠긴다).</summary>
    public static bool PrerequisitesMet(IReadOnlyDictionary<int, int> levels, HqNodeData data) {
        if (data == null) return false;
        if (data.Requires == null) return true;

        foreach (int id in data.Requires)
            if (LevelOf(levels, id) < PrerequisiteLevel) return false;
        return true;
    }

    /// <summary>지금 명성으로 이 노드를 한 단계 올릴 수 있는지(최대 레벨/선행/비용 전부 충족).</summary>
    public static bool CanUpgrade(IReadOnlyDictionary<int, int> levels, HqNodeData data, int renown) {
        if (data == null) return false;

        int level = LevelOf(levels, data.Id);
        if (level >= MaxLevel) return false;
        if (!PrerequisitesMet(levels, data)) return false;
        return renown >= data.UpgradeCost(level);
    }

    /// <summary>지금 올릴 수 있는 노드가 하나라도 있는지(본부 버튼 알림 배지 판정).</summary>
    public static bool AnyUpgradable(HqNodeDatabase db, IReadOnlyDictionary<int, int> levels, int renown) {
        if (db == null) return false;
        foreach (var data in db.All)
            if (CanUpgrade(levels, data, renown)) return true;
        return false;
    }
}
