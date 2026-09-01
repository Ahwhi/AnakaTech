using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 정적 데이터(PLANNING 3). 몬스터 종류/비율/배경을 클라 SO 로 관리하고 서버와는 dungeonIndex 만 공유한다.
/// 한 런에서 등장하는 몬스터 총 수 = 모든 SpawnEntry.weight 합. 등장 순서는 보장되지 않고 비율 내 랜덤(PLANNING 3-2).
/// </summary>
[CreateAssetMenu(fileName = "DungeonData", menuName = "Anaka/Data/Dungeon")]
public sealed class DungeonData : ScriptableObject {
    [Serializable]
    public struct SpawnEntry {
        public MonsterData monster;
        [Min(0)] public int weight;   // 이 몬스터가 한 런에서 등장하는 마리 수
    }

    [Header("Identity")]
    [Tooltip("서버와 공유하는 고정 인덱스(예: 1-1 -> 임의의 고유 정수).")]
    [SerializeField] private int dungeonIndex;
    [Tooltip("표시용 이름(예: \"1-1\").")]
    [SerializeField] private string displayName;
    [Tooltip("이 던전이 속한 그룹. 던전 선택 드롭다운의 목록은 여기 할당된 그룹들에서 유도된다.")]
    [SerializeField] private DungeonGroupData group;

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("Spawns (몬스터 종류 x 마리 수)")]
    [SerializeField] private List<SpawnEntry> spawns = new();

    [Header("Formation Override (선택)")]
    [Tooltip("끄면 BattleSettings 의 공용 배치를 그대로 쓴다(대부분의 던전). " +
             "켜면 이 던전만 아래 값으로 유닛을 배치한다(보스방처럼 넓이/거리가 다른 던전).")]
    [SerializeField] private bool overrideFormation;
    [Tooltip("이 던전 전용 배치. 위 체크가 꺼져 있으면 무시된다.")]
    [SerializeField] private BattleFormation formation = BattleFormation.Default;

    public int DungeonIndex => dungeonIndex;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public DungeonGroupData Group => group;
    public Sprite BackgroundSprite => backgroundSprite;
    public IReadOnlyList<SpawnEntry> Spawns => spawns;

    /// <summary>이 던전 전용 배치(오버라이드 미사용이면 null = 호출측이 공용 기본값을 쓴다).
    /// 던전이 "다르게 배치할지"만 표현하고, 기본값을 어디서 가져오는지는 알지 않는다.</summary>
    public BattleFormation? FormationOverride => overrideFormation ? formation : (BattleFormation?)null;

    /// <summary>한 런의 총 몬스터 수(모든 weight 합).</summary>
    public int TotalMonsterCount {
        get {
            int total = 0;
            for (int i = 0; i < spawns.Count; i++)
                if (spawns[i].monster != null) total += Mathf.Max(0, spawns[i].weight);
            return total;
        }
    }

    /// <summary>
    /// 몬스터별 등장 마리 수를 합산한 목록(같은 몬스터가 여러 SpawnEntry 로 나뉘어 있어도 한 항목으로 묶는다).
    /// 던전 정보를 "몬스터 + 마리 수"로 표시하는 UI 가 사용한다. 등장 순서와는 무관하다.
    /// </summary>
    public List<SpawnEntry> BuildMonsterCounts() {
        var list = new List<SpawnEntry>(spawns.Count);
        for (int i = 0; i < spawns.Count; i++) {
            var e = spawns[i];
            if (e.monster == null || e.weight <= 0) continue;

            int found = list.FindIndex(x => x.monster == e.monster);
            if (found >= 0) {
                var merged = list[found];
                merged.weight += e.weight;
                list[found] = merged;
            }
            else {
                list.Add(new SpawnEntry { monster = e.monster, weight = e.weight });
            }
        }
        return list;
    }

    /// <summary>비율대로 채운 뒤 무작위로 섞은 이번 런의 스폰 순서를 만든다(PLANNING 3-2).</summary>
    public List<MonsterData> BuildShuffledSpawnQueue() {
        var list = new List<MonsterData>(TotalMonsterCount);
        for (int i = 0; i < spawns.Count; i++) {
            var e = spawns[i];
            if (e.monster == null) continue;
            for (int n = 0; n < e.weight; n++) list.Add(e.monster);
        }
        // Fisher-Yates 셔플
        for (int i = list.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
