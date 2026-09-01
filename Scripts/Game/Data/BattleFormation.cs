using System;
using UnityEngine;

/// <summary>
/// 전투 시작 시 유닛을 중심점 둘레의 원 위에 배치하는 규격(캐릭터 = 안쪽 작은 원, 몬스터 = 바깥 큰 원).
/// 값의 정의를 한 곳에 모아 두고 두 군데에서 같은 모양으로 쓴다:
///  - BattleSettings.Formation : 모든 던전의 공용 기본값
///  - DungeonData.FormationOverride : 특정 던전만 다르게 배치할 때의 오버라이드(체크박스로 사용 여부 결정)
/// 배치 해석은 BattleDirector 가 런 시작 시 한 번 수행한다(오버라이드가 없으면 공용 기본값).
/// </summary>
[Serializable]
public struct BattleFormation {
    [Tooltip("캐릭터가 중심점 둘레에 배치되는 원의 반지름(안쪽 작은 원).")]
    public float CharacterRingRadius;
    [Tooltip("캐릭터 배치 반지름 지터(원 위에서 안팎으로 흔들리는 범위).")]
    public float CharacterRingJitter;
    [Tooltip("몬스터가 중심점 둘레에 배치되는 원의 반지름(캐릭터를 감싸는 바깥 큰 원). " +
             "크게 잡으면 몬스터가 걸어 들어오는 시간이 늘어 런 클리어가 느려진다.")]
    public float MonsterRingRadius;
    [Tooltip("몬스터 배치 반지름 지터.")]
    public float MonsterRingJitter;

    [Header("Spawn Arc (몬스터가 나타나는 방향)")]
    [Tooltip("몬스터 스폰 각도 구간의 개수와 범위. 비우면(길이 0) 전방위(0~360도) 랜덤이다. " +
             "여러 구간을 두면 그 중 하나를 균등하게 골라 그 안에서 랜덤 각도를 뽑는다. " +
             "예: 좌우 양쪽에서만 몰려오게 하려면 (-30,30) 과 (150,210) 두 구간을 둔다(호위 의뢰).")]
    public SpawnArc[] SpawnArcs;

    /// <summary>몬스터 스폰 각도 구간 1개(도 단위, 월드 기준 반시계). Min &gt; Max 여도 자동으로 정렬해 쓴다.</summary>
    [Serializable]
    public struct SpawnArc {
        public float MinAngle;
        public float MaxAngle;

        public SpawnArc(float minAngle, float maxAngle) { MinAngle = minAngle; MaxAngle = maxAngle; }
    }

    /// <summary>이번 스폰에 쓸 각도(도). 구간이 없으면 전방위 랜덤이다.</summary>
    public float RandomSpawnAngle() {
        if (SpawnArcs == null || SpawnArcs.Length == 0)
            return UnityEngine.Random.Range(0f, 360f);

        var arc = SpawnArcs[UnityEngine.Random.Range(0, SpawnArcs.Length)];
        float min = Mathf.Min(arc.MinAngle, arc.MaxAngle);
        float max = Mathf.Max(arc.MinAngle, arc.MaxAngle);
        // 구간 폭이 0이면(설정 실수) 그 각도로 고정 스폰된다. 전방위로 조용히 되돌리지 않고 그대로 두어 설정 의도를 보존한다.
        return UnityEngine.Random.Range(min, max);
    }

    /// <summary>신규 에셋/미설정 필드의 시작값. 인스펙터에서 오버라이드를 켰을 때 반지름 0(전원 중앙 겹침)으로
    /// 시작하지 않도록 실제 운용값에 가까운 기본을 준다.</summary>
    public static BattleFormation Default => new BattleFormation {
        CharacterRingRadius = 2f,
        CharacterRingJitter = 0.35f,
        MonsterRingRadius = 30f,
        MonsterRingJitter = 10f,
        SpawnArcs = Array.Empty<SpawnArc>()
    };
}
