/// <summary>
/// 코스튬 id 규약(클라 측). 서버 Server.Data.CostumeCatalog 와 반드시 같은 규약이어야 한다.
///
/// 코스튬 id = 캐릭터 id * 100 + seq(1~99)
///   예) 캐릭터 1001 의 첫 코스튬 = 100101, 두 번째 = 100102
///
/// 대응표 대신 id 에서 주인 캐릭터를 역산하는 방식이라, 이벤트/업데이트로 코스튬이 늘어도
/// 서버 코드는 손댈 것이 없다(클라에서 CostumeData 를 만들어 CostumeDatabase 에 등록하기만 하면 된다).
/// </summary>
public static class CostumeRules {
    /// <summary>캐릭터 1명이 가질 수 있는 코스튬 수의 상한(id 규약상 seq 자릿수).</summary>
    public const int SeqPerCharacter = 100;

    /// <summary>캐릭터의 seq 번째 코스튬 id(seq 는 1부터).</summary>
    public static int CostumeId(int charDataId, int seq) => charDataId * SeqPerCharacter + seq;

    /// <summary>코스튬 id 의 주인 캐릭터 id. 규약에 어긋나면 0.</summary>
    public static int CharacterOf(int costumeId) {
        if (costumeId <= 0) return 0;
        int seq = costumeId % SeqPerCharacter;
        return seq == 0 ? 0 : costumeId / SeqPerCharacter;   // seq 는 1부터라 0 이면 규약 위반이다.
    }

    /// <summary>이 코스튬이 해당 캐릭터의 것인지.</summary>
    public static bool BelongsTo(int costumeId, int charDataId) =>
        charDataId != 0 && CharacterOf(costumeId) == charDataId;
}
