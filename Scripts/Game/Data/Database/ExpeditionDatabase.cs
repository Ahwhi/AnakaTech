using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원정대 장소 5곳의 표시/전투 데이터(이름, 썸네일, 배경, 레벨별 몬스터 구성).
/// 체력과 보상은 서버 권위라 여기 두지 않는다(ExpeditionCatalog / ExpeditionRules).
///
/// 레벨별 몬스터 구성은 한 판에 함께 나오는 묶음이다. 장소 체력(서버 값)이 남아 있는 동안 묶음을 다 잡으면
/// 같은 묶음이 다시 나와 제한 시간까지 계속 싸운다(ExpeditionDirector).
/// </summary>
[CreateAssetMenu(fileName = "ExpeditionDatabase", menuName = "Anaka/Database/Expedition")]
public sealed class ExpeditionDatabase : ScriptableObject {
    [Serializable]
    public sealed class LevelData {
        [Tooltip("이 레벨에서 한 묶음으로 함께 나오는 몬스터. 묶음을 다 잡아도 장소 체력이 남아 있으면 다시 나온다.")]
        public MonsterData[] monsters = Array.Empty<MonsterData>();
    }

    [Serializable]
    public sealed class LocationData {
        public ExpeditionLocation location;
        public string displayName;
        [Tooltip("원정대 팝업 카드 썸네일.")]
        public Sprite thumbnail;
        [Tooltip("전투 배경. 비우면 씬에 깔린 배경을 그대로 쓴다.")]
        public Sprite background;
        [Tooltip("1레벨부터 순서대로(ExpeditionRules.MaxLevel 개). 모자라면 마지막 레벨 구성을 이어 쓴다.")]
        public LevelData[] levels = Array.Empty<LevelData>();
    }

    [SerializeField] private List<LocationData> locations = new();

    public LocationData Get(ExpeditionLocation location) {
        foreach (var l in locations)
            if (l != null && l.location == location) return l;
        return null;
    }

    /// <summary>장소 이름. 데이터가 비어 있어도 화면이 비지 않도록 기본 이름으로 떨어진다.</summary>
    public string NameOf(ExpeditionLocation location) {
        var data = Get(location);
        return data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : DefaultName(location);
    }

    /// <summary>그 레벨의 몬스터 묶음(없으면 빈 배열). 레벨 데이터가 모자라면 마지막 레벨 것을 쓴다.</summary>
    public IReadOnlyList<MonsterData> MonstersFor(ExpeditionLocation location, int level) {
        var data = Get(location);
        if (data == null || data.levels == null || data.levels.Length == 0) return Array.Empty<MonsterData>();
        int index = Mathf.Clamp(level - 1, 0, data.levels.Length - 1);
        var lv = data.levels[index];
        return lv != null && lv.monsters != null ? lv.monsters : Array.Empty<MonsterData>();
    }

    private static string DefaultName(ExpeditionLocation location) => location switch {
        ExpeditionLocation.CommTower => "통신탑",
        ExpeditionLocation.SupplyDepot => "보급창고",
        ExpeditionLocation.Recruitment => "모집사무실",
        ExpeditionLocation.Vault => "금고",
        ExpeditionLocation.Market => "장터",
        _ => location.ToString()
    };
}
