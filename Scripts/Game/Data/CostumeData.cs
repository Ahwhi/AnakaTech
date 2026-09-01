using System;
using UnityEngine;

/// <summary>
/// 코스튬 1종의 정적 데이터. 코스튬은 "캐릭터의 일러스트를 통째로 갈아끼우는 외형 아이템"이며,
/// 장착하면 정해진 스탯 보정도 함께 받는다.
///
/// 역할 분담(각인과 같은 철학):
///   - 서버 : 어떤 id 가 존재하고 누구의 것인지(CostumeCatalog) + 누가 보유/장착했는지. 밸런스 수치는 모른다.
///   - 여기 : 일러스트/이름/효과 수치. 아트와 밸런스는 전부 이 에셋이 권위다.
///
/// 일러스트 프레이밍은 캐릭터와 완전히 같은 규약을 쓴다(IIllustrationSource). 코스튬마다 그림 구도가
/// 다르므로 초상 관심점(UiFocus)도 코스튬마다 따로 잡을 수 있어야 하며, 그래서 CharacterData 와 같은
/// 필드를 코스튬도 자기 몫으로 갖는다. 미설정으로 두면 표시측이 원본 캐릭터 값으로 폴백한다(CharacterAppearance).
///
/// 캐릭터마다 코스튬이 없을 수도, 여럿일 수도 있다. 새 코스튬은 이 에셋을 만들어 CostumeDatabase 에 넣기만 하면
/// 상점/우편/이벤트 어디서든 지급 가능해진다(지급 경로는 코스튬 id 만 알면 된다).
/// </summary>
[CreateAssetMenu(fileName = "CostumeData", menuName = "Anaka/Data/Costume")]
public sealed class CostumeData : ScriptableObject, IIllustrationSource {
    /// <summary>코스튬 장착 효과 1줄(스탯 1종 + 값). 사거리 +1 / 공격력 +15 처럼 모든 스탯을 건드릴 수 있다.</summary>
    [Serializable]
    public struct Effect {
        [Tooltip("올릴 스탯. 단위는 CombatStats 주석의 규약을 따른다(예: 공격속도는 %p).")]
        public StatType Stat;
        [Tooltip("더해질 값. 성장/장비/각인과 같은 축으로 합산된다.")]
        public float Value;
    }

    [Header("Identity")]
    [Tooltip("서버와 공유하는 고정 id. 반드시 CostumeRules 규약(캐릭터 id * 100 + 1~99)을 따라야 한다. 절대 재사용/변경 금지.")]
    [SerializeField] private int id;
    [Tooltip("주인 캐릭터. 코스튬은 이 캐릭터에게만 장착할 수 있다. id 와 짝이 맞지 않으면 에디터가 경고한다.")]
    [SerializeField] private CharacterData character;
    [SerializeField] private string displayName;
    [Tooltip("코스튬 등급(획득처 표시/정렬용). 스탯에는 관여하지 않는다.")]
    [SerializeField] private Grade grade = Grade.N;

    [Header("Description")]
    [Tooltip("코스튬 소개 문구(코스튬 목록/상세에 노출). 비우면 표시측이 해당 줄을 숨긴다.")]
    [TextArea(2, 6)]
    [SerializeField] private string description;

    [Header("Visuals")]
    [Tooltip("이 코스튬의 일러스트. 장착하면 캐릭터의 기본 일러스트를 대체한다(전 화면 공통).")]
    [SerializeField] private Sprite illustration;
    [Tooltip("전투 유닛 Illust 오버라이드 스프라이트. 비우면 위의 Illustration 을 그대로 사용한다.")]
    [SerializeField] private Sprite battleBodySprite;

    [Header("UI Illustration Focus")]
    [Tooltip("일러스트 내 관심점(보통 얼굴 중심)의 정규화 좌표. (0,0)=좌하단, (1,1)=우상단. " +
             "기본 일러스트와 구도가 다르므로 코스튬마다 따로 맞춘다(UI Focus 편집 도구로 조정).")]
    [SerializeField] private Vector2 uiFocus = new Vector2(0.5f, 0.75f);

    [Header("Equip Effect")]
    [Tooltip("장착 중에만 적용되는 스탯 보정. 비워 두면 외형만 바꾸는 코스튬이다.")]
    [SerializeField] private Effect[] effects = Array.Empty<Effect>();

    public int Id => id;
    /// <summary>주인 캐릭터 id. 참조가 비어 있으면 id 규약에서 역산한다(데이터 이관 중에도 안전).</summary>
    public int CharacterId => character != null ? character.Id : CostumeRules.CharacterOf(id);
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public Grade Grade => grade;
    /// <summary>코스튬 소개 문구. 미설정이면 빈 문자열(표시측이 해당 영역을 숨긴다).</summary>
    public string Description => description ?? "";
    public Sprite Illustration => illustration;
    public Vector2 UiFocus => uiFocus;
    /// <summary>코스튬도 캐릭터와 같은 전신 일러스트라 항상 크롭 프레이밍이다.</summary>
    public IllustrationFit Fit => IllustrationFit.Crop;
    public Sprite BattleBodySprite => battleBodySprite;
    /// <summary>전투 유닛에 실제로 표시할 스프라이트. 전투 전용 그림이 있으면 그것을, 없으면 일러스트를 쓴다.</summary>
    public Sprite BattleSprite => battleBodySprite != null ? battleBodySprite : illustration;
    public Effect[] Effects => effects;

    /// <summary>
    /// 장착 효과의 합계. 성장 밖 보정(장비/각인/본부/수련)과 같은 축이라 CharacterBuild 가 그대로 더한다.
    /// </summary>
    public CombatStats BonusStats {
        get {
            var sum = new CombatStats();
            if (effects == null) return sum;
            foreach (var e in effects) sum = sum.WithAdded(e.Stat, e.Value);
            return sum;
        }
    }

    /// <summary>효과 요약(스탯 + 값). 코스튬 카드/상세가 공통으로 쓴다. 효과가 없으면 빈 문자열.</summary>
    public string EffectSummary() {
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

#if UNITY_EDITOR
    // id 규약이 깨지면 서버가 주인 캐릭터를 역산하지 못해 장착이 거부된다. 에셋을 만들 때 바로 알 수 있게 경고한다.
    private void OnValidate() {
        if (character == null || id == 0) return;
        if (CostumeRules.BelongsTo(id, character.Id)) return;
        Debug.LogWarning($"[CostumeData] {name}: id({id}) 가 주인 캐릭터({character.Id}) 규약에 맞지 않습니다. " +
                         $"{CostumeRules.CostumeId(character.Id, 1)} ~ {CostumeRules.CostumeId(character.Id, 99)} 범위로 정하세요.", this);
    }
#endif
}
