/// <summary>
/// "이 캐릭터를 지금 어떤 그림으로 그릴 것인가"를 정하는 단일 관문. 표시하는 화면들은 코스튬 저장소나
/// 코스튬 데이터베이스를 직접 알 필요 없이 여기서 받은 CharacterAppearance 를 슬롯에 넘기기만 하면 된다.
///
/// 두 갈래를 한 곳에 모은다:
///   - Mine  : 내 캐릭터. 장착 코스튬은 내 저장소(PlayerStateStore)가 안다.
///   - Other : 남의 캐릭터. 장착 코스튬은 서버가 패킷에 실어 준 id 로 온다(내 저장소에는 없다).
/// 두 경로가 같은 조립기를 통과하므로, 내 화면에서 보는 내 외형과 남이 보는 내 외형이 어긋날 수 없다.
///
/// 코스튬이 없거나(캐릭터에 코스튬이 아직 없음) 미보유/미장착이면 자연스럽게 기본 일러스트가 나온다.
/// </summary>
public sealed class CharacterAppearanceService {
    private readonly CharacterDatabase _charDb;
    private readonly CostumeDatabase _costumeDb;
    private readonly PlayerStateStore _store;

    public CharacterAppearanceService(CharacterDatabase charDb, CostumeDatabase costumeDb, PlayerStateStore store) {
        _charDb = charDb;
        _costumeDb = costumeDb;
        _store = store;
    }

    /// <summary>내 캐릭터의 현재 외형(내가 장착한 코스튬 반영).</summary>
    public CharacterAppearance Mine(int charDataId) =>
        Other(charDataId, _store != null ? _store.EquippedCostumeId(charDataId) : 0,
              _store != null ? _store.EquippedUnitSkinId(charDataId) : 0);

    /// <summary>내 캐릭터의 현재 외형(원본을 이미 들고 있는 경우). 장착 아웃핏(전투 유닛 프리팹)도 함께 담는다.</summary>
    public CharacterAppearance Mine(CharacterData character) =>
        Other(character,
              character != null && _store != null ? _store.EquippedCostumeId(character.Id) : 0,
              character != null && _store != null ? _store.EquippedUnitSkinId(character.Id) : 0);

    /// <summary>다른 유저 캐릭터의 외형. costumeId/unitSkinId 는 서버가 내려준 값이며 0 이면 기본이다.</summary>
    public CharacterAppearance Other(int charDataId, int costumeId, int unitSkinId = 0) =>
        Other(_charDb != null ? _charDb.Get(charDataId) : null, costumeId, unitSkinId);

    /// <summary>다른 유저 캐릭터의 외형(원본을 이미 들고 있는 경우).</summary>
    public CharacterAppearance Other(CharacterData character, int costumeId, int unitSkinId = 0) =>
        new(character,
            costumeId != 0 && _costumeDb != null ? _costumeDb.Get(costumeId) : null,
            unitSkinId != 0 && _costumeDb != null && _costumeDb.UnitSkins != null ? _costumeDb.UnitSkins.Get(unitSkinId) : null);
}
