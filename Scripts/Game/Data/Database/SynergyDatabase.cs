using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 시너지 정적 데이터 레지스트리. MainLifetimeScope 인스펙터에 할당해 DI 로 주입한다(키 방식 아님, 규칙 5).
///
/// 시너지가 걸리면 편성 전원이 효과를 받는다(조건을 이룬 캐릭터만이 아니다).
/// 조건은 두 종류다(SynergyData.ConditionKind).
///   Members    정해진 캐릭터들이 전부 모이면 활성화.
///   GradeCount 정해진 등급 캐릭터가 N 명 이상이면 활성화. 같은 등급의 여러 단계(예: N3/N4/N5)가 함께 조건을 채우면
///              가장 높은 단계 하나만 남긴다. 그래서 활성화 목록과 보정 합계가 같은 정리 규칙(CollectActive)을 거친다.
///
/// 시너지는 서버가 알지 못한다. 조건(구성 캐릭터/등급)과 효과가 전부 밸런스 수치이고, 판정 근거인 "덱 구성"은
/// 서버가 이미 권위로 들고 있어 클라가 새로 만들어 낼 수 있는 값이 아니기 때문이다. 그래서 각인/코스튬처럼
/// 원본 상태에서 효과를 조립하는 규칙(CharacterBuild)에 얹으면 내 파티와 대전 상대가 자동으로 같은 결과를 낸다.
/// </summary>
[CreateAssetMenu(fileName = "SynergyDatabase", menuName = "Anaka/Database/Synergy Database")]
public sealed class SynergyDatabase : ScriptableObject {
    [SerializeField] private List<SynergyData> synergies = new();

    [Tooltip("등급 시너지(GradeCount)가 캐릭터 등급을 찾는 데 쓴다. 비우면 등급 시너지가 활성화되지 않는다.")]
    [SerializeField] private CharacterDatabase characterDatabase;

    // 보정 합계 계산용 재사용 버퍼(편성이 바뀔 때마다 도는 경로라 매번 할당하지 않는다).
    private readonly List<SynergyData> _buffer = new();
    private bool _warnedNoCharDb;

    public IReadOnlyList<SynergyData> All => synergies;

    public SynergyData Get(int id) {
        for (int i = 0; i < synergies.Count; i++) {
            var s = synergies[i];
            if (s != null && s.Id == id) return s;
        }
        return null;
    }

    /// <summary>
    /// 이 덱에서 활성화되는 시너지들을 목록에 채운다(호출부가 버퍼를 들고 있어 매 판정마다 할당하지 않는다).
    /// 등급 시너지는 같은 등급 중 가장 높은 단계만 남긴다.
    /// </summary>
    public void CollectActive(IReadOnlyList<int> deckCharIds, List<SynergyData> result) {
        if (result == null) return;
        result.Clear();
        if (deckCharIds == null || deckCharIds.Count == 0) return;

        WarnIfNoCharacterDatabase();
        for (int i = 0; i < synergies.Count; i++) {
            var s = synergies[i];
            if (s != null && s.IsActiveIn(deckCharIds, characterDatabase)) result.Add(s);
        }
        RemoveLowerGradeTiers(result);
    }

    /// <summary>
    /// 이 덱에서 지정 캐릭터가 받는 시너지 보정 합계.
    ///
    /// 시너지가 걸리면 조건을 이룬 캐릭터뿐 아니라 편성 전원이 효과를 받는다. 그래서 이 캐릭터가
    /// 시너지 구성원인지는 보지 않고 "덱에 편성돼 있는지"만 본다. 편성에 없는 캐릭터(덱 화면에서
    /// 구경만 하는 캐릭터)까지 보정을 받으면 표시 전투력이 실제 전투와 어긋나므로 그것만 걸러 낸다.
    /// </summary>
    public CombatStats BonusFor(int charDataId, IReadOnlyList<int> deckCharIds) {
        var sum = new CombatStats();
        if (charDataId == 0 || deckCharIds == null || deckCharIds.Count == 0) return sum;

        bool inDeck = false;
        for (int i = 0; i < deckCharIds.Count; i++) {
            if (deckCharIds[i] != charDataId) continue;
            inDeck = true;
            break;
        }
        if (!inDeck) return sum;

        CollectActive(deckCharIds, _buffer);
        for (int i = 0; i < _buffer.Count; i++) sum += _buffer[i].BonusStats;
        _buffer.Clear();
        return sum;
    }

    // 같은 등급의 등급 시너지가 여럿 걸렸으면 필요 인원이 가장 많은 것 하나만 남긴다(N5 가 걸리면 N3/N4 는 뺀다).
    private static void RemoveLowerGradeTiers(List<SynergyData> active) {
        for (int i = active.Count - 1; i >= 0; i--) {
            var s = active[i];
            if (!s.IsGradeCount) continue;
            for (int j = 0; j < active.Count; j++) {
                var other = active[j];
                if (other == s || !other.IsGradeCount || other.Grade != s.Grade) continue;
                // 필요 인원이 더 많거나, 같으면 목록 앞쪽 것을 남긴다(같은 단계 중복 설정 방어).
                if (other.GradeCount > s.GradeCount || (other.GradeCount == s.GradeCount && j < i)) {
                    active.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void WarnIfNoCharacterDatabase() {
        if (characterDatabase != null || _warnedNoCharDb) return;
        foreach (var s in synergies) {
            if (s == null || !s.IsGradeCount) continue;
            _warnedNoCharDb = true;
            Debug.LogError("[SynergyDatabase] Character Database 가 비어 있어 등급 시너지가 활성화되지 않습니다. 인스펙터에서 할당하세요.");
            return;
        }
    }
}
