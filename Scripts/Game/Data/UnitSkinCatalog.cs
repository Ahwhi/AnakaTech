using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 아웃핏(V1.12) 정적 데이터. 아웃핏은 전투 유닛 프리팹(CharacterUnit)을 통째로 바꾸는 장착 아이템이며
/// 장착 효과(스탯)도 함께 준다. 코스튬(일러스트 교체)과는 별개라 함께 낄 수 있다.
///
/// 서버는 id 와 보유 개수/장착만 알고, 프리팹/이름/아이콘/효과 수치는 이 SO 가 갖는다(코스튬과 같은 철학).
/// id 는 서버 UnitSkinCatalog 와 1:1로 약속한다. 새 아웃핏 = 서버에 id 추가 + 여기 항목 추가.
/// CostumeDatabase 에 할당해 쓴다(꾸미기 데이터라 같은 경로로 주입되어, 씬마다 따로 배선하지 않는다).
/// </summary>
[CreateAssetMenu(fileName = "UnitSkinCatalog", menuName = "Anaka/Database/Unit Skin Catalog")]
public sealed class UnitSkinCatalog : ScriptableObject {
    [Serializable]
    public sealed class Entry {
        [Tooltip("서버 UnitSkinCatalog 와 같은 id(1=빨강 2=사이언 3=노랑 4=초록 5=파랑).")]
        public int id;
        public string displayName;
        [Tooltip("상점/아웃핏 목록에 쓰는 아이콘.")]
        public Sprite icon;
        [Tooltip("이 아웃핏을 낀 소녀가 전투에 나설 때 쓰는 유닛 프리팹(기본 CharacterUnit 프리팹의 베리에이션).")]
        public CharacterUnit unitPrefab;
        [Tooltip("장착 효과(스탯 + 값). 성장/장비/각인과 같은 축으로 합산된다.")]
        public CostumeData.Effect[] effects = Array.Empty<CostumeData.Effect>();

        public CombatStats BonusStats {
            get {
                var sum = new CombatStats();
                if (effects == null) return sum;
                foreach (var e in effects) sum = sum.WithAdded(e.Stat, e.Value);
                return sum;
            }
        }

        /// <summary>효과 요약(한 줄에 하나). 효과가 없으면 빈 문자열.</summary>
        public string EffectSummary() {
            if (effects == null || effects.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < effects.Length; i++) {
                if (i > 0) sb.Append('\n');
                sb.Append(GrowthRules.StatName(effects[i].Stat))
                  .Append(" +")
                  .Append(StatFormat.FormatValue(effects[i].Stat, effects[i].Value));
            }
            return sb.ToString();
        }
    }

    [Tooltip("기본 유닛 프리팹(아웃핏 없음). 전투는 씬의 기본 프리팹을 쓰고, 이 값은 아웃핏 화면의 '기본' 미리보기에만 쓴다.")]
    [SerializeField] private CharacterUnit defaultUnitPrefab;
    [Tooltip("기본 유닛(아웃핏 없음) 아이콘. 아웃핏 화면의 '기본' 칸/상세에 쓴다.")]
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private List<Entry> entries = new();

    /// <summary>표시할 아이콘(0 이면 기본 유닛 아이콘).</summary>
    public Sprite IconFor(int id) {
        var e = Get(id);
        return e != null ? e.icon : defaultIcon;
    }

    private Dictionary<int, Entry> _byId;

    /// <summary>미리보기에 그릴 프리팹(0 이면 기본 유닛 프리팹).</summary>
    public CharacterUnit PreviewPrefabFor(int id) {
        var e = Get(id);
        return e != null && e.unitPrefab != null ? e.unitPrefab : defaultUnitPrefab;
    }

    /// <summary>등록 순서 그대로의 전체 목록(아웃핏 화면이 이 순서로 칸을 만든다).</summary>
    public IReadOnlyList<Entry> All => entries;

    /// <summary>id 의 아웃핏(0 이거나 없는 id 면 null = 기본 아웃핏).</summary>
    public Entry Get(int id) {
        if (id == 0) return null;
        if (_byId == null) {
            _byId = new Dictionary<int, Entry>(entries.Count);
            foreach (var e in entries) if (e != null) _byId[e.id] = e;
        }
        return _byId.TryGetValue(id, out var entry) ? entry : null;
    }

    /// <summary>아웃핏 장착 효과(기본 아웃핏/없는 id 면 0).</summary>
    public CombatStats BonusFor(int id) {
        var e = Get(id);
        return e != null ? e.BonusStats : new CombatStats();
    }

#if UNITY_EDITOR
    // 에셋 목록을 고친 뒤 인덱스가 옛 내용으로 남지 않게 한다.
    private void OnValidate() => _byId = null;
#endif
}
