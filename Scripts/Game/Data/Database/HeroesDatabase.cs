using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 영웅전 단계 구성(V1.12). 50단계를 하나하나 짜지 않고 "구간"으로 나눈다.
/// 구간은 시작 단계(From Stage)부터 다음 구간 직전까지 쓰이며 몬스터 구성/사거리/스폰 속도를 정한다.
/// 단계별 몬스터 강함은 구간이 아니라 항목의 성장 목표(At Max Stage)가 정한다(1~최대 단계 전체 곡선).
///
/// 한 판에 나오는 몬스터는 구간의 Spawns 목록이 정한다. 항목마다 마릿수를 정하고, 전부 잡으면 클리어다
/// (처치 목표 = 마릿수 합계). 보스처럼 나중에 나올 몬스터는 Appear After Kills 로 등장 시점을 늦춘다.
///
/// 영웅전 몬스터는 전부 논타겟 공격을 한다. 몬스터 데이터는 그대로 두고 공격을 스폰할 때 덮어 씌우므로,
/// 다른 컨텐츠에서 쓰는 몬스터를 복제하지 않고 그대로 넣으면 된다.
/// </summary>
[CreateAssetMenu(fileName = "HeroesDatabase", menuName = "Anaka/Database/Heroes")]
public sealed class HeroesDatabase : ScriptableObject {
    /// <summary>한 판에 나올 몬스터 한 종류와 그 규칙.</summary>
    [Serializable]
    public sealed class SpawnEntry {
        public MonsterData Monster;

        [Tooltip("이 몬스터가 한 판에 나오는 마릿수. 구간의 마릿수 합계가 처치 목표다.")]
        [Min(1)] public int Count = 1;

        [Tooltip("켜면 보스로 다룬다. 보스가 전장에 있는 동안 상단 게이지가 처치 수 대신 보스 체력(%)을 보여 준다.")]
        public bool IsBoss;

        [Tooltip("이만큼 잡은 뒤부터 나온다(0 = 처음부터). 보스는 다른 몬스터 수만큼 두면 마지막에 나온다. " +
                 "다른 몬스터가 다 나오고 전장이 비면 이 값과 상관없이 나온다(판이 막히지 않게).")]
        [Min(0)] public int AppearAfterKills;

        [Tooltip("1단계에서 몬스터 데이터 스탯에 곱하는 공격력/체력/방어력 배율. 보통 1.")]
        [Min(0.01f)] public float StatMultiplier = 1f;

        [Header("Growth (0 이면 성장하지 않음)")]
        [Tooltip("최대 단계(50)에서의 공격력. 1단계 값(몬스터 데이터 x Stat Multiplier)에서 이 값까지 성장 곡선(기본 직선)을 따라 오른다. " +
                 "구간과 상관없는 전체 곡선이라 구간마다 같은 값을 넣으면 된다. 0 이면 1단계 값 그대로다.")]
        [FormerlySerializedAs("AttackAtLastStage")]
        [Min(0f)] public float AttackAtMaxStage;
        [Tooltip("최대 단계(50)에서의 체력. 0 이면 1단계 값 그대로다.")]
        [FormerlySerializedAs("HealthAtLastStage")]
        [Min(0f)] public float HealthAtMaxStage;
        [Tooltip("최대 단계(50)에서의 방어력. 0 이면 1단계 값 그대로다.")]
        [FormerlySerializedAs("DefenseAtLastStage")]
        [Min(0f)] public float DefenseAtMaxStage;

        [Tooltip("공격 속도 보너스(%). 100 이면 두 배로 자주 쏜다. Attack Interval Seconds 를 정하면 쓰지 않는다.")]
        public float AttackSpeedBonus;

        [Tooltip("공격 간격(초)을 직접 정한다. 0 이면 몬스터 공속 + Attack Speed Bonus 로 정해진다. " +
                 "여러 발을 쏘는 몬스터는 예고 + 비행 시간(길이 / 탄속)보다 길게 두어야 앞 탄과 다음 예고가 겹치지 않는다.")]
        [Min(0f)] public float AttackIntervalSeconds;

