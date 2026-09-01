using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 던전 정적 데이터 레지스트리. dungeonIndex -> DungeonData 조회.
/// 그룹 목록은 등록된 던전들의 Group 에서 유도하므로(별도 목록을 관리하지 않음) 던전에 그룹만 할당하면 자동 반영된다.
/// </summary>
[CreateAssetMenu(fileName = "DungeonDatabase", menuName = "Anaka/Database/Dungeon Database")]
public sealed class DungeonDatabase : ScriptableObject {
    [SerializeField] private List<DungeonData> dungeons = new();

    private Dictionary<int, DungeonData> _byIndex;
    private List<DungeonGroupData> _groups;
    private Dictionary<DungeonGroupData, List<DungeonData>> _byGroup;

    public IReadOnlyList<DungeonData> All => dungeons;

    /// <summary>등록된 던전들이 실제로 속한 그룹 목록(SortOrder -> GroupId 순). 그룹 미할당 던전은 포함되지 않는다.</summary>
    public IReadOnlyList<DungeonGroupData> Groups {
        get { EnsureIndex(); return _groups; }
    }

    public DungeonData Get(int dungeonIndex) {
        EnsureIndex();
        return _byIndex.TryGetValue(dungeonIndex, out var data) ? data : null;
    }

    /// <summary>해당 그룹에 속한 던전 목록(등록 순서 유지). 없으면 빈 목록.</summary>
    public IReadOnlyList<DungeonData> InGroup(DungeonGroupData group) {
        EnsureIndex();
        if (group != null && _byGroup.TryGetValue(group, out var list)) return list;
        return System.Array.Empty<DungeonData>();
    }

    /// <summary>던전이 속한 그룹(미할당이거나 없는 던전이면 null).</summary>
    public DungeonGroupData GroupOf(int dungeonIndex) {
        var d = Get(dungeonIndex);
        return d != null ? d.Group : null;
    }

    private void EnsureIndex() {
        if (_byIndex != null) return;
        _byIndex = new Dictionary<int, DungeonData>(dungeons.Count);
        _groups = new List<DungeonGroupData>();
        _byGroup = new Dictionary<DungeonGroupData, List<DungeonData>>();

        for (int i = 0; i < dungeons.Count; i++) {
            var d = dungeons[i];
            if (d == null) continue;
            _byIndex[d.DungeonIndex] = d;

            var g = d.Group;
            if (g == null) continue;
            if (!_byGroup.TryGetValue(g, out var list)) {
                list = new List<DungeonData>();
                _byGroup.Add(g, list);
                _groups.Add(g);
            }
            list.Add(d);
        }

        _groups.Sort(CompareGroup);
    }

    private static int CompareGroup(DungeonGroupData a, DungeonGroupData b) {
        int order = a.SortOrder.CompareTo(b.SortOrder);
        return order != 0 ? order : a.GroupId.CompareTo(b.GroupId);
    }

    private void OnValidate() {
        _byIndex = null;
        _groups = null;
        _byGroup = null;
    }
}
