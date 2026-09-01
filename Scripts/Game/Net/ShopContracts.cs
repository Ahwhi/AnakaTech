using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 상점 통신 계약과 순수 DTO(생성 패킷 비의존). 상점 UI 는 이 타입들만 참조한다.

public sealed class ShopOfferDto {
    public string OfferId;
    public string DisplayName;
    public string Description;
    public string NameColorHex;
    public string Major;
    public string Sub;
    public string ImageKey;
    public string Currency;   // gold / diamond / cash / (가챠면 티켓키)
    public int Price;

    // 가챠(티켓 우선 결제 + 부족분은 해당 소환권 판매 재화).
    public bool IsGacha;
    public bool GachaIsEquip;
    public string TicketKey;
    // 이 소환권을 파는 상품의 OfferId(서버 권위). 소환권이 모자랄 때 열 구매 팝업을 이 값으로 찾는다.
    // 지급 구성으로 역추적하면 같은 재화를 끼워주는 패키지가 잡히므로 반드시 이 값을 쓴다.
    public string TicketOfferId;
    public int PullPrice;     // 티켓 부족분 1회당 가격(소환권 판매 오퍼의 재화 단위)
    public int GachaCount;
    public string PoolKey;
    // 로테이션 픽업이 걸린 뽑기인지(서버 권위). 픽업 배경/기간과 천장 문구를 띄울지 판단하는 단일 근거라
    // 클라가 "특수" 같은 문자열로 라인을 구분하지 않는다.
    public bool HasPickup;

    // 한정/구매제한/스텝업.
    public int LimitType;   // ShopLimitRules.LimitType 값(0=None,1=Daily,2=Weekly,3=Monthly,4=Account,5=Subscription)
    public int LimitCount;  // 제한 단위(창/구독) 당 최대 구매 가능 횟수. 잔여 표시의 분모.
    public bool SoldOut;    // 구매완료(한도 도달)
    public bool Locked;     // 판매기간 아님/선행 미구매
    public int Remaining;   // 한정 상품의 남은 구매 가능 횟수(창 기준). 비한정은 -1.
    public bool HasRates;   // 확률 표기 가능 상품(가챠/랜덤 등급 상품) -> 확률 보기 버튼 노출

    // 수량 선택.
    public bool AllowQty;
    public int MaxQty;

    // 판매 기간(UTC epoch seconds, 0=상시).
    public long SaleStartUnix;
    public long SaleEndUnix;

    // 표시 전용. 서버 카탈로그에서 지정한 추천 상품이면 카드에 HOT 태그를 노출한다.
    public bool Hot;

    /// <summary>
    /// 한정 판매(플래시 세일) 상품인지. 발동 조건이 충족된 계정에만 내려오므로 true 면 지금 열려 있다는 뜻이다.
    /// 상점이 구성품을 보여 주는 패키지 카드를 고르는 근거이며, 판매 기간(SaleEndUnix)도 계정별 값으로 채워져 온다.
    /// </summary>
    public bool Flash;

    // 구매 시 지급되는 구성품(표시 전용, 서버 카탈로그 권위). 가챠 오퍼는 비어 있다.
    public List<RewardItem> Grants = new();
}

public sealed class ShopCatalogDto {
    public int Gold;
    public int Diamond;
    // 범용 재화(티켓 등) 잔량(키 -> 수량). gold/diamond 는 위 전용 필드로 별도 제공.
    public Dictionary<string, int> Currencies = new();
    // 대분류 버튼 표시명(Major 키 -> 이름). 클라 ShopPopup 이 버튼 라벨을 이 값으로 설정한다.
    public Dictionary<string, string> CategoryNames = new();
    // 대분류별 "전체" 서브탭 노출 여부(Major 키 -> bool).
    public Dictionary<string, bool> CategoryHasAllTab = new();
    public List<ShopOfferDto> Offers = new();

    // 픽업 로테이션(특수 뽑기) 현황. 캐릭터 트랙과 무기 트랙이 서로 독립이라 기간도 따로 온다.
    public int RotationCharId;
    public long RotationStartUnix;
    public long RotationEndUnix;
    public int RotationEquipId;
    public long RotationEquipStartUnix;
    public long RotationEquipEndUnix;

    // 가챠 천장 카운터(풀키 -> 현재 카운트). 클라 가챠 팝업의 "N회 내 확정" 표시용.
    public Dictionary<string, int> Pities = new();

    // 무료 뽑기 현황(키 -> 다음 가능 시각 UTC epoch seconds). 0/과거면 지금 가능. 키: normal_char / normal_equip.
    public Dictionary<string, long> FreeGachaNextUnix = new();
}

public sealed class GachaRollDto {
    public int CharDataId;
    public bool IsNew;
    public int ShardsGained;
    public int MaxExp;   // 신규 획득(레벨 1) 시 필요 경험치(서버 확정). 중복 획득이면 0.
}