        [Tooltip("이 몬스터만의 공격. 비우면 아래 Monster Attack(기본 공격)을 쓴다.")]
        public BasicAttack Attack;

        [Tooltip("공격이 라인샷일 때 이 몬스터만 다르게 쓸 값(발 수/부채꼴/속도/예고/길이). 비워 두면 공격 그대로.")]
        public LineShotAttack.Variant LineShot = new();
    }

    [Serializable]
    public sealed class StageBand {
        [Tooltip("이 구간이 시작되는 단계. 다음 구간의 시작 단계 직전까지 이 구성을 쓴다.")]
        [Min(1)] public int FromStage = 1;

        [Tooltip("한 판에 나오는 몬스터 구성. 아직 나올 수 있는 항목 중에서 남은 마릿수에 비례해 무작위로 섞여 나온다.")]
        public List<SpawnEntry> Spawns = new();

        [Tooltip("동시에 전장에 둘 최대 몬스터 수.")]
        [Min(1)] public int MaxConcurrentMonsters = 8;

        [Tooltip("몬스터를 한 마리씩 채워 넣는 간격(초).")]
        [Min(0.05f)] public float SpawnIntervalSeconds = 0.8f;

        [Tooltip("몬스터 사거리 덮어쓰기(월드 units). 0 이면 몬스터 데이터의 값.")]
        [Min(0f)] public float MonsterRange = 4f;

        /// <summary>한 판의 처치 목표(마릿수 합계).</summary>
        public int TotalCount {
            get {
                int total = 0;
                foreach (var e in Spawns) if (e != null && e.Monster != null) total += Mathf.Max(1, e.Count);
                return total;
            }
        }
    }

    [Tooltip("단계 구간. 시작 단계 순으로 넣는다(첫 구간은 1단계부터).")]
    [SerializeField] private List<StageBand> bands = new();

    [Tooltip("영웅전 몬스터의 기본 공격(LineShotAttack). 항목에 공격을 따로 넣지 않으면 이것을 쓴다. 비우면 몬스터 데이터의 공격.")]
    [SerializeField] private BasicAttack monsterAttack;

    [Tooltip("몬스터가 나타나는 거리(조작 소녀 기준, 월드 units). 전장 밖이면 전장 가장자리에서 나타난다.")]
    [SerializeField, Min(1f)] private float spawnDistance = 9f;

    [Header("Boss")]
    [Tooltip("보스(Is Boss 항목)가 나타날 때 남은 제한 시간에 더하는 시간(초).")]
    [SerializeField, Min(0f)] private float bossTimeBonusSeconds = 15f;

    [Header("Stat Growth")]
    [Tooltip("1단계 값에서 최대 단계 목표(At Max Stage)까지 오르는 모양. 가로 = 진행도(1단계 0 ~ 최대 단계 1), " +
             "세로 = 목표까지 오른 비율(0 ~ 1). 기본은 직선(단계마다 같은 양씩 오른다). 초반을 더 가파르게 하려면 위로 볼록하게 만든다.")]
    [SerializeField] private AnimationCurve statGrowthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Monster Movement")]
    [Tooltip("몬스터가 전장 안을 떠도는 방식. 소녀를 쫓지 않고 떠돌다가 사거리에 소녀가 들어오면 걸으면서 쏜다.")]
    [SerializeField] private WanderMoveInput.Pattern monsterWander = new();

    [Header("Heal Item")]
    [Tooltip("회복 아이템 프리팹(HealPickup). 비우면 회복 아이템이 나오지 않는다.")]
    [SerializeField] private HealPickup healPickupPrefab;
    [Tooltip("전장에 동시에 놓일 최대 회복 아이템 수.")]
    [SerializeField, Min(0)] private int maxHealPickups = 2;
    [Tooltip("회복 아이템을 하나씩 뿌리는 간격(초). 판이 시작될 때도 이 간격 뒤에 첫 아이템이 놓인다.")]
    [SerializeField, Min(0.5f)] private float healSpawnIntervalSeconds = 8f;
    [Tooltip("회복 아이템이 조작 소녀에게서 최소한 떨어져 놓일 거리(월드 units). 바로 발밑에 생기지 않게 한다.")]
    [SerializeField, Min(0f)] private float healMinDistance = 3f;
    [Tooltip("선택. 회복 아이템을 집을 때 소녀 위치에 재생할 VFX.")]
    [SerializeField] private GameObject healVfxPrefab;

