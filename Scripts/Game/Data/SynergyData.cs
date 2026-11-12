using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시너지 1종의 정적 데이터. 시너지는 "정해진 캐릭터들이 한 덱에 모두 모이면 편성 전원이 효과를 받는" 규칙이다.
/// 조건을 이룬 캐릭터만이 아니라 그 덱에 편성된 인원 전부가 보정을 받는다.
///
/// 설계 의도(비공개 조합): 시너지 조건은 유저에게 미리 알려 주지 않는다. 편성을 이리저리 바꿔 보다가
/// 우연히 맞으면 활성화 목록에 나타나고, 그것을 유저들끼리 공유하게 하는 것이 이 시스템의 재미다.
/// 그래서 어떤 화면도 "미발견 시너지 목록"을 그리지 않으며, 활성화된 것만 이름과 효과를 보여 준다.
///
/// 구성 인원은 2~5명으로 자유롭게 둘 수 있다(덱 정원이 5명이므로 5명이 상한이다).
/// 예외로 등급 시너지(Condition = GradeCount)는 특정 캐릭터가 아니라 "그 등급 캐릭터 N 명 이상"을 조건으로 한다.
/// 효과는 코스튬과 같은 축(스탯 + 값)이라 장비/각인/코스튬과 같은 경로로 합산된다.
/// </summary>
[CreateAssetMenu(fileName = "SynergyData", menuName = "Anaka/Data/Synergy")]
public sealed class SynergyData : ScriptableObject {
    /// <summary>활성화 조건 종류. 값이 직렬화되므로 순서를 바꾸지 말고 새 종류는 아래에 추가한다.</summary>
    public enum ConditionKind {
        /// <summary>정해진 캐릭터들(Members)이 덱에 전부 있어야 한다(기본).</summary>
        Members = 0,
        /// <summary>정해진 등급(Grade) 캐릭터가 덱에 Grade Count 명 이상 있어야 한다. 같은 등급끼리는 가장 높은 단계만 걸린다.</summary>
        GradeCount = 1,
    }

    /// <summary>시너지 효과 1줄(스탯 1종 + 값). 코스튬 효과와 같은 규약이다.</summary>
    [Serializable]
    public struct Effect {
        [Tooltip("올릴 스탯. 단위는 CombatStats 주석의 규약을 따른다(예: 공격속도는 %p).")]
        public StatType Stat;
        [Tooltip("더해질 값. 시너지가 걸린 덱의 편성 전원에게 더해진다.")]
        public float Value;
    }

    [Header("Identity")]
    [Tooltip("시너지 고유 id. 활성화 판정에는 쓰이지 않지만 로그/저장에 쓸 수 있도록 둔다. 재사용 금지.")]
    [SerializeField] private int id;
    [Tooltip("시너지 이름. 활성화되면 편성 화면과 토스트에 이 이름이 나온다.")]
    [SerializeField] private string displayName;

    [Header("Condition")]
    [Tooltip("Members = 아래 캐릭터들이 전부 있어야 활성화(기본). GradeCount = 정해진 등급 캐릭터가 정해진 수 이상이면 활성화.")]
    [SerializeField] private ConditionKind condition = ConditionKind.Members;

    [Header("Members (Condition = Members)")]
    [Tooltip("이 시너지를 이루는 캐릭터들. 여기 있는 캐릭터가 덱에 전부 있어야 활성화된다. " +
             "2~5명으로 자유롭게 구성한다(덱 정원이 5명이라 5가 상한). 중복/빈 칸은 무시된다.")]
    [SerializeField] private CharacterData[] members = Array.Empty<CharacterData>();

    [Header("Grade (Condition = GradeCount)")]
    [Tooltip("세는 등급. 이 등급과 정확히 같은 캐릭터만 센다(상위 등급은 세지 않는다).")]
    [SerializeField] private Grade grade;
    [Tooltip("필요 인원(2~5). 이 수 이상이면 활성화된다. 같은 등급의 더 높은 단계가 함께 걸리면 이 시너지는 빠진다.")]
    [SerializeField, Range(2, 5)] private int gradeCount = 3;

    [Header("Effect")]
    [Tooltip("활성화 시 편성 전원이 받는 스탯 보정(조건을 이룬 캐릭터만이 아니다).")]
    [SerializeField] private Effect[] effects = Array.Empty<Effect>();

