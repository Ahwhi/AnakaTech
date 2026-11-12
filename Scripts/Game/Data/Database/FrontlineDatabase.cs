using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 전선 정적 데이터(V1.12). 전선마다 이름/썸네일/배경과 몬스터 구성 1개만 짜 두면, 단계가 오를수록 같은 구성의
/// 체력과 공격력/방어력이 각자의 배율로 강해진다(무한 타워와 같은 방식, 배율만 둘로 나눴다).
///   N단계 배율 = 성장 배율의 (N - 1) 제곱. 1단계는 템플릿 그대로다.
///
/// 목록의 순서가 곧 전선 id 다(0 = 첫 번째 전선). 서버 FrontlineCatalog 와 id 를 공유하므로 순서를 바꾸지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "FrontlineDatabase", menuName = "Anaka/Database/Frontline")]
public sealed class FrontlineDatabase : ScriptableObject {
    [Serializable]
    public struct Spawn {
        public MonsterData Monster;
        [Min(1)] public int Count;
    }

    [Serializable]
    public sealed class Frontline {
        [Tooltip("표시 이름. 비우면 기본 이름(감시 초소/고지/최전방/차단선/보급로/해안선)을 쓴다.")]
        public string DisplayName;

        [Tooltip("전선 팝업 카드의 썸네일.")]
        public Sprite Thumbnail;

        [Tooltip("전투 배경. 비우면 Default Background 를 쓴다.")]
        public Sprite Background;

        [Tooltip("한 판에 나오는 몬스터. 전부 잡으면 클리어다.")]
        public List<Spawn> Spawns = new();

        [Tooltip("동시에 화면에 둘 최대 몬스터 수. 0 이면 전투 설정(BattleSettings)의 값을 쓴다.")]
        [Min(0)] public int MaxConcurrentMonsters;

        [Tooltip("몬스터를 채워 넣는 간격(초). 0 이면 전투 설정(BattleSettings)의 값을 쓴다.")]
        [Min(0f)] public float SpawnIntervalSeconds;

        [Tooltip("한 판의 제한 시간(초). 시간 안에 몬스터를 전부 잡지 못하면 실패다. 0 이면 Default Time Limit Seconds 를 쓴다.")]
        [Min(0f)] public float TimeLimitSeconds;

        [Tooltip("단계가 하나 오를 때마다 몬스터 체력에 곱하는 배율. 50단계 배율 = 이 값의 49제곱(1.326 = 약 100만 배).")]
        [Min(1f)] public float HealthGrowthPerStage = 1.326f;

        [Tooltip("단계가 하나 오를 때마다 몬스터 공격력/방어력에 곱하는 배율. 50단계 배율 = 이 값의 49제곱(1.08 = 약 43배).")]
        [FormerlySerializedAs("StatGrowthPerStage")]
        [Min(1f)] public float AttackGrowthPerStage = 1.08f;
    }

    [Tooltip("전선 6곳. 순서가 곧 전선 id 다(0 = 감시 초소 ... 5 = 해안선).")]
    [SerializeField] private List<Frontline> frontlines = new();

    [Tooltip("배율의 상한. 단계가 아무리 높아도 수치가 무한대로 넘어가지 않게 자른다.")]
    [SerializeField, Min(1f)] private float maxStatMultiplier = 1e15f;

    [Tooltip("전선에 배경이 없을 때 쓸 전장 배경. 비우면 씬에 깔린 배경을 그대로 쓴다.")]
    [SerializeField] private Sprite defaultBackground;

    [Tooltip("전선에 제한 시간을 따로 적지 않았을 때 쓰는 한 판의 제한 시간(초).")]
    [SerializeField, Min(1f)] private float defaultTimeLimitSeconds = 30f;

    public int Count => frontlines.Count;

    /// <summary>그 전선 한 판의 제한 시간(초).</summary>
    public float TimeLimitOf(int frontlineId) {
        var f = Get(frontlineId);
        return f != null && f.TimeLimitSeconds > 0f ? f.TimeLimitSeconds : defaultTimeLimitSeconds;
    }

    /// <summary>그 전선의 데이터. 없으면 null.</summary>
    public Frontline Get(int frontlineId) =>
        frontlineId >= 0 && frontlineId < frontlines.Count ? frontlines[frontlineId] : null;

    public string NameOf(int frontlineId) {
        var f = Get(frontlineId);
        return f != null && !string.IsNullOrEmpty(f.DisplayName) ? f.DisplayName : FrontlineRules.DefaultName(frontlineId);
    }

    public Sprite ThumbnailOf(int frontlineId) => Get(frontlineId)?.Thumbnail;

    public Sprite BackgroundOf(int frontlineId) {
        var f = Get(frontlineId);
        return f != null && f.Background != null ? f.Background : defaultBackground;
    }

    /// <summary>그 단계 몬스터의 체력 배율.</summary>
    public float HealthMultiplierFor(int frontlineId, int stage) {
        var f = Get(frontlineId);
        return f != null ? GrowthMultiplier(f.HealthGrowthPerStage, stage) : 1f;
    }

    /// <summary>그 단계 몬스터의 공격력/방어력 배율.</summary>
    public float AttackMultiplierFor(int frontlineId, int stage) {
        var f = Get(frontlineId);
        return f != null ? GrowthMultiplier(f.AttackGrowthPerStage, stage) : 1f;
    }

    // 1단계는 1배, 이후 단계마다 growth 를 한 번씩 더 곱한다. 상한을 넘으면 자른다.
    private float GrowthMultiplier(float growth, int stage) {
        double m = Math.Pow(growth, Mathf.Max(0, stage - 1));
        return (float)Math.Min(m, maxStatMultiplier);
    }

    /// <summary>한 판의 스폰 순서(구성을 펼쳐 섞은 것). 같은 종류가 몰려 나오지 않게 섞는다.</summary>
    public List<MonsterData> BuildShuffledSpawnQueue(int frontlineId) {
        var list = new List<MonsterData>();
        var f = Get(frontlineId);
        if (f == null) return list;

        foreach (var s in f.Spawns) {
            if (s.Monster == null) continue;
            for (int i = 0; i < Mathf.Max(1, s.Count); i++) list.Add(s.Monster);
        }
        for (int i = list.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