    public BasicAttack MonsterAttack => monsterAttack;
    public float SpawnDistance => spawnDistance;
    public float BossTimeBonusSeconds => bossTimeBonusSeconds;
    public WanderMoveInput.Pattern MonsterWander => monsterWander;
    public HealPickup HealPickupPrefab => healPickupPrefab;
    public int MaxHealPickups => maxHealPickups;
    public float HealSpawnIntervalSeconds => healSpawnIntervalSeconds;
    public float HealMinDistance => healMinDistance;
    public GameObject HealVfxPrefab => healVfxPrefab;

    /// <summary>그 단계가 쓰는 구간. 시작 단계가 그 단계 이하인 구간 중 가장 늦게 시작하는 것. 데이터가 없으면 null.</summary>
    public StageBand BandFor(int stage) {
        StageBand best = null;
        foreach (var b in bands) {
            if (b == null || b.FromStage > stage) continue;
            if (best == null || b.FromStage >= best.FromStage) best = b;
        }
        return best;
    }

    /// <summary>
    /// 그 단계에서 항목 몬스터의 스탯. 시작값(1단계 값)은 몬스터 데이터 x 항목 배율이고,
    ///   목표 수치(At Max Stage)를 정한 스탯은 1단계에서 최대 단계까지 성장 곡선을 따라 목표에 닿는다
    ///   (값 = 시작값 + (목표 - 시작값) x 곡선(진행도), 진행도 = (단계 - 1) / (최대 단계 - 1)). 기본 곡선은 직선이다.
    ///   곡선은 구간과 상관없이 전체 단계 기준이라, 구간을 나눠도 같은 항목 값이면 단계 사이가 끊기지 않는다.
    ///   정하지 않은 스탯(0)은 시작값 그대로다.
    /// 공속/이속 등 나머지 스탯은 몬스터 데이터 값 그대로다(공속 보너스는 따로 더한다).
    /// 공격 간격(초)을 정했으면 그 간격이 나오도록 공속을 맞춘다(baseAttacksPerSecond = BattleSettings 의 기준 공속).
    /// </summary>
    public CombatStats StatsFor(SpawnEntry entry, StageBand band, int stage, float baseAttacksPerSecond) {
        var stats = entry.Monster.BaseStats;
        float startMult = entry.StatMultiplier;

        int max = HeroesRules.MaxStage;
        float progress = max > 1 ? Mathf.Clamp01((float)(stage - 1) / (max - 1)) : 1f;
        float t = statGrowthCurve != null && statGrowthCurve.length > 0 ? statGrowthCurve.Evaluate(progress) : progress;

        stats.Attack = Grow(stats.Attack * startMult, entry.AttackAtMaxStage, t);
        stats.Health = Grow(stats.Health * startMult, entry.HealthAtMaxStage, t);
        stats.Defense = Grow(stats.Defense * startMult, entry.DefenseAtMaxStage, t);
        // 공격 간격 = (1 / 기준 공속) / (1 + 공속% / 100) 이므로, 원하는 간격에서 공속%를 거꾸로 구한다.
        if (entry.AttackIntervalSeconds > 0f && baseAttacksPerSecond > 0f)
            stats.AttackSpeed = (1f / (baseAttacksPerSecond * entry.AttackIntervalSeconds) - 1f) * 100f;
        else
            stats.AttackSpeed += entry.AttackSpeedBonus;
        if (band.MonsterRange > 0f) stats.Range = band.MonsterRange;
        return stats;
    }

    private static float Grow(float start, float target, float t) {
        if (target <= 0f) return start;
        return Mathf.Max(0f, start + (target - start) * t);
    }
}