    [Header("Description")]
    [Tooltip("효과 설명 문구. 비우면 효과 목록으로 자동 생성한다(스탯 이름 + 값).")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    // 활성화 판정은 편성이 바뀔 때마다 돌므로 id 배열을 1회 만들어 재사용한다(에셋 참조 순회를 매번 하지 않는다).
    private int[] _memberIds;

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public Effect[] Effects => effects;

    /// <summary>이 시너지를 이루는 캐릭터 id들(중복/빈 칸 제거). 활성화 판정의 기준이다.</summary>
    public IReadOnlyList<int> MemberIds {
        get {
            if (_memberIds != null) return _memberIds;

            var list = new List<int>(members != null ? members.Length : 0);
            if (members != null) {
                foreach (var m in members) {
                    if (m == null || list.Contains(m.Id)) continue;
                    list.Add(m.Id);
                }
            }
            _memberIds = list.ToArray();
            return _memberIds;
        }
    }

    public ConditionKind Condition => condition;
    public bool IsGradeCount => condition == ConditionKind.GradeCount;
    public Grade Grade => grade;
    public int GradeCount => Mathf.Clamp(gradeCount, 2, 5);

    /// <summary>구성 인원 수(2 미만이면 시너지로 성립하지 않는다). 등급 시너지는 필요 인원이다.</summary>
    public int MemberCount => IsGradeCount ? GradeCount : MemberIds.Count;

    /// <summary>
    /// 이 덱에서 조건을 채우는지(같은 등급 시너지끼리 가장 높은 단계만 남기는 정리는 SynergyDatabase 가 한다).
    ///   Members    구성원이 전부 편성돼 있어야 한다. 구성원이 2명 미만인 에셋은 활성화시키지 않는다(설정 실수 방어).
    ///   GradeCount 그 등급 캐릭터가 필요 인원 이상이어야 한다. 등급은 charDb 로 찾는다(없으면 활성화하지 않는다).
    /// </summary>
    public bool IsActiveIn(IReadOnlyList<int> deckCharIds, CharacterDatabase charDb) {
        if (IsGradeCount) return CountGrade(deckCharIds, charDb) >= GradeCount;

        var ids = MemberIds;
        if (ids.Count < 2 || deckCharIds == null || deckCharIds.Count < ids.Count) return false;

        for (int i = 0; i < ids.Count; i++) {
            bool found = false;
            for (int j = 0; j < deckCharIds.Count; j++) {
                if (deckCharIds[j] != ids[i]) continue;
                found = true;
                break;
            }
            if (!found) return false;
        }
        return true;
    }

    // 덱에서 이 등급 캐릭터 수(중복 id 는 한 번만 센다).
    private int CountGrade(IReadOnlyList<int> deckCharIds, CharacterDatabase charDb) {
        if (deckCharIds == null || charDb == null) return 0;
        int count = 0;
        for (int i = 0; i < deckCharIds.Count; i++) {
            int id = deckCharIds[i];
            if (id == 0) continue;
            bool dup = false;
            for (int j = 0; j < i; j++) if (deckCharIds[j] == id) { dup = true; break; }
            if (dup) continue;
            var data = charDb.Get(id);
            if (data != null && data.Grade == grade) count++;
        }
        return count;
    }

    /// <summary>편성 전원이 받는 보정 합계. 장비/각인/코스튬과 같은 축이라 그대로 더해진다.</summary>
    public CombatStats BonusStats {
        get {
            var sum = new CombatStats();
            if (effects == null) return sum;
            foreach (var e in effects) sum = sum.WithAdded(e.Stat, e.Value);
            return sum;
        }
    }

    /// <summary>효과 요약(스탯 + 값). 활성화 목록과 토스트가 공통으로 쓴다.</summary>
    public string EffectSummary() {
        if (!string.IsNullOrEmpty(description)) return description;
        if (effects == null || effects.Length == 0) return "";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < effects.Length; i++) {
            if (i > 0) sb.Append('\n');
            sb.Append(GrowthRules.StatName(effects[i].Stat))
              .Append(" +")
              .Append(StatFormat.FormatValue(effects[i].Stat, effects[i].Value));
        }
        return sb.ToString();
    }

    // 인스펙터에서 구성원을 바꾸면 캐시를 버린다(플레이 중 편집 대비).
    private void OnValidate() => _memberIds = null;
}
