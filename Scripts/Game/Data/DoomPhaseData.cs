using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 멸망전 한 페이즈의 데이터. 의뢰(CommissionData)의 토벌과 같은 모양이라 몬스터 구성을 같은 감각으로 짠다.
///
/// 페이즈는 "여기 있는 몬스터를 전부 잡으면 다음" 이다. 제한 시간 안에 다 잡지 못하면 그 자리에서 패배하고,
/// 그때까지 처치한 누적 수가 점수가 된다.
///
/// 동시 등장 수(maxConcurrentMonsters)를 반드시 정할 것. 멸망전은 내 소녀가 최대 40명까지 나오므로
/// 몬스터까지 한꺼번에 쏟아지면 중급 기기에서 프레임이 무너진다. 총량은 spawns 로 정하고
/// 화면에 동시에 몇 마리까지 둘지는 이 값으로 조인다.
/// </summary>
[CreateAssetMenu(fileName = "DoomPhase", menuName = "Anaka/Data/Doom Phase")]
public sealed class DoomPhaseData : ScriptableObject {
    [Serializable]
    public struct Spawn {
        public MonsterData Monster;
        [Min(1)] public int Count;
    }

    [Header("Identity")]
    [Tooltip("페이즈 번호(1부터). 데이터베이스가 이 값으로 순서를 잡으므로 빠지거나 겹치지 않게 둔다.")]
    [SerializeField, Min(1)] private int phase = 1;

    [Tooltip("화면에 표시할 이름(비우면 'N 페이즈').")]
    [SerializeField] private string displayName;

    [Header("Battle")]
    [Tooltip("이 페이즈의 제한 시간(초). 안에 다 잡지 못하면 패배한다.")]
    [SerializeField, Min(1f)] private float timeLimitSeconds = 60f;

    [Tooltip("이 페이즈에서 나오는 몬스터 전체. 여기 적힌 수를 전부 처치해야 다음 페이즈로 넘어간다.")]
    [SerializeField] private List<Spawn> spawns = new();

    [Tooltip("동시에 화면에 둘 최대 몬스터 수. 0 이면 제한 없음(권장하지 않음).")]
    [SerializeField, Min(0)] private int maxConcurrentMonsters = 20;

    [Tooltip("몬스터를 채워 넣는 간격(초). 0 이면 자리가 비는 즉시 채운다.")]
    [SerializeField, Min(0f)] private float spawnIntervalSeconds = 0.5f;

    [Header("Visual")]
    [Tooltip("이 페이즈의 배경(비우면 직전 페이즈의 배경을 그대로 쓴다).")]
    [SerializeField] private Sprite backgroundSprite;

    public int Phase => Mathf.Max(1, phase);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? $"{Phase} 페이즈" : displayName;
    public float TimeLimitSeconds => timeLimitSeconds;
    public IReadOnlyList<Spawn> Spawns => spawns;
    public int MaxConcurrentMonsters => maxConcurrentMonsters;
    public float SpawnIntervalSeconds => spawnIntervalSeconds;
    public Sprite BackgroundSprite => backgroundSprite;

    /// <summary>이 페이즈에서 처치해야 하는 총 마리 수(= 이 페이즈가 주는 점수).</summary>
    public int TotalMonsters {
        get {
            int total = 0;
            foreach (var s in spawns)
                if (s.Monster != null) total += Mathf.Max(1, s.Count);
            return total;
        }
    }
}