// 장비 구매/장비 가챠로 새로 지급된 장비 인스턴스(가방=미장착). 즉시 인벤토리 반영 + 연출에 사용.
// 능력치는 개체별 랜덤이라 굴린 포인트를 함께 내려받아 가챠 연출에서 바로 보여준다.
public sealed class EquipRollDto {
    public long InstanceId;
    public int EquipDataId;
    public int StarLevel = 1;
    public int Level = 1;
    public int MainPoints;
    public readonly int[] OptionPoints = new int[EquipmentStatLayout.MaxOptionSlots];
}

public sealed class BuyResultDto {
    public bool IsSuccess;
    public int FailReason;    // 0=성공,1=오퍼없음,2=재화부족,3=중복,4=인앱미지원,5=지갑없음,6=한도초과,7=선행단계미구매,8=판매기간아님,9=수량오류
    public int NewGold;
    public int NewDiamond;
    public int NewStamina;
    public List<GachaRollDto> Rolls = new();
    public List<EquipRollDto> EquipRolls = new();

    /// <summary>
    /// 실제로 지급된 항목(서버 권위). 카탈로그의 예고 구성품(ShopOfferDto.Grants)과 달리 랜덤이 확정된 뒤의 값이라
    /// "랜덤 만능조각"이 아니라 실제로 받은 "SSR 만능조각 1개"가 담긴다. 수량 선택 배수도 이미 반영되어 있다.
    /// 구매 완료 표시는 이 목록을 우선 쓰고, 비어 있을 때만 예고 구성품으로 대신한다.
    /// </summary>
    public List<RewardItem> Grants = new();
}

// 가챠 확률 표시 항목(캐릭터/장비 id + 등급 + 실제 공급 확률).
public sealed class GachaRateEntryDto {
    public int Id;
    public bool IsEquip;
    public int Grade;       // 0=N..4=XR
    // 이 항목 1개가 나올 확률(0~1). 등급 확률을 후보 개수로 나눈 값이라 1/13 같은 무한소수가 흔하므로
    // 정수 분율이 아니라 실수로 받는다(표시 반올림 자리는 GachaRatesPopup 이 정한다).
    public double Rate;
}

/// <summary>로테이션 픽업 1건(현재 또는 예정). Order 0=진행 중, 1=다음.</summary>
public sealed class RotationEntryDto {
    public bool IsEquip;
    public int Order;
    public int Id;
    public int Grade;       // 0=N..4=XR
    public long StartUnix;
    public long EndUnix;

    public bool IsCurrent => Order == 0;
}

/// <summary>캐릭터/무기 트랙의 로테이션 일정. 트랙별로 Order 순서대로 담긴다.</summary>
public sealed class RotationInfoDto {
    public List<RotationEntryDto> Entries = new();

    /// <summary>지정 트랙의 항목만 Order 순으로. (캐릭터 탭/무기 탭을 나눠 그릴 때)</summary>
    public List<RotationEntryDto> Track(bool isEquip) {
        var list = new List<RotationEntryDto>();
        foreach (var e in Entries) if (e.IsEquip == isEquip) list.Add(e);
        list.Sort((a, b) => a.Order - b.Order);
        return list;
    }
}

public sealed class GachaRatesDto {
    public string OfferId;
    public List<GachaRateEntryDto> Entries = new();

    /// <summary>등급별 합산 확률(0~1). 항목 확률을 등급으로 모은 값이라 항목 표기와 항상 일치한다.</summary>
    public Dictionary<int, double> RateByGrade() {
        var map = new Dictionary<int, double>();
        foreach (var e in Entries) {
            map.TryGetValue(e.Grade, out double acc);
            map[e.Grade] = acc + e.Rate;
        }
        return map;
    }
}

/// <summary>상점 통신 추상화(AttendanceService 와 동일한 WaitPacketAsync + Send 패턴).</summary>
public interface IShopService {
    UniTask<ShopCatalogDto> RequestShopAsync(CancellationToken ct = default);
    /// <summary>
    /// 구매 요청. 재화로 사는 상품은 store/receipt 를 비워 두고, 현금 상품은 스토어 결제를 끝낸 뒤
    /// 그 영수증을 실어 보낸다(서버가 스토어에 되물어 확인한 뒤에만 지급한다).
    /// </summary>
    UniTask<BuyResultDto> BuyOfferAsync(string offerId, string idempotencyKey, int qty = 1, bool free = false,
                                        IapStoreKind store = IapStoreKind.None, string receipt = null,
                                        CancellationToken ct = default);

    /// <summary>
    /// 스토어 결제 창을 열기 전에 "지금 살 수 있는지" 를 서버에 묻는다. 0 이면 가능하고, 그 외에는 실패 사유다.
    ///
    /// 인앱결제는 스토어가 먼저 돈을 받고 서버가 나중에 지급한다. 결제 한도 같은 우리 규칙을 지급 단계에서
    /// 판정하면 유저는 이미 돈을 낸 뒤라 되돌릴 수 없으므로, 결제 창을 열기 전에 먼저 확인한다.
    /// </summary>
    UniTask<int> CanBuyOfferAsync(string offerId, int qty, CancellationToken ct = default);
    UniTask<GachaRatesDto> RequestGachaRatesAsync(string offerId, CancellationToken ct = default);
    UniTask<RotationInfoDto> RequestRotationInfoAsync(CancellationToken ct = default);
}
