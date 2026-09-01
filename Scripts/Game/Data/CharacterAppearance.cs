using UnityEngine;

/// <summary>
/// 캐릭터 1명이 "지금 어떻게 보이는가". 기본 일러스트(CharacterData)와 장착 코스튬(CostumeData)을 한 값으로 묶어,
/// 표시하는 쪽이 코스튬의 존재를 몰라도 되게 만든다.
///
/// 왜 필요한가: 일러스트를 그리는 자리는 덱/도감/전투/프로필/길드/친구/랭킹/아레나까지 수십 곳이다.
/// 그 전부가 "코스튬이 있으면 코스튬 그림, 없으면 기본 그림" 판정을 각자 하면 한 곳만 빠져도 그 화면에서만
/// 옛 외형이 남는다. 판정을 이 한 값에 모아 두면 표시측은 지금까지처럼 IIllustrationSource 하나만 알면 된다.
///
/// 내 캐릭터든 남의 캐릭터든 만드는 방법만 다르고(내 것은 저장소, 남의 것은 패킷의 코스튬 id) 이후는 완전히 같다.
/// 조립은 CharacterAppearanceService 가 담당한다.
///
/// 코스튬이 자기 관심점(UiFocus)을 갖고 있으므로 초상 크롭도 코스튬별로 정확히 맞는다.
/// </summary>
public readonly struct CharacterAppearance : IIllustrationSource {
    /// <summary>원본 캐릭터. 코스튬 유무와 무관하게 이름/등급/스킬 등 정체성은 여기서 온다.</summary>
    public readonly CharacterData Character;

    /// <summary>장착 코스튬. null 이면 기본 일러스트다.</summary>
    public readonly CostumeData Costume;

    public CharacterAppearance(CharacterData character, CostumeData costume = null) {
        Character = character;
        // 다른 캐릭터의 코스튬이 잘못 실려 오면(데이터 오류/변조) 조용히 기본 외형으로 되돌린다.
        Costume = costume != null && character != null && costume.CharacterId == character.Id ? costume : null;
    }

    /// <summary>코스튬을 끼고 있는지(표시 강조/장착 버튼 상태 등에 쓴다).</summary>
    public bool HasCostume => Costume != null;

    /// <summary>지금 장착 중인 코스튬 id(기본 외형이면 0).</summary>
    public int CostumeId => Costume != null ? Costume.Id : 0;

    // ── IIllustrationSource(UI 초상/일러스트 슬롯) ──
    public Sprite Illustration => Costume != null ? Costume.Illustration
                                                  : (Character != null ? Character.Illustration : null);

    public Vector2 UiFocus => Costume != null ? Costume.UiFocus
                                              : (Character != null ? Character.UiFocus : new Vector2(0.5f, 0.5f));

    public IllustrationFit Fit => IllustrationFit.Crop;

    // ── 전투 유닛 표현 ──
    /// <summary>전투 유닛에 표시할 스프라이트(전투 전용 그림이 있으면 그것을 쓴다).</summary>
    public Sprite BattleSprite => Costume != null ? Costume.BattleSprite
                                                  : (Character != null ? Character.BattleSprite : null);

    /// <summary>기본 외형(코스튬 없음). 코스튬을 모르는 자리에서 캐릭터만으로 만들 때 쓴다.</summary>
    public static CharacterAppearance Default(CharacterData character) => new(character);
}
