using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 의뢰 1단계의 정적 데이터. 세 종류(레이드/토벌/호위)가 같은 에셋 형식을 쓰고, 종류에 따라 읽는 필드만 달라진다.
///  - 레이드   : Spawns 의 첫 항목을 보스로 쓴다(수량 무시). 진행도 = 보스 체력.
///  - 토벌     : Spawns 의 수량 합이 처치 목표다. 진행도 = 처치 수 / 목표.
///  - 호위     : Spawns 를 제한 시간 동안 반복 스폰하고 EscortPrefab 을 중앙에 세운다. 진행도 = 의뢰인 체력.
///
/// 보상은 여기 두지 않는다. 지급은 서버 CommissionCatalog 가 단일 소스이고, 표시는 클라 CommissionRewardRules 가 미러한다.
/// (보상을 SO 에 두면 서버 값과 어긋난 채로 "표시만 다른" 상태가 생긴다.)
/// </summary>
[CreateAssetMenu(fileName = "CommissionData", menuName = "Anaka/Data/Commission")]
public sealed class CommissionData : ScriptableObject {
    /// <summary>스폰 항목(몬스터 + 마리 수). 종류에 따라 "총 등장 수" 또는 "웨이브 비율"로 해석된다.</summary>
    [Serializable]
    public struct Spawn {
        public MonsterData Monster;
        [Min(1)] public int Count;
    }

    [Header("Identity")]
    [Tooltip("의뢰 종류. 진행/실패 규칙과 상단 게이지 표시가 이 값으로 결정된다.")]
    [SerializeField] private CommissionType type;
    [Tooltip("단계(1-based). 서버 진행도(clearedStage)와 같은 값을 쓴다.")]
    [SerializeField, Min(1)] private int stage = 1;
    [Tooltip("표시 이름. 비우면 '<종류> N단계'로 자동 생성한다.")]
    [SerializeField] private string displayName;

    [Header("Battle")]
    [SerializeField] private Sprite backgroundSprite;
    [Tooltip("제한 시간(초). 0 이하면 시간 제한 없음(현재 세 종류 모두 제한 시간을 쓴다).")]
    [SerializeField, Min(0f)] private float timeLimitSeconds = 30f;
    [Tooltip("등장 몬스터. 레이드는 첫 항목이 보스, 토벌은 수량 합이 목표 처치 수, 호위는 웨이브 풀이다.")]
    [SerializeField] private List<Spawn> spawns = new();
    [Tooltip("동시에 전장에 존재할 수 있는 몬스터 수. 0 이하면 BattleSettings 의 공용 상한을 쓴다.")]
    [SerializeField, Min(0)] private int maxConcurrentMonsters;
    [Tooltip("스폰 주기(초). 0 이하면 BattleSettings 의 공용 주기를 쓴다.")]
    [SerializeField, Min(0f)] private float spawnIntervalSeconds;

    [Header("Formation Override (선택)")]
    [Tooltip("끄면 BattleSettings 의 공용 배치를 그대로 쓴다. 켜면 이 의뢰만 아래 값으로 배치한다 " +
             "(보스방처럼 가깝게, 호위처럼 좌우에서만 나오게 등).")]
    [SerializeField] private bool overrideFormation;
    [SerializeField] private BattleFormation formation = BattleFormation.Default;

    [Header("Escort (호위 의뢰 전용)")]
    [Tooltip("전장 중앙에 세울 의뢰인 유닛 프리팹(EscortUnit). 호위 의뢰가 아니면 비워 둔다.")]
    [SerializeField] private EscortUnit escortPrefab;
    [Tooltip("의뢰인의 체력. 회복 수단이 없으므로 이 값이 곧 허용 실수량이다.")]
    [SerializeField, Min(1f)] private float escortHealth = 1000f;
    [Tooltip("의뢰인의 방어력. 캐릭터 방어 스탯과 같은 규칙으로 피해를 깎는다.")]
    [SerializeField, Min(0f)] private float escortDefense;

    public CommissionType Type => type;
    public int Stage => Mathf.Max(1, stage);
    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? $"{CommissionRewardRules.TypeName(type)} {Stage}단계" : displayName;
    public Sprite BackgroundSprite => backgroundSprite;
    public float TimeLimitSeconds => timeLimitSeconds;
    public IReadOnlyList<Spawn> Spawns => spawns;
    public int MaxConcurrentMonsters => maxConcurrentMonsters;
    public float SpawnIntervalSeconds => spawnIntervalSeconds;
    public EscortUnit EscortPrefab => escortPrefab;
    public float EscortHealth => escortHealth;
    public float EscortDefense => escortDefense;

    /// <summary>이 의뢰 전용 배치(오버라이드 미사용이면 null = 호출측이 공용 기본값을 쓴다).</summary>
    public BattleFormation? FormationOverride => overrideFormation ? formation : (BattleFormation?)null;

    /// <summary>레이드 의뢰의 보스(첫 스폰 항목). 없으면 null.</summary>
    public MonsterData Boss {
        get {
            for (int i = 0; i < spawns.Count; i++)
                if (spawns[i].Monster != null) return spawns[i].Monster;
            return null;
        }
    }

    /// <summary>스폰 수량의 합(토벌 의뢰의 처치 목표).</summary>
    public int TotalMonsterCount {
        get {
            int total = 0;
            for (int i = 0; i < spawns.Count; i++)
                if (spawns[i].Monster != null) total += Mathf.Max(1, spawns[i].Count);
            return total;
        }
    }

    /// <summary>비율대로 채운 뒤 무작위로 섞은 스폰 순서(토벌 의뢰의 등장 순서).</summary>
    public List<MonsterData> BuildShuffledSpawnQueue() {
        var list = new List<MonsterData>(TotalMonsterCount);
        for (int i = 0; i < spawns.Count; i++) {
            var e = spawns[i];
            if (e.Monster == null) continue;
            for (int n = 0; n < Mathf.Max(1, e.Count); n++) list.Add(e.Monster);
        }
        for (int i = list.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    /// <summary>수량 가중치로 몬스터 1종을 뽑는다(호위 의뢰의 무한 웨이브). 비어 있으면 null.</summary>
    public MonsterData PickWeightedMonster() {
        int total = TotalMonsterCount;
        if (total <= 0) return null;

        int roll = UnityEngine.Random.Range(0, total);
        for (int i = 0; i < spawns.Count; i++) {
            var e = spawns[i];
            if (e.Monster == null) continue;
            roll -= Mathf.Max(1, e.Count);
            if (roll < 0) return e.Monster;
        }
        return null;
    }
}
