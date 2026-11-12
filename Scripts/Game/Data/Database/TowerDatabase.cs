using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무한 타워 층 구성. 1~10층(템플릿)만 직접 짜 두면 그 뒤는 같은 구성을 반복하며 몬스터만 강해진다.
///   11층 = 1층 구성 x 배율 1회, 12층 = 2층 구성 x 배율 1회, ..., 21층 = 1층 구성 x 배율 2회 ...
/// 배율은 공격력/체력/방어력에만 곱하며, 반복할 때마다 한 번 더 곱한다(cycleStatMultiplier 의 반복 횟수 제곱).
///
/// 층은 전투 시간(50초)을 버티면 깨지므로, 몬스터는 다 잡아도 끝나지 않고 구성을 처음부터 다시 내보낸다(loopSpawns).
/// </summary>
[CreateAssetMenu(fileName = "TowerDatabase", menuName = "Anaka/Database/Tower")]
public sealed class TowerDatabase : ScriptableObject {
    [Serializable]
    public struct Spawn {
        public MonsterData Monster;
        [Min(1)] public int Count;
    }

    [Serializable]
    public sealed class FloorTemplate {
        [Tooltip("이 층에서 나오는 몬스터. 다 잡으면 같은 구성을 다시 내보낸다(Loop Spawns 가 켜져 있을 때).")]
        public List<Spawn> Spawns = new();

        [Tooltip("동시에 화면에 둘 최대 몬스터 수. 0 이면 전투 설정(BattleSettings)의 값을 쓴다.")]
        [Min(0)] public int MaxConcurrentMonsters;

        [Tooltip("몬스터를 채워 넣는 간격(초). 0 이면 전투 설정(BattleSettings)의 값을 쓴다.")]
        [Min(0f)] public float SpawnIntervalSeconds;

        [Tooltip("이 층의 배경. 비우면 Default Background 를 쓴다.")]
        public Sprite Background;
    }

    [Tooltip("1층부터 차례로 짠 층 구성(템플릿). 개수만큼 반복 주기가 된다(10개면 10층마다 반복).")]
    [SerializeField] private List<FloorTemplate> floors = new();

    [Tooltip("템플릿을 한 바퀴 돌 때마다 몬스터 공격력/체력/방어력에 곱하는 배율(2 = 반복할 때마다 2배).")]
    [SerializeField, Min(1f)] private float cycleStatMultiplier = 2f;

    [Tooltip("배율의 상한. 반복이 아주 길어져도 수치가 무한대로 넘어가지 않게 자른다.")]
    [SerializeField, Min(1f)] private float maxStatMultiplier = 1e15f;

    [Tooltip("층 구성을 다 잡았을 때 처음부터 다시 내보낼지. 버티기 컨텐츠라 기본은 켠다.")]
    [SerializeField] private bool loopSpawns = true;

    [Tooltip("층 템플릿에 배경이 없을 때 쓸 전장 배경. 비우면 씬에 깔린 배경을 그대로 쓴다.")]
    [SerializeField] private Sprite defaultBackground;

    public int TemplateCount => floors.Count;
    public bool LoopSpawns => loopSpawns;

    /// <summary>그 층이 쓰는 템플릿. 데이터가 없으면 null.</summary>
    public FloorTemplate TemplateFor(int floor) {
        if (floors.Count == 0) return null;
        return floors[(Mathf.Max(1, floor) - 1) % floors.Count];
    }

    /// <summary>그 층이 템플릿을 몇 바퀴 돈 자리인지(1~10층 = 0).</summary>
    public int CycleOf(int floor) => floors.Count == 0 ? 0 : (Mathf.Max(1, floor) - 1) / floors.Count;

    /// <summary>그 층 몬스터의 공격력/체력/방어력 배율.</summary>
    public float StatMultiplierFor(int floor) {
        double m = Math.Pow(cycleStatMultiplier, CycleOf(floor));
        return (float)Math.Min(m, maxStatMultiplier);
    }

    public Sprite BackgroundFor(int floor) {
        var t = TemplateFor(floor);
        return t != null && t.Background != null ? t.Background : defaultBackground;
    }

    /// <summary>그 층 구성의 총 마리 수(0 이면 비어 있는 층).</summary>
    public int MonsterCountFor(int floor) {
        var t = TemplateFor(floor);
        if (t == null) return 0;
        int total = 0;
        foreach (var s in t.Spawns)
            if (s.Monster != null) total += Mathf.Max(1, s.Count);
        return total;
    }
}
