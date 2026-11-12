using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 멸망전 페이즈 목록. 페이즈 번호로 조회하며, 없는 번호를 물으면 null 을 돌려준다.
/// 데이터가 끝나는 지점이 곧 "지금 만들어 둔 마지막 페이즈"이고, 거기까지 깨면 화면이 다음 업데이트를 안내한다.
/// </summary>
[CreateAssetMenu(fileName = "DoomDatabase", menuName = "Anaka/Database/Doom")]
public sealed class DoomDatabase : ScriptableObject {
    [Tooltip("페이즈 목록. 순서는 상관없다(Phase 값으로 정렬해 쓴다).")]
    [SerializeField] private List<DoomPhaseData> phases = new();

    [Header("Endless (만들어 둔 마지막 페이즈 이후)")]
    [Tooltip("자동으로 이어 붙일 마지막 페이즈 번호. 만들어 둔 마지막 페이즈 다음부터 여기까지는 마지막 페이즈의 구성을 그대로 쓰고 " +
             "몬스터 공격력/방어력/체력만 페이즈마다 배율을 곱한다. 만들어 둔 마지막 페이즈 이하면 이어 붙이지 않는다.")]
    [SerializeField, Min(0)] private int endlessLastPhase = 100;
    [Tooltip("이어 붙인 페이즈 1개마다 몬스터 공격력/방어력/체력에 곱하는 배율(1.5 = 페이즈마다 1.5배).")]
    [SerializeField, Min(1f)] private float endlessStatMultiplier = 1.5f;

    private Dictionary<int, DoomPhaseData> _byPhase;
    private int _lastAuthored;

    /// <summary>직접 만들어 둔 마지막 페이즈 번호(0 = 데이터 없음). 이 뒤는 자동으로 이어 붙인 페이즈다.</summary>
    public int LastAuthoredPhase {
        get { Build(); return _lastAuthored; }
    }

    /// <summary>플레이할 수 있는 마지막 페이즈 번호(이어 붙인 페이즈 포함, 0 = 데이터 없음).</summary>
    public int LastPhase {
        get {
            Build();
            return _lastAuthored > 0 ? Mathf.Max(_lastAuthored, endlessLastPhase) : 0;
        }
    }

    /// <summary>모든 페이즈를 합친 총 몬스터 수(= 지금 데이터로 낼 수 있는 최고 점수). 이어 붙인 페이즈 포함.</summary>
    public int TotalMonsters {
        get {
            int total = 0;
            for (int p = 1; p <= LastPhase; p++) {
                var data = Get(p);
                if (data != null) total += data.TotalMonsters;
            }
            return total;
        }
    }

    /// <summary>
    /// 그 페이즈의 데이터. 만들어 둔 페이즈면 그 데이터, 이어 붙인 페이즈면 만들어 둔 마지막 페이즈의 데이터(같은 구성)다.
    /// 이어 붙인 페이즈의 몬스터 강화는 StatMultiplier 로 따로 받는다.
    /// </summary>
    public DoomPhaseData Get(int phase) {
        Build();
        if (_byPhase.TryGetValue(phase, out var data)) return data;
        if (_lastAuthored > 0 && phase > _lastAuthored && phase <= endlessLastPhase) return _byPhase[_lastAuthored];
        return null;
    }

    /// <summary>
    /// 그 페이즈 몬스터의 공격력/방어력/체력 배율. 만들어 둔 페이즈는 1, 이어 붙인 페이즈는
    /// 마지막으로 만들어 둔 페이즈에서 한 칸 멀어질 때마다 endlessStatMultiplier 를 한 번 더 곱한다.
    /// </summary>
    public float StatMultiplier(int phase) {
        Build();
        if (_lastAuthored <= 0 || phase <= _lastAuthored) return 1f;
        return Mathf.Pow(endlessStatMultiplier, phase - _lastAuthored);
    }

    /// <summary>그 페이즈가 마지막인지(다음 데이터가 없는지).</summary>
    public bool IsLast(int phase) => Get(phase + 1) == null;

    private void Build() {
        if (_byPhase != null) return;
        _byPhase = new Dictionary<int, DoomPhaseData>(phases.Count);
        foreach (var p in phases) {
            if (p == null) continue;
            // 같은 번호가 둘이면 앞의 것을 남긴다. 조용히 덮어쓰면 어느 쪽이 쓰였는지 알 수 없다.
            if (_byPhase.ContainsKey(p.Phase)) {
                Debug.LogWarning($"[Doom] 페이즈 번호가 겹칩니다: {p.Phase} ({p.name})", this);
                continue;
            }
            _byPhase[p.Phase] = p;
            if (p.Phase > _lastAuthored) _lastAuthored = p.Phase;
        }
    }

#if UNITY_EDITOR
    // 인스펙터에서 목록을 고치면 인덱스를 다시 만든다.
    private void OnValidate() {
        _byPhase = null;
        _lastAuthored = 0;
    }
#endif
}
