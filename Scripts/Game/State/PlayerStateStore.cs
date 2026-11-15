using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

/// <summary>
/// 클라 측 플레이어 상태의 단일 소스(서버 권위 값의 로컬 캐시). 재화/기력/던전 인덱스는 ReactiveProperty 로
/// 노출해 UI/전투가 구독한다. 캐릭터 성장/덱은 서버가 갱신할 때만 반영한다.
/// 이 저장소는 통신을 하지 않는다(순수 상태). 서버 호출은 IMainDataService 가 담당하고 결과를 여기 Apply 한다.
/// </summary>
public sealed class PlayerStateStore {
    // 각인/본부/코스튬 효과를 최종 스탯으로 해석하기 위한 정적 데이터
    // (전부 선택 주입: 미할당이면 그 보너스만 0 으로 안전 동작).
    private readonly EngravingDatabase _engravingDb;
    private readonly HqNodeDatabase _hqDb;
    private readonly CostumeDatabase _costumeDb;
    private readonly SynergyDatabase _synergyDb;

    public PlayerStateStore(EngravingDatabase engravingDb = null, HqNodeDatabase hqDb = null,
                            CostumeDatabase costumeDb = null, SynergyDatabase synergyDb = null) {
        _engravingDb = engravingDb;
        _hqDb = hqDb;
        _costumeDb = costumeDb;
        _synergyDb = synergyDb;
    }

    public ReactiveProperty<int> Gold { get; } = new(0);
    public ReactiveProperty<int> Diamond { get; } = new(0);
    public ReactiveProperty<int> Stamina { get; } = new(0);
    // 소환권(가챠용, 서버 권위). 일반 = 일반 캐릭/장비 뽑기 공용, 특수 = 특수 캐릭/장비 뽑기 공용.
    public ReactiveProperty<int> NormalTicket { get; } = new(0);
    public ReactiveProperty<int> SpecialTicket { get; } = new(0);
    // 의뢰 컨텐츠 재화. 의뢰해결권은 매일 자동 충전(서버 권위), 명성/각인석은 의뢰 보상으로만 나온다.
    public ReactiveProperty<int> CommissionTicket { get; } = new(0);
    public ReactiveProperty<int> Renown { get; } = new(0);
    public ReactiveProperty<int> EngravingStone { get; } = new(0);
    /// <summary>스테이지 소탕권. 진행 중인 던전을 장수만큼 즉시 클리어한다(상점/보상으로 수급).</summary>
    public ReactiveProperty<int> StageSweepTicket { get; } = new(0);
    /// <summary>기력 부스트권. 부스트 모드 클리어 1회에 1장 쓰이며 매일 자정 자동 충전된다(서버 권위).</summary>
    public ReactiveProperty<int> StaminaBoostTicket { get; } = new(0);
    /// <summary>길드 포인트. 길드 레이드/대항전 보상으로 쌓이며 길드 상점의 결제 재화다.</summary>
    public ReactiveProperty<int> GuildPoint { get; } = new(0);
    /// <summary>수련 완료권. 진행 중인 수련의 남은 시간을 즉시 당긴다(서버 권위).</summary>
    public ReactiveProperty<int> TrainingTicket { get; } = new(0);
    /// <summary>쟁탈전 코인. 쟁탈전 보상으로 쌓이며 쟁탈전 상점의 결제 재화다.</summary>
    public ReactiveProperty<int> ArenaCoin { get; } = new(0);
    /// <summary>파견 증명서. 파견 완료 보상으로만 쌓이며 코스튬 구매의 결제 재화다.</summary>
    public ReactiveProperty<int> DispatchCertificate { get; } = new(0);

    /// <summary>모집 마일리지. 모집을 돌리면 소녀/장비를 가리지 않고 쌓이며 SSR 소녀 픽업에 쓴다.</summary>
    public ReactiveProperty<int> GachaMileage { get; } = new(0);

    /// <summary>한계돌파권. 조건을 채운 캐릭터를 한계돌파할 때 쓴다(서버 범용 재화 지갑).</summary>
    public ReactiveProperty<int> LimitBreakTicket { get; } = new(0);
    public ReactiveProperty<int> CurrentDungeonIndex { get; } = new(0);

    /// <summary>클리어한 최고 던전 인덱스(0=클리어 없음, 서버 권위). 던전 해금 표시 기준(DungeonUnlockRule).
    /// 메인 진입 스냅샷에서 설정되고, 던전 클리어 보상마다 갱신된다(재접속 없이 다음 던전이 열린다).</summary>
    public ReactiveProperty<int> HighestClearedDungeonIndex { get; } = new(0);

    /// <summary>계정 닉네임(서버 권위). 메인 진입 스냅샷에서 설정, 닉네임 변경 시 갱신된다.</summary>
    public ReactiveProperty<string> Nickname { get; } = new("");

    /// <summary>장착 프로필 테두리 id(0=미장착). 메인 진입/테두리 장착 변경 시 갱신된다.</summary>
    public ReactiveProperty<int> EquippedProfileFrameId { get; } = new(0);

    public int StaminaMax { get; private set; }

    // 재화 종류 -> 값 매핑. 재화 UI(CurrencyView)가 타입만으로 값을 구독하도록 하는 단일 진입점.
    private Dictionary<CurrencyType, ReactiveProperty<int>> _currencyMap;

    /// <summary>재화 종류에 해당하는 값(ReactiveProperty)을 반환한다(없으면 null).</summary>
    public IReadOnlyReactiveProperty<int> Currency(CurrencyType type) {
        _currencyMap ??= new Dictionary<CurrencyType, ReactiveProperty<int>> {
            { CurrencyType.Gold, Gold },
            { CurrencyType.Diamond, Diamond },
            { CurrencyType.Stamina, Stamina },
            { CurrencyType.NormalTicket, NormalTicket },
            { CurrencyType.SpecialTicket, SpecialTicket },
            { CurrencyType.CommissionTicket, CommissionTicket },
            { CurrencyType.Renown, Renown },
            { CurrencyType.EngravingStone, EngravingStone },
            { CurrencyType.StageSweepTicket, StageSweepTicket },
            { CurrencyType.StaminaBoostTicket, StaminaBoostTicket },
            { CurrencyType.GuildPoint, GuildPoint },
            { CurrencyType.TrainingTicket, TrainingTicket },
            { CurrencyType.ArenaCoin, ArenaCoin },
            { CurrencyType.DispatchCertificate, DispatchCertificate },
            { CurrencyType.GachaMileage, GachaMileage },
            { CurrencyType.LimitBreakTicket, LimitBreakTicket },
        };
        return _currencyMap.TryGetValue(type, out var rp) ? rp : null;
    }

    /// <summary>
    /// 재화 값을 서버 확정값으로 덮어쓴다. 소모/지급 응답이 잔량을 내려줄 때 그 값을 그대로 반영하는 용도라
    /// 증감(+=)이 아니라 대입이다. 지갑 컬럼이 아닌 범용 재화도 같은 경로로 다룬다.
    /// </summary>
    public void SetCurrency(CurrencyType type, int value) {
        if (Currency(type) is ReactiveProperty<int> rp) rp.Value = value;
    }

    /// <summary>의뢰해결권 최대 보유량(서버가 내려준 값). 재화 UI 가 "n / max" 로 표시할 때 쓴다.</summary>
    public int CommissionTicketMax { get; private set; } = 1;

    /// <summary>기력 부스트권 일일 자동 충전량(서버 권위). 부스트 버튼이 "N / 이 값"으로 표시한다.
    /// 상점 구매분은 이 값을 넘겨 보유할 수 있으므로 보유량의 상한이 아니라 "매일 채워지는 기준선"이다.</summary>
    public int StaminaBoostTicketDaily { get; private set; } = 30;

    public readonly Dictionary<int, CharacterProgress> Characters = new();
    public DeckState Deck { get; } = new();

    // ── 각인 / 본부 / 의뢰 진행(서버 권위 캐시) ─────────────
    /// <summary>보유 각인의 레벨(각인 id -> 레벨). 레벨은 계정 공용이라 캐릭터별로 갖지 않는다.</summary>
    public readonly Dictionary<int, int> EngravingLevels = new();

    /// <summary>
    /// 아직 갖지 못한 각인이 남았는지. 서버는 미보유 각인 중에서 도면을 뽑고, 다 모은 계정에는 각인석으로 대신 준다.
    /// 보상 미리보기가 이 값으로 실제 지급과 같은 품목을 보여 준다. 각인 데이터가 없으면 남은 것으로 본다(예고를 숨기지 않는다).
    /// </summary>
    public bool HasUnownedEngraving {
        get {
            if (_engravingDb == null) return true;
            foreach (var e in _engravingDb.All)
                if (e != null && !EngravingLevels.ContainsKey(e.Id)) return true;
            return false;
        }
    }

    /// <summary>캐릭터별 각인 장착(캐릭터 id -> 슬롯 배열, 값 0=빈 슬롯). 길이는 항상 EngravingRules.SlotCount.</summary>
    public readonly Dictionary<int, int[]> EquippedEngravings = new();

    /// <summary>본부 노드 레벨(노드 id -> 레벨). 레벨 0 노드는 담지 않는다.</summary>
    public readonly Dictionary<int, int> HqNodeLevels = new();

    /// <summary>의뢰 종류별 클리어한 최고 단계(종류 인덱스 -> 단계, 0=없음).</summary>
    public readonly Dictionary<int, int> CommissionClearedStage = new();

    /// <summary>각인 보유/장착이 바뀌었을 때 알림(각인 UI 갱신용).</summary>
    public event Action EngravingsChanged;
    /// <summary>본부 노드가 바뀌었을 때 알림(트리 UI 갱신용).</summary>
    public event Action HeadquartersChanged;
    /// <summary>의뢰 진행/입장권이 바뀌었을 때 알림(의뢰 팝업 갱신용).</summary>
    public event Action CommissionChanged;

    // 계정 전체 스탯 보너스(본부)는 노드가 바뀔 때만 달라지므로 캐시한다. 파티 5명 x 매 갱신마다 트리를
    // 전부 훑지 않도록 하기 위한 것이며, 무효화 지점은 ApplyHeadquarters 한 곳뿐이다.
    private CombatStats _accountBonusCache;
    private bool _accountBonusValid;

    /// <summary>본부 노드가 주는 계정 전체(모든 캐릭터 공통) 스탯 보너스.</summary>
    public CombatStats AccountBonus {
        get {
            if (!_accountBonusValid) {
                _accountBonusCache = _hqDb != null ? _hqDb.TotalBonus(HqNodeLevels) : new CombatStats();
                _accountBonusValid = true;
            }
            return _accountBonusCache;
        }
    }

    /// <summary>
    /// 캐릭터 1명이 성장(레벨/별/스탯 포인트) 밖에서 받는 모든 보정의 합:
    /// 장비 + 각인(항상 적용분) + 본부 + 계정 투자 + 수련 + 코스튬 + 시너지.
    /// 전투와 표시(전투력/상세)가 반드시 같은 값을 쓰도록 하는 단일 관문이므로, 새 보정원이 생기면 여기만 더한다.
    /// </summary>
    /// <param name="deckOverride">시너지 판정에 쓸 편성(편성 화면의 편집 중 작업본). null 이면 확정된 편성을 쓴다.</param>
    public CombatStats TotalBonusFor(int charDataId, EquipmentDatabase equipDb,
                                     IReadOnlyList<int> deckOverride = null) =>
        EquipmentBonusFor(charDataId, equipDb) + EngravingBonusFor(charDataId) + AccountBonus + InvestmentBonus
        + TrainingBonusFor(charDataId) + CostumeBonusFor(charDataId) + UnitSkinBonusFor(charDataId)
        + SynergyBonusFor(charDataId, deckOverride);

    /// <summary>
    /// 현재 편성에서 지정 캐릭터가 받는 시너지 보정. 편성에 없는 캐릭터는 자연히 0 이다.
    ///
    /// 기준이 되는 덱은 기본적으로 "서버에 확정된 편성"이다. 전투와 HUD 는 확정본을 봐야 화면과 서버가 어긋나지 않는다.
    /// 다만 편성 화면은 아직 커밋하지 않은 작업본으로 "이렇게 짜면 얼마가 되는지"를 보여 줘야 하므로,
    /// 그때만 deckOverride 로 편집 중인 편성을 넘긴다.
    /// </summary>
    public CombatStats SynergyBonusFor(int charDataId, IReadOnlyList<int> deckOverride = null) =>
        // 조립 규칙은 CharacterBuild 한 곳에만 둔다(대전의 상대 파티도 같은 함수를 쓴다).
        CharacterBuild.SynergyBonus(charDataId, deckOverride ?? Deck.SelectedCharIds, _synergyDb);

    /// <summary>지정 캐릭터의 각인 중 "항상 적용" 효과만 합산한 보정(전투력/상세 표시에 반영되는 부분).</summary>
    public CombatStats EngravingBonusFor(int charDataId) => EngravingLoadoutFor(charDataId).Always;

    /// <summary>
    /// 지정 캐릭터의 각인 로드아웃(항상 적용분 + 조건부 목록). 전투 유닛은 이 객체를 받아 조건부 효과를 스스로 켜고 끈다.
    /// 각인 데이터베이스가 없거나 장착이 없으면 공용 빈 인스턴스를 돌려준다(할당 없음).
    /// </summary>
    public EngravingLoadout EngravingLoadoutFor(int charDataId) {
        if (!EquippedEngravings.TryGetValue(charDataId, out var slots) || slots == null) return EngravingLoadout.Empty;
        // 조립 규칙은 CharacterBuild 한 곳에만 둔다(대전의 상대 파티도 같은 함수를 쓴다).
        return CharacterBuild.Engravings(slots, GetProgress(charDataId)?.StarLevel ?? 0, EngravingLevels, _engravingDb);
    }

    /// <summary>
    /// 표시용 캐릭터 런타임(최종 스탯 + 전투력)의 단일 관문. 보정 합계와 조건부 각인 효과를 함께 넘겨
    /// 화면마다 전투력이 다르게 나오지 않도록 한다. 표시 화면은 CharacterRuntime 을 직접 만들지 말고 이것을 쓴다.
    /// </summary>
    /// <param name="deckOverride">시너지 판정에 쓸 편성(TotalBonusFor 와 같은 의미).</param>
    public CharacterRuntime RuntimeFor(CharacterData data, CharacterProgress progress, BattleSettings settings,
                                       EquipmentDatabase equipDb, IReadOnlyList<int> deckOverride = null) =>
        new CharacterRuntime(data, progress, settings, TotalBonusFor(data.Id, equipDb, deckOverride),
                             EngravingLoadoutFor(data.Id).Conditionals);

    // ── 코스튬(외형 교체 + 장착 효과) ─────────────────────────────
    /// <summary>보유 코스튬 id 집합(계정 단위, 서버 권위). 어떤 경로로 받았든 여기 쌓인다.</summary>
    public readonly HashSet<int> OwnedCostumes = new();

    /// <summary>캐릭터별 장착 코스튬(캐릭터 id -> 코스튬 id). 항목이 없으면 기본 일러스트다.</summary>
    public readonly Dictionary<int, int> EquippedCostumes = new();

    /// <summary>코스튬 보유/장착이 바뀌었을 때 알림(외형과 스탯이 함께 바뀌므로 표시부가 구독한다).</summary>
    public event Action CostumesChanged;

    /// <summary>지정 캐릭터가 장착 중인 코스튬 id(0 = 기본 일러스트).</summary>
    public int EquippedCostumeId(int charDataId) =>
        EquippedCostumes.TryGetValue(charDataId, out int id) ? id : 0;

    /// <summary>이 코스튬을 보유 중인지(장착 가능 판정. 최종 판정은 서버가 한다).</summary>
    public bool OwnsCostume(int costumeId) => costumeId != 0 && OwnedCostumes.Contains(costumeId);

    /// <summary>지정 캐릭터의 장착 코스튬이 주는 스탯 보정(미장착이면 0).</summary>
    public CombatStats CostumeBonusFor(int charDataId) =>
        // 조립 규칙은 CharacterBuild 한 곳에만 둔다(대전의 상대 파티도 같은 함수를 쓴다).
        CharacterBuild.CostumeBonus(EquippedCostumeId(charDataId), charDataId, _costumeDb);

    /// <summary>서버 스냅샷(보유 + 장착)을 통째로 반영한다. 아웃핏도 같은 조회에 실려 오므로 함께 맞춘다.</summary>
    public void ApplyCostumes(CostumeInfoDto dto) {
        OwnedCostumes.Clear();
        EquippedCostumes.Clear();
        OwnedUnitSkins.Clear();
        EquippedUnitSkins.Clear();
        if (dto != null) {
            foreach (int id in dto.Owned) OwnedCostumes.Add(id);
            foreach (var e in dto.Equipped) EquippedCostumes[e.CharDataId] = e.CostumeId;
            foreach (var kv in dto.OwnedUnitSkins) OwnedUnitSkins[kv.Key] = kv.Value;
            foreach (var kv in dto.EquippedUnitSkins) EquippedUnitSkins[kv.Key] = kv.Value;
        }
        CostumesChanged?.Invoke();
    }

    // ── 아웃핏(V1.12) ──────────────────────────────────
    /// <summary>아웃핏 보유 개수(아웃핏 id -> 개수, 서버 권위). 같은 아웃핏을 여러 개 가질 수 있다.</summary>
    public readonly Dictionary<int, int> OwnedUnitSkins = new();

    /// <summary>캐릭터별 장착 아웃핏(캐릭터 id -> 아웃핏 id). 항목이 없으면 기본 아웃핏이다.</summary>
    public readonly Dictionary<int, int> EquippedUnitSkins = new();

    /// <summary>지정 캐릭터가 장착 중인 아웃핏 id(0 = 기본 아웃핏).</summary>
    public int EquippedUnitSkinId(int charDataId) =>
        EquippedUnitSkins.TryGetValue(charDataId, out int id) ? id : 0;

    /// <summary>아웃핏 보유 개수.</summary>
    public int UnitSkinCount(int unitSkinId) => OwnedUnitSkins.TryGetValue(unitSkinId, out int n) ? n : 0;

    /// <summary>그 아웃핏을 끼고 있는 소녀 수.</summary>
    public int UnitSkinInUse(int unitSkinId) {
        int used = 0;
        foreach (var kv in EquippedUnitSkins) if (kv.Value == unitSkinId) used++;
        return used;
    }

    /// <summary>
    /// 이 소녀가 그 아웃핏을 지금 낄 수 있는지(남은 개수가 있거나 이미 끼고 있다). 최종 판정은 서버가 한다.
    /// </summary>
    public bool CanEquipUnitSkin(int charDataId, int unitSkinId) =>
        unitSkinId == 0 || EquippedUnitSkinId(charDataId) == unitSkinId
        || UnitSkinCount(unitSkinId) - UnitSkinInUse(unitSkinId) > 0;

    /// <summary>지정 캐릭터의 장착 아웃핏이 주는 스탯 보정(미장착이면 0).</summary>
    public CombatStats UnitSkinBonusFor(int charDataId) =>
        // 조립 규칙은 CharacterBuild 한 곳에만 둔다(대전의 상대 파티도 같은 함수를 쓴다).
        CharacterBuild.UnitSkinBonus(EquippedUnitSkinId(charDataId), _costumeDb);

    /// <summary>아웃핏 장착 결과 반영(unitSkinId=0 이면 기본 아웃핏으로 되돌린 것).</summary>
    public void ApplyUnitSkinEquip(int charDataId, int unitSkinId) {
        if (unitSkinId == 0) EquippedUnitSkins.Remove(charDataId);
        else EquippedUnitSkins[charDataId] = unitSkinId;
        CostumesChanged?.Invoke();
        // 아웃핏도 스탯을 바꾸므로 성장 변화와 같은 통로로 알린다(전투력/스탯 표시가 그대로 따라온다).
        RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { charDataId });
    }

    /// <summary>장착 결과 반영(costumeId=0 이면 기본 일러스트로 되돌린 것).</summary>
    public void ApplyCostumeEquip(int charDataId, int costumeId) {
        if (costumeId == 0) EquippedCostumes.Remove(charDataId);
        else EquippedCostumes[charDataId] = costumeId;
        CostumesChanged?.Invoke();
        // 코스튬은 스탯도 바꾸므로 성장 변화와 같은 통로로도 알린다(전투력/스탯 표시가 그대로 따라온다).
        RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { charDataId });
    }

    /// <summary>코스튬 획득 반영(상점/우편/이벤트 공용). 이미 보유 중이면 아무 일도 하지 않는다.</summary>
    public void AddCostume(int costumeId) {
        if (costumeId == 0 || !OwnedCostumes.Add(costumeId)) return;
        CostumesChanged?.Invoke();
    }

    /// <summary>지정 캐릭터의 각인 슬롯 배열(없으면 새로 만들어 등록). 길이는 항상 EngravingRules.SlotCount.</summary>
    public int[] EngravingSlotsOf(int charDataId) {
        if (!EquippedEngravings.TryGetValue(charDataId, out var slots) || slots == null || slots.Length != EngravingRules.SlotCount) {
            slots = new int[EngravingRules.SlotCount];
            EquippedEngravings[charDataId] = slots;
        }
        return slots;
    }

    /// <summary>각인 스냅샷 반영(보유 레벨 + 캐릭터별 장착). 서버 권위 값으로 전체를 교체한다.</summary>
    public void ApplyEngravings(EngravingInfoDto dto) {
        EngravingLevels.Clear();
        EquippedEngravings.Clear();
        if (dto != null) {
            EngravingStone.Value = dto.EngravingStone;
            foreach (var o in dto.Owned) EngravingLevels[o.EngravingId] = o.Level;
            foreach (var s in dto.Equipped) {
                if (!EngravingRules.IsValidSlot(s.SlotIndex)) continue;
                EngravingSlotsOf(s.CharDataId)[s.SlotIndex] = s.EngravingId;
            }
        }
        EngravingsChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);   // 각인은 능력치에 영향(전투/전투력 즉시 반영)
    }

    /// <summary>각인 1종의 레벨 갱신(강화 성공). 잔여 각인석도 서버 확정값으로 맞춘다.</summary>
    public void ApplyEngravingUpgrade(int engravingId, int level, int newStone) {
        EngravingLevels[engravingId] = level;
        EngravingStone.Value = newStone;
        EngravingsChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);
    }

    /// <summary>각인 장착/해제 반영(engravingId=0 이면 해제).</summary>
    public void ApplyEngravingEquip(int charDataId, int slotIndex, int engravingId) {
        if (!EngravingRules.IsValidSlot(slotIndex)) return;
        EngravingSlotsOf(charDataId)[slotIndex] = engravingId;
        EngravingsChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { charDataId });
    }

    /// <summary>본부 스냅샷 반영. 계정 보너스 캐시를 무효화해 다음 스탯 계산부터 새 값이 쓰인다.</summary>
    public void ApplyHeadquarters(HeadquartersDto dto) {
        HqNodeLevels.Clear();
        if (dto != null) {
            Renown.Value = dto.Renown;
            foreach (var n in dto.Nodes)
                if (n.Level > 0) HqNodeLevels[n.NodeId] = n.Level;
        }
        _accountBonusValid = false;
        HeadquartersChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);
    }

    /// <summary>본부 노드 1개의 레벨 갱신(해금/강화 성공).</summary>
    public void ApplyHqNodeUpgrade(int nodeId, int level, int newRenown) {
        if (level > 0) HqNodeLevels[nodeId] = level;
        else HqNodeLevels.Remove(nodeId);
        Renown.Value = newRenown;
        _accountBonusValid = false;
        HeadquartersChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);
    }

    /// <summary>의뢰 스냅샷 반영(입장권/명성/각인석/종류별 진행도).</summary>
    public void ApplyCommissionInfo(CommissionInfoDto dto) {
        CommissionClearedStage.Clear();
        if (dto != null) {
            // 상한을 반응 값보다 먼저 넣고 강제 통지한다(ApplyMainData 와 같은 이유).
            // 뒤에 두거나 값이 같아 발화하지 않으면 "n / 1" 로 그려진 화면이 그대로 남는다.
            CommissionTicketMax = Mathf.Max(1, dto.TicketMax);
            CommissionTicket.SetValueAndForceNotify(dto.Tickets);
            Renown.Value = dto.Renown;
            EngravingStone.Value = dto.EngravingStone;
            foreach (var s in dto.Stages) CommissionClearedStage[s.CommissionType] = s.ClearedStage;
        }
        CommissionChanged?.Invoke();
    }

    /// <summary>의뢰 종류의 클리어한 최고 단계(0=없음).</summary>
    public int ClearedStageOf(CommissionType type) =>
        CommissionClearedStage.TryGetValue((int)type, out int s) ? s : 0;

    /// <summary>의뢰 보상 수령 결과 반영(재화 잔액 + 진행도 + 편성원 경험치).</summary>
    public void ApplyCommissionReward(CommissionRewardDto dto) {
        if (dto == null) return;
        Gold.Value = dto.NewGold;
        Diamond.Value = dto.NewDiamond;
        Renown.Value = dto.NewRenown;
        EngravingStone.Value = dto.NewEngravingStone;
        if (dto.NewTickets >= 0) CommissionTicket.Value = dto.NewTickets;
        if (dto.ClearedStage > 0) CommissionClearedStage[dto.CommissionType] = dto.ClearedStage;

        ApplyGrantedRewards(dto.Granted);
        ApplyCharExp(dto.CharExp);
        CommissionChanged?.Invoke();
    }

    /// <summary>
    /// 메인 퀘스트 보상 수령 결과를 반영한다. 지갑 잔액과 캐릭터 경험치를 서버가 확정해 내려 주므로
    /// 화면이 다시 계산하지 않는다(경험치는 지갑이 아니라 편성원에게 들어가 레벨/스탯포인트까지 바뀐다).
    /// </summary>
    public void ApplyMainQuestReward(MainQuestClaimResultDto dto) {
        if (dto == null || !dto.IsSuccess) return;
        Gold.Value = dto.NewGold;
        Diamond.Value = dto.NewDiamond;

        ApplyGrantedRewards(dto.Granted);
        // 응답에는 골드/다이아 잔액만 실려 오므로 범용 재화(소탕권/의뢰해결권 등)는 지급 목록으로 더한다.
        // 빠뜨리면 그 재화를 근거로 켜지는 배지(소탕 등)가 팝업을 열어 잔량을 다시 받을 때까지 꺼져 있다.
        AddGrantedCurrencies(dto.Granted);
        ApplyCharExp(dto.CharExp);
    }

    /// <summary>
    /// 서버가 이미 지급한 보상 목록을 보유 상태에 즉시 반영한다(각인 도면 / 소모품 / 코스튬).
    /// 스냅샷을 다시 받지 않아도 방금 받은 것이 목록에 바로 나타나게 하는 용도이며,
    /// 의뢰 보상/상점 구매 등 "지급 목록을 돌려주는" 모든 경로가 이 하나를 쓴다.
    /// 지갑 잔액처럼 서버가 최신값을 따로 내려주는 항목은 여기서 건드리지 않는다.
    /// </summary>
    public void ApplyGrantedRewards(IReadOnlyList<RewardItem> granted) {
        if (granted == null) return;

        bool engravingChanged = false;
        bool itemChanged = false;
        bool costumeChanged = false;
        List<int> shardCharIds = null;   // 조각이 늘어난 소녀(없으면 통지도 하지 않는다)

        for (int i = 0; i < granted.Count; i++) {
            var r = granted[i];
            switch (r.Kind) {
                case RewardItemKind.RandomEngraving:
                    if (r.RefId == 0 || EngravingLevels.ContainsKey(r.RefId)) continue;
                    EngravingLevels[r.RefId] = 1;
                    engravingChanged = true;
                    break;

                case RewardItemKind.Item:
                    if (r.RefId == 0 || r.Amount < 1) continue;
                    Items[r.RefId] = ItemCount(r.RefId) + r.Amount;
                    itemChanged = true;
                    break;

                case RewardItemKind.Costume:
                    if (r.RefId == 0 || !OwnedCostumes.Add(r.RefId)) continue;
                    costumeChanged = true;
                    break;

                case RewardItemKind.UnitSkin:
                    if (r.RefId == 0 || r.Amount < 1) continue;
                    OwnedUnitSkins[r.RefId] = UnitSkinCount(r.RefId) + r.Amount;
                    costumeChanged = true;
                    break;

                case RewardItemKind.CharacterShard:
                    // 보유하지 않은 소녀의 조각은 담을 자리가 없다(보유 목록은 서버 스냅샷이 만든다).
                    // 그 경우는 다음 스냅샷에서 맞춰지므로 여기서 행을 만들지 않는다.
                    if (r.RefId == 0 || r.Amount < 1 || !Characters.TryGetValue(r.RefId, out var progress)) continue;
                    progress.Shards += r.Amount;
                    (shardCharIds ??= new List<int>()).Add(r.RefId);
                    break;
            }
        }

        if (engravingChanged) EngravingsChanged?.Invoke();
        if (itemChanged) ItemsChanged?.Invoke();
        if (costumeChanged) CostumesChanged?.Invoke();
        // 조각은 별레벨 승급의 재료라 성장 화면이 바로 따라와야 한다(승급 버튼이 켜지는 근거가 이 값이다).
        if (shardCharIds != null) RaiseCharactersChanged(CharacterChangeReason.Growth, shardCharIds);
    }

    /// <summary>
    /// 지급 목록의 범용 재화(티켓/명성/각인석/파견 증명서 등)를 보유량에 더한다.
    ///
    /// ApplyGrantedRewards 가 재화를 건드리지 않는 이유는 대부분의 경로가 지급과 함께 최신 잔액을 따로
    /// 내려주기 때문이다(그때 더하면 두 번 반영된다). 잔액을 돌려주지 않는 경로만 이 메서드를 함께 부른다.
    /// 지갑 컬럼(골드/다이아/기력)은 언제나 서버가 최신값을 주므로 여기서 제외한다.
    /// </summary>
    public void AddGrantedCurrencies(IReadOnlyList<RewardItem> granted) {
        if (granted == null) return;

        for (int i = 0; i < granted.Count; i++) {
            var r = granted[i];
            if (r.Kind != RewardItemKind.Currency || r.Amount < 1) continue;

            var type = CurrencyKeys.ToType(r.CurrencyKey);
            if (type == null) continue;
            if (type == CurrencyType.Gold || type == CurrencyType.Diamond || type == CurrencyType.Stamina) continue;

            var prop = Currency(type.Value);
            if (prop != null) SetCurrency(type.Value, prop.Value + r.Amount);
        }
    }

    // ── 수련(집중 수련) ───────────────────────────────────────
    /// <summary>
    /// 캐릭터별 수련 효과(캐릭터 id -> 효과). 한 캐릭터는 효과를 하나만 가지며, 수련을 다시 완료하면 대체된다.
    /// 서버 권위 값의 캐시이므로 스냅샷/작업 응답으로만 갱신한다.
    /// </summary>
    public readonly Dictionary<int, TrainingResultDto> TrainingResults = new();

    /// <summary>진행 중인 수련 슬롯(슬롯 번호 -> 진행 상태). 비어 있는 슬롯은 담지 않는다.</summary>
    public readonly Dictionary<int, TrainingSlotDto> TrainingSlots = new();

    /// <summary>해금된 수련 슬롯 수(서버 권위). 다음 슬롯 해금 버튼의 판정 근거다.</summary>
    public int TrainingUnlockedSlots { get; private set; } = 1;

    /// <summary>수련 슬롯/결과가 바뀌었을 때 알림(수련 팝업 갱신용).</summary>
    public event Action TrainingChanged;

    /// <summary>지정 캐릭터의 수련 효과가 주는 스탯 보정(없으면 0 보정).</summary>
    public CombatStats TrainingBonusFor(int charDataId) =>
        TrainingResults.TryGetValue(charDataId, out var r)
            ? TrainingRules.BonusOf(r.Effect, r.ValueTenth)
            : new CombatStats();

    /// <summary>지정 캐릭터의 수련 효과(없으면 false). 카드 표시/덮어쓰기 확인에 쓴다.</summary>
    public bool TryGetTraining(int charDataId, out TrainingResultDto result) =>
        TrainingResults.TryGetValue(charDataId, out result);

    /// <summary>지정 캐릭터가 지금 어느 슬롯에서 수련 중인지(아니면 false).</summary>
    public bool TryGetTrainingSlotOf(int charDataId, out TrainingSlotDto slot) {
        foreach (var kv in TrainingSlots) {
            if (kv.Value != null && kv.Value.CharDataId == charDataId) { slot = kv.Value; return true; }
        }
        slot = null;
        return false;
    }

    /// <summary>수련 스냅샷 반영(슬롯 현황 + 캐릭터별 효과). 서버 권위 값으로 전체를 교체한다.</summary>
    public void ApplyTrainingInfo(TrainingInfoDto dto) {
        TrainingResults.Clear();
        TrainingSlots.Clear();
        if (dto != null) {
            TrainingUnlockedSlots = Mathf.Max(1, dto.UnlockedSlots);
            TrainingTicket.SetValueAndForceNotify(dto.TrainingTicket);
            foreach (var r in dto.Results) TrainingResults[r.CharDataId] = r;
            foreach (var s in dto.Slots) if (s != null && s.CharDataId != 0) TrainingSlots[s.SlotIndex] = s;
        }
        TrainingChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);   // 수련은 능력치에 영향(전투/전투력 즉시 반영)
    }

    /// <summary>
    /// 수련 작업 결과를 반영한다. 작업 종류에 따라 바뀌는 것이 다르므로 한 함수에서 갈라 처리한다
    /// (스냅샷을 다시 받지 않아도 화면과 전투가 곧바로 맞는다).
    /// </summary>
    public void ApplyTrainingAction(TrainingActionResultDto result) {
        if (result == null || !result.IsSuccess) return;

        if (result.UnlockedSlots > 0) TrainingUnlockedSlots = result.UnlockedSlots;
        Diamond.Value = result.NewDiamond;
        TrainingTicket.Value = result.NewTrainingTicket;

        switch (result.Action) {
            case TrainingActionType.Start:
            case TrainingActionType.Instant:
                TrainingSlots[result.SlotIndex] = new TrainingSlotDto {
                    SlotIndex = result.SlotIndex, CharDataId = result.CharDataId,
                    StartUnix = result.StartUnix, EndUnix = result.EndUnix,
                };
                break;

            case TrainingActionType.Cancel:
                TrainingSlots.Remove(result.SlotIndex);
                break;

            case TrainingActionType.Claim:
                TrainingSlots.Remove(result.SlotIndex);
                if (result.Effect >= 0 && result.CharDataId != 0) {
                    TrainingResults[result.CharDataId] = new TrainingResultDto {
                        CharDataId = result.CharDataId, Effect = result.Effect, ValueTenth = result.ValueTenth,
                    };
                    // 수련 효과는 능력치라 진행 중인 전투와 전투력 표시에 즉시 반영돼야 한다
                    // (전투력 상승 연출도 이 통지를 근거로 뜬다).
                    RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { result.CharDataId });
                }
                break;
        }

        TrainingChanged?.Invoke();
    }

    /// <summary>진행 중인 수련 슬롯 목록(배지 판정 근거를 서비스에 넘길 때 사용).</summary>
    public List<TrainingSlotDto> TrainingSlotList() {
        var list = new List<TrainingSlotDto>(TrainingSlots.Count);
        foreach (var kv in TrainingSlots) if (kv.Value != null) list.Add(kv.Value);
        return list;
    }

    // ── 소모품 아이템 ─────────────────────────────────────────
    /// <summary>보유 소모품(아이템 id -> 개수). 0 이하는 담지 않는다.</summary>
    public readonly Dictionary<int, int> Items = new();

    /// <summary>보유 소모품이 바뀌었을 때 알림(목록 UI 갱신용).</summary>
    public event Action ItemsChanged;

    /// <summary>보유 개수(없으면 0).</summary>
    public int ItemCount(int itemId) => Items.TryGetValue(itemId, out int c) ? c : 0;

    /// <summary>소모품 스냅샷 반영. 서버 권위 값으로 전체를 교체한다.</summary>
    public void ApplyItems(ItemInfoDto dto) {
        Items.Clear();
        if (dto != null)
            foreach (var i in dto.Items)
                if (i.Count > 0) Items[i.ItemId] = i.Count;
        ItemsChanged?.Invoke();
    }

    /// <summary>소모품 사용 결과 반영(잔여 수량 + 대상 캐릭터 변화).</summary>
    public void ApplyItemUse(ItemUseResult result) {
        if (result == null || !result.IsSuccess) return;

        SetItemCount(result.ItemId, result.NewCount);
        // 사용 결과로 받은 아이템(경험치 추출의 캡슐 등). 서버가 받은 뒤 보유 개수를 주므로 그대로 맞춘다.
        foreach (var g in result.GrantedItems) SetItemCount(g.ItemId, g.Count);
        ItemsChanged?.Invoke();

        // 성장 초기화로 돌려받은 지갑 재화. 서버가 받은 뒤 보유량을 주므로 그대로 맞춘다(조각은 아래 캐릭터 스냅샷에 들어 있다).
        if (result.Refund.Diamond > 0) Diamond.Value = result.NewDiamond;
        if (result.Refund.LimitBreakTicket > 0) LimitBreakTicket.Value = result.NewLimitBreakTicket;

        // 경험치를 주는 아이템은 기존처럼 경험치 증분으로 가볍게 반영한다(전투 보상과 같은 갱신 경로).
        // 그 밖에 캐릭터를 바꾸는 아이템(경험치 추출처럼 레벨을 되돌리는 것)은 증분으로 맞출 수 없어 사용 후 스냅샷으로 교체한다.
        if (result.CharExp != null && result.CharExp.GainedExp > 0)
            ApplyCharExp(new[] { result.CharExp });
        else if (result.Updated != null)
            ApplyCharacterSnapshot(result.Updated);
    }

    private void SetItemCount(int itemId, int count) {
        if (count > 0) Items[itemId] = count;
        else Items.Remove(itemId);
    }

    /// <summary>보상으로 받은 소모품을 보유 목록에 즉시 더한다(스냅샷 재요청 없이 개수만 맞춘다).</summary>
    public void AddItem(int itemId, int count) {
        if (itemId == 0 || count < 1) return;
        Items[itemId] = ItemCount(itemId) + count;
        ItemsChanged?.Invoke();
    }

    /// <summary>의뢰 입장 시 소모된 입장권 반영.</summary>
    public void SetCommissionTickets(int tickets) {
        CommissionTicket.Value = tickets;
        CommissionChanged?.Invoke();
    }

    // 장비 인벤토리(instanceId -> 인스턴스) + 만능조각(등급 -> 수량).
    public readonly Dictionary<long, EquipmentInstanceDto> Equipment = new();
    public readonly Dictionary<int, int> UniversalShards = new();
    public event Action InventoryChanged;

    // ── 투자 ─────────────────────────────────────
    /// <summary>
    /// 투자 슬롯 레벨(슬롯 번호 -> 레벨). 경험치 증폭은 서버가 하지만, 전투 효과(선제 스킬 게이지/스쿼드 재생/스탯 보정)는
    /// 전투를 돌리는 클라가 적용해야 하므로 그 근거를 여기 둔다. 서버 응답으로만 채운다.
    /// </summary>
    public readonly Dictionary<int, int> InvestmentLevels = new();

    /// <summary>투자 레벨이 바뀌었을 때 알림(투자 화면 갱신용).</summary>
    public event Action InvestmentChanged;

    // 투자 스탯 보정은 레벨이 바뀔 때만 달라지므로 캐시한다(본부 AccountBonus 와 같은 이유).
    // 무효화 지점은 아래 Apply 두 곳뿐이다.
    private CombatStats _investmentBonusCache;
    private bool _investmentBonusValid;

    /// <summary>투자 슬롯이 모든 캐릭터에게 주는 전투 스탯 보정(2회 공격/회피/치명타 등).</summary>
    public CombatStats InvestmentBonus {
        get {
            if (!_investmentBonusValid) {
                _investmentBonusCache = InvestmentRules.StatBonus(InvestmentLevels);
                _investmentBonusValid = true;
            }
            return _investmentBonusCache;
        }
    }

    /// <summary>서버가 내려준 투자 현황을 반영한다.</summary>
    public void ApplyInvestment(InvestmentDto dto) {
        if (dto == null) return;
        InvestmentLevels.Clear();
        foreach (var kv in dto.Levels) InvestmentLevels[kv.Key] = kv.Value;
        OnInvestmentChanged();
    }

    /// <summary>슬롯 하나의 레벨만 갱신한다(업그레이드 직후).</summary>
    public void ApplyInvestmentLevel(int slotIndex, int level) {
        InvestmentLevels[slotIndex] = level;
        OnInvestmentChanged();
    }

    // 투자 스탯은 모든 캐릭터의 최종 스탯에 들어가므로, 본부와 같이 성장 변경으로 알려
    // 전투 중 유닛 스탯과 전투력 표시가 즉시 다시 계산되게 한다.
    private void OnInvestmentChanged() {
        _investmentBonusValid = false;
        InvestmentChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);
    }

    /// <summary>지정 효과의 총 증가분(1000 분율). 전투 효과가 이 값 하나만 보면 되도록 여기서 합산한다.</summary>
    public int InvestmentPermille(InvestmentEffect effect) => InvestmentRules.PermilleOf(InvestmentLevels, effect);

    /// <summary>덱 구성이 바뀌었을 때 알림(HUD 갱신 등).</summary>
    public event Action DeckChanged;
    /// <summary>캐릭터 성장치/보유가 바뀌었을 때 알림. 정밀 갱신을 위해 "무엇이 왜 바뀌었는지"(CharacterChange)를 함께 전달한다.
    /// 원인/대상을 신경 쓰지 않는 구독자는 인자를 무시하고 전체 갱신하면 된다(예: 배지/정보 패널).</summary>
    public event Action<CharacterChange> CharactersChanged;

    private void RaiseCharactersChanged(CharacterChangeReason reason, IReadOnlyList<int> ids = null) =>
        CharactersChanged?.Invoke(new CharacterChange(reason, ids));

    /// <summary>
    /// 씬 진입 초기 데이터를 받아오는 중인지. 계정 스냅샷/인벤토리/각인/본부가 순차로 도착하며 값이 계단식으로 채워지는 구간이라,
    /// "변화"에 반응하는 표시(전투력 상승 연출 등)는 이 구간을 상승으로 오해하면 안 된다.
    /// 값 자체를 쓰는 표시(현재 전투력 숫자 등)는 신경 쓸 필요가 없다.
    /// </summary>
    public bool IsInitialLoading { get; private set; }

    /// <summary>초기 로딩 시작을 알린다(씬 진입 오케스트레이터가 호출). 실패로 끊겨도 반드시 EndInitialLoad 로 닫을 것.</summary>
    public void BeginInitialLoad() => IsInitialLoading = true;

    /// <summary>
    /// 초기 로딩이 끝났을 때(성공/실패 무관). 씬 진입 직후에 스토어 값을 읽어야 하는 화면이 이 신호를 기다린다.
    /// 로딩 중에 그리면 전투력/보유량 같은 값이 0 으로 찍힌 채 남는다.
    /// </summary>
    public event Action InitialLoadCompleted;

    /// <summary>초기 로딩 종료를 알린다.</summary>
    public void EndInitialLoad() {
        IsInitialLoading = false;
        InitialLoadCompleted?.Invoke();
    }

    public void ApplyMainData(MainDataDto d) {
        // 상한/기준선 같은 비반응 값을 ReactiveProperty 보다 먼저 넣는다.
        // 반응 값에 대입하는 순간 구독자가 그 자리에서 실행되므로, 뒤에 두면 구독자가 아직 0인 상한을 읽어
        // "180 / 0" 같은 화면이 만들어진다(다음 변경 때까지 그대로 남는다).
        StaminaMax = d.StaminaMax;
        if (d.StaminaBoostTicketDaily > 0) StaminaBoostTicketDaily = d.StaminaBoostTicketDaily;

        Nickname.Value = d.Nickname ?? "";
        EquippedProfileFrameId.Value = d.EquippedProfileFrameId;
        Gold.Value = d.Gold;
        Diamond.Value = d.Diamond;
        // 부스트권을 기력보다 먼저 넣는다. 부스트 버튼은 두 값을 함께 보고 활성 여부를 정하므로
        // 마지막에 들어오는 값이 최종 판정을 내리게 해야 한다.
        //
        // 상한과 짝을 이루는 값은 강제 통지한다. HUD 는 스냅샷이 도착하기 전에 이미 구독을 걸어 두므로
        // (ApplyMainData 는 서버 응답 뒤에 실행된다), 서버 값이 구독 시점의 값과 같으면 ReactiveProperty 가
        // 발화하지 않아 상한이 0/기본값인 채로 그려진 화면이 그대로 굳는다(예: 기력이 실제로 0인 계정).
        StaminaBoostTicket.SetValueAndForceNotify(d.StaminaBoostTicket);
        Stamina.SetValueAndForceNotify(d.Stamina);
        CurrentDungeonIndex.Value = d.CurrentDungeonIndex;
        HighestClearedDungeonIndex.Value = d.HighestClearedDungeonIndex;

        Characters.Clear();
        foreach (var c in d.Characters)
            if (c != null) Characters[c.CharDataId] = c;

        Deck.Set(d.DeckCharIds, d.RepresentativeCharId);
        DeckChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Snapshot);   // 전체 스냅샷 교체(로스터 포함)
    }

    public CharacterProgress GetProgress(int charDataId) =>
        Characters.TryGetValue(charDataId, out var p) ? p : null;

    // ── 장비 ─────────────────────────────────────
    public void ApplyInventory(InventoryDto inv) {
        Equipment.Clear();
        UniversalShards.Clear();
        if (inv != null) {
            foreach (var e in inv.Equipment) Equipment[e.InstanceId] = e;
            foreach (var kv in inv.UniversalShards) UniversalShards[kv.Key] = kv.Value;
        }
        InventoryChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);   // 장비 변경은 능력치에 영향(PLANNING 3-3 즉시 반영). 대상 불명(전체).
    }

    /// <summary>만능조각 전환 결과 반영(캐릭터 조각 증가 + 만능조각 감소).</summary>
    public void ApplyShardConvert(ConvertShardResult r) {
        if (r == null || !r.IsSuccess) return;
        if (Characters.TryGetValue(r.CharDataId, out var p)) p.Shards = r.NewCharShards;
        UniversalShards[r.Grade] = r.NewUniversalQty;
        InventoryChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { r.CharDataId });
    }

    /// <summary>
    /// 지정 캐릭터가 장착한 장비들의 합산 스탯 보정. 장비 스탯이 전투/전투력/상세 표시로 흘러가는 단일 관문이므로
    /// 여기만 고치면 소비측(전투/덱 전투력/도감 상세)이 전부 따라온다.
    /// 본인 전용무기를 장착한 경우 전용무기 보너스(EquipmentRules.SignatureBonus)를 함께 더한다.
    /// </summary>
    public CombatStats EquipmentBonusFor(int charDataId, EquipmentDatabase db) {
        if (db == null || charDataId == 0) return new CombatStats();
        // 조립 규칙은 CharacterBuild 한 곳에만 둔다(대전의 상대 파티도 같은 함수를 쓴다).
        return CharacterBuild.EquipmentBonus(EquippedItemsOf(charDataId), charDataId, db);
    }

    // 이 캐릭터가 장착 중인 장비만 추려 낸다(슬롯 중복 방어는 조립 쪽이 한다).
    private IEnumerable<EquipmentInstanceDto> EquippedItemsOf(int charDataId) {
        foreach (var kv in Equipment)
            if (kv.Value != null && kv.Value.EquippedCharDataId == charDataId) yield return kv.Value;
    }

    /// <summary>장비 1개의 최종 스탯(성장 + 한계돌파 반영, 전용무기 보너스 제외). 상세/비교 표시와 합산이 같은 식을 쓰도록 모아둔다.</summary>
    public static CombatStats EquipmentStatsOf(EquipmentInstanceDto inst, EquipmentData data) =>
        EquipmentRules.ResolveStats(inst, data);

    /// <summary>
    /// 지정 장비를 해당 캐릭터에게 장착했을 때의 전투력 변화량(미착용 장비 비교용).
    /// 같은 슬롯에 이미 장착된 장비는 교체되어 빠지는 것으로 계산한다(스왑 결과가 곧 실제 결과).
    ///
    /// 전투력은 스탯의 가중합이 아니라 곱연산 기반이므로(CombatPowerFormula) 델타 스탯만으로는 변화량을 알 수 없다.
    /// 반드시 장착 전/후 두 상태의 전투력을 각각 구해 차이를 낸다. 그 결과 같은 장비라도 캐릭터의 현재 빌드에 따라
    /// 표시되는 상승치가 달라지며, 이것이 실제 강해지는 정도와 일치한다.
    /// </summary>
    public float CombatPowerDeltaIfEquipped(CharacterData character, EquipmentInstanceDto inst,
                                            EquipmentDatabase db, BattleSettings settings) {
        if (character == null || inst == null || db == null || settings == null) return 0f;
        var data = db.Get(inst.EquipDataId);
        if (data == null) return 0f;

        int charDataId = character.Id;
        var progress = GetProgress(charDataId);
        if (progress == null) return 0f;

        var gain = EquipGainStats(inst, data, charDataId, EquippedItem(charDataId, data.Slot, db), db);
        var currentBonus = TotalBonusFor(charDataId, db);
        var conditionals = EngravingLoadoutFor(charDataId).Conditionals;
        float before = new CharacterRuntime(character, progress, settings, currentBonus, conditionals).CombatPower;
        float after  = new CharacterRuntime(character, progress, settings, currentBonus + gain, conditionals).CombatPower;
        return after - before;
    }

    /// <summary>
    /// 후보 목록 중 이 캐릭터의 전투력을 가장 많이 올려 주는 장비의 인스턴스 id(올려 주는 장비가 하나도 없으면 0).
    /// 가방처럼 후보가 많은 목록에서 "가장 좋은 교체 후보"를 표시하고 앞으로 당길 때 쓴다.
    ///
    /// 후보마다 CombatPowerDeltaIfEquipped 를 부르면 기준 전투력과 보정 합산, 같은 슬롯의 기존 장비 탐색을
    /// 후보 수만큼 반복하게 된다. 이 함수는 변하지 않는 것들을 한 번만 구해 두고 후보별 차이만 비교한다.
    /// 판정 식은 상세 패널이 보여 주는 상승치와 동일하다(같은 EquipGainStats 를 통한다).
    /// </summary>
    public long BestUpgradeInstanceId(CharacterData character, IReadOnlyList<EquipmentInstanceDto> candidates,
                                      EquipmentDatabase db, BattleSettings settings) {
        if (character == null || candidates == null || db == null || settings == null) return 0;
        var progress = GetProgress(character.Id);
        if (progress == null) return 0;

        // 같은 슬롯에 착용 중인 장비를 후보마다 다시 찾지 않도록 한 번에 모아 둔다.
        var equippedBySlot = new Dictionary<EquipmentSlot, EquipmentInstanceDto>();
        foreach (var kv in Equipment) {
            var e = kv.Value;
            if (e == null || e.EquippedCharDataId != character.Id) continue;
            var d = db.Get(e.EquipDataId);
            if (d != null) equippedBySlot[d.Slot] = e;
        }

        var currentBonus = TotalBonusFor(character.Id, db);
        var conditionals = EngravingLoadoutFor(character.Id).Conditionals;
        float before = new CharacterRuntime(character, progress, settings, currentBonus, conditionals).CombatPower;

        long bestId = 0;
        float bestGain = 0f;   // 0 에서 시작해 "오히려 약해지는 장비"는 추천하지 않는다.
        for (int i = 0; i < candidates.Count; i++) {
            var inst = candidates[i];
            var data = inst != null ? db.Get(inst.EquipDataId) : null;
            if (data == null) continue;

            equippedBySlot.TryGetValue(data.Slot, out var current);
            var gain = EquipGainStats(inst, data, character.Id, current, db);
            float delta = new CharacterRuntime(character, progress, settings, currentBonus + gain, conditionals).CombatPower - before;
            if (delta <= bestGain) continue;   // 동점이면 먼저 들어온 쪽(= 목록 정렬상 앞선 쪽)을 남긴다.
            bestGain = delta;
            bestId = inst.InstanceId;
        }
        return bestId;
    }

    // 이 장비를 끼워 생기는 스탯 변화량(전용무기 보너스 포함, 같은 슬롯에 착용 중이던 장비는 빠진다).
    // 상세 표시와 목록 비교가 같은 식을 쓰도록 한 곳에 모아 둔다.
    private static CombatStats EquipGainStats(EquipmentInstanceDto inst, EquipmentData data, int charDataId,
                                              EquipmentInstanceDto current, EquipmentDatabase db) {
        var gain = EquipmentStatsOf(inst, data);
        if (data.IsOwnSignatureOf(charDataId))
            gain += EquipmentRules.SignatureBonus(data.Grade, data.WeaponType, data.Id, inst.LimitBroken);

        if (current == null || current.InstanceId == inst.InstanceId) return gain;

        var currentData = db.Get(current.EquipDataId);
        if (currentData == null) return gain;

        gain += EquipmentStatsOf(current, currentData) * -1f;
        if (currentData.IsOwnSignatureOf(charDataId))
            gain += EquipmentRules.SignatureBonus(currentData.Grade, currentData.WeaponType, currentData.Id,
                                                  current.LimitBroken) * -1f;
        return gain;
    }

    /// <summary>
    /// target 을 각성시킬 때 candidate 를 재료로 쓸 수 있는지. 같은 장비 id 의 다른 인스턴스여야 하며,
    /// 다른 캐릭터가 착용 중인 것은 제외한다(보고 있지 않은 캐릭터의 장비가 모르는 사이에 소모되는 사고 방지).
    /// 한계돌파한 장비도 재료가 되지 않는다(서버 Awaken 과 같은 조건).
    /// 재료 목록과 각성 가능 표시가 같은 판정을 쓰도록 규칙을 여기 한 곳에 둔다.
    /// </summary>
    public static bool IsAwakenMaterial(EquipmentInstanceDto target, EquipmentInstanceDto candidate, int viewerCharDataId) {
        if (target == null || candidate == null) return false;
        if (candidate.InstanceId == target.InstanceId) return false;
        if (candidate.EquipDataId != target.EquipDataId) return false;
        if (candidate.LimitBroken) return false;
        return !candidate.IsEquipped || candidate.EquippedCharDataId == viewerCharDataId;
    }

    /// <summary>
    /// 지금 바로 각성할 수 있는지(별레벨 여유 + 재료 수량 + 다이아). 각성 패널의 버튼 조건과 같은 규칙이므로
    /// 표시가 켜졌는데 들어가 보면 버튼이 꺼져 있는 상황이 생기지 않는다.
    /// </summary>
    public bool CanAwakenNow(EquipmentInstanceDto inst, int viewerCharDataId) {
        if (inst == null || !EquipmentRules.CanAwaken(inst.StarLevel)) return false;
        if (Diamond.Value < EquipmentRules.AwakenDiaCost(inst.StarLevel)) return false;

        int need = EquipmentRules.AwakenMaterialCount(inst.StarLevel);
        int have = 0;
        foreach (var kv in Equipment) {
            if (!IsAwakenMaterial(inst, kv.Value, viewerCharDataId)) continue;
            if (++have >= need) return true;
        }
        return false;
    }

    /// <summary>지정 캐릭터가 해당 슬롯에 장착한 장비 인스턴스(없으면 null).</summary>
    public EquipmentInstanceDto EquippedItem(int charDataId, EquipmentSlot slot, EquipmentDatabase db) {
        if (db == null) return null;
        foreach (var kv in Equipment) {
            var e = kv.Value;
            if (e.EquippedCharDataId != charDataId) continue;
            var data = db.Get(e.EquipDataId);
            if (data != null && data.Slot == slot) return e;
        }
        return null;
    }

    public void SetStamina(int value) => Stamina.Value = value;

    /// <summary>
    /// 서버 범용 재화 지갑(PlayerCurrency)으로 관리되는 재화 목록. 상점 응답이 이 지갑 전체를 내려주므로
    /// 여기 있는 재화는 구매/보상 직후 잔량이 한 번에 맞는다.
    ///
    /// 목록을 따로 두는 이유: 응답에 없는 키를 0 으로 되돌려야 하는데(서버 권위), 응답 딕셔너리만 훑으면
    /// "보유량이 0이라 행이 없는 재화"가 옛 값으로 남는다. 그래서 "무엇을 맞출지"를 이 목록이 정한다.
    /// 새 재화가 이 지갑을 타면 여기 한 줄만 추가하면 되고, 빠뜨리면 그 재화만 조용히 갱신되지 않는다
    /// (실제로 명성/각인석이 빠져 있어 상점에서 사도 본부 배지가 즉시 켜지지 않는 문제가 있었다).
    ///
    /// 골드/다이아/기력은 지갑 컬럼이라 여기 두지 않고 카탈로그의 전용 필드로 반영하며,
    /// 의뢰해결권은 별도 저장소(PlayerCommissionState)라 이 지갑에 실려 오지 않는다.
    /// </summary>
    private static readonly CurrencyType[] ShopWalletCurrencies = {
        CurrencyType.NormalTicket,
        CurrencyType.SpecialTicket,
        CurrencyType.StageSweepTicket,
        CurrencyType.StaminaBoostTicket,
        CurrencyType.GuildPoint,
        CurrencyType.TrainingTicket,
        CurrencyType.Renown,
        CurrencyType.EngravingStone,
        CurrencyType.ArenaCoin,
        CurrencyType.DispatchCertificate,
        CurrencyType.GachaMileage,
        CurrencyType.LimitBreakTicket,
    };

    /// <summary>서버 상점 카탈로그의 범용 재화(티켓/명성/각인석 등) 잔량을 반영한다(없는 키는 0으로 재설정, 서버 권위).</summary>
    public void ApplyShopCurrencies(IReadOnlyDictionary<string, int> currencies) {
        foreach (var type in ShopWalletCurrencies)
            SetCurrency(type, Qty(currencies, CurrencyKeys.ToKey(type)));
    }

    private static int Qty(IReadOnlyDictionary<string, int> currencies, string key) =>
        currencies != null && currencies.TryGetValue(key, out var v) ? v : 0;

    public void ApplyDeck(IReadOnlyList<int> ids, int representativeId) {
        Deck.Set(ids, representativeId);
        DeckChanged?.Invoke();
    }

    /// <summary>서버가 내려준 최신 캐릭터 성장치로 교체한다(스탯 투자/별레벨 승급 결과 반영).</summary>
    public void ApplyCharacterSnapshot(CharacterProgress snapshot) {
        if (snapshot == null) return;
        Characters[snapshot.CharDataId] = snapshot;
        RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { snapshot.CharDataId });
    }

    /// <summary>
    /// 스탯포인트 자동 투자 대상만 갱신한다. 설정 응답에는 다른 성장치가 실려 오지 않으므로
    /// 스냅샷 전체를 교체하면 레벨/투자치가 0으로 날아간다.
    /// </summary>
    public void ApplyAutoStatType(int charDataId, int autoStatType) {
        if (!Characters.TryGetValue(charDataId, out var p) || p == null) return;
        p.AutoStatType = autoStatType;
        RaiseCharactersChanged(CharacterChangeReason.Growth, new[] { charDataId });
    }

    /// <summary>가챠/구매로 획득한 캐릭터를 반영한다(신규=보유 추가, 중복=조각 증가).</summary>
    public void ApplyGachaRolls(System.Collections.Generic.IReadOnlyList<GachaRollDto> rolls) {
        if (rolls == null) return;
        var ids = new List<int>(rolls.Count);
        foreach (var r in rolls) {
            if (r.IsNew) {
                if (!Characters.ContainsKey(r.CharDataId))
                    Characters[r.CharDataId] = new CharacterProgress {
                        CharDataId = r.CharDataId, Level = 1, StarLevel = 1, MaxExp = r.MaxExp
                    };
            } else if (Characters.TryGetValue(r.CharDataId, out var p)) {
                p.Shards += r.ShardsGained;
            }
            ids.Add(r.CharDataId);
        }
        RaiseCharactersChanged(CharacterChangeReason.Roster, ids);   // 신규 획득 등 보유 목록 변동(카드 수 변할 수 있음)
    }

    /// <summary>구매/장비 가챠로 새로 지급된 장비를 인벤토리에 즉시 추가한다(재로그인 없이 바로 확인 가능).</summary>
    public void ApplyEquipRolls(System.Collections.Generic.IReadOnlyList<EquipRollDto> rolls) {
        if (rolls == null || rolls.Count == 0) return;
        foreach (var r in rolls) {
            var inst = new EquipmentInstanceDto {
                InstanceId = r.InstanceId,
                EquipDataId = r.EquipDataId,
                StarLevel = r.StarLevel,
                Level = r.Level,
                MainPoints = r.MainPoints,
                EquippedCharDataId = 0,   // 지급은 항상 가방(미장착)
            };
            for (int i = 0; i < inst.OptionPoints.Length && i < r.OptionPoints.Length; i++)
                inst.OptionPoints[i] = r.OptionPoints[i];
            Equipment[r.InstanceId] = inst;
        }
        InventoryChanged?.Invoke();
        RaiseCharactersChanged(CharacterChangeReason.Growth);   // 장비 보유 변화가 전투력 표시 등에 반영되도록. 대상 불명(전체).
    }

    /// <summary>
    /// 던전 보상(경험치/골드/레벨업)과 해금 진행도를 반영한다.
    /// 기력/부스트권은 클리어 시점에 소모되므로 여기서 함께 서버 확정값으로 맞춘다.
    /// </summary>
    public void ApplyReward(DungeonRewardDto r) {
        Gold.Value = r.NewGold;
        Stamina.Value = r.NewStamina;
        StaminaBoostTicket.Value = r.NewBoostTicket;

        // 되돌아가지 않도록 최대값만 반영(뒤 던전을 다시 깨도 진행도는 유지).
        if (r.HighestClearedDungeonIndex > HighestClearedDungeonIndex.Value)
            HighestClearedDungeonIndex.Value = r.HighestClearedDungeonIndex;

        ApplyCharExp(r.CharExp);
    }

    /// <summary>
    /// 편성원 경험치/레벨 지급 결과를 반영한다(방치 던전 클리어와 의뢰 보상이 공유).
    /// 필요 경험치(MaxExp)는 서버가 확정해 보낸 값을 그대로 쓴다(클라에 성장 공식 미러를 두지 않는다).
    /// </summary>
    public void ApplyCharExp(IReadOnlyList<CharExpGain> gains) {
        if (gains == null || gains.Count == 0) return;

        var ids = new List<int>(gains.Count);
        foreach (var e in gains) {
            if (!Characters.TryGetValue(e.CharDataId, out var p)) continue;
            p.Level = e.NewLevel;
            p.Exp = e.NewExp;
            p.MaxExp = e.MaxExp;
            // 자동 투자가 켜진 캐릭터는 서버가 레벨업 즉시 그 스탯에 포인트를 넣어버린다(GameDataRepository.ApplyExp).
            // 여기서도 같은 규칙으로 반영해야 미사용 포인트/투자치가 서버와 어긋나지 않는다.
            if (!p.AddAutoInvested(e.GainedStatPoints)) p.StatPoints += e.GainedStatPoints;
            ids.Add(e.CharDataId);
        }
        RaiseCharactersChanged(CharacterChangeReason.Exp, ids);   // 경험치/레벨(가벼운 갱신 대상)
    }
}

/// <summary>캐릭터 변경 통지의 원인. 구독자가 필요한 만큼만 정밀하게 갱신하도록 한다.</summary>
public enum CharacterChangeReason {
    Snapshot,   // 전체 스냅샷 교체(로그인/메인 진입) - 로스터 포함 전면 갱신
    Roster,     // 보유 목록 변동(신규 획득 등) - 카드 수가 달라질 수 있음
    Exp,        // 전투 경험치/레벨(방치 보상) - 값만 가볍게 갱신(재정렬/재생성 불필요)
    Growth      // 성장/장비/조각 등 능력치 변동 - 상세/전투력 갱신 필요
}

/// <summary>캐릭터 변경 통지 상세. Ids 가 null 이면 대상 불명(전체로 간주).</summary>
public readonly struct CharacterChange {
    public readonly CharacterChangeReason Reason;
    public readonly System.Collections.Generic.IReadOnlyList<int> Ids;
    public CharacterChange(CharacterChangeReason reason, System.Collections.Generic.IReadOnlyList<int> ids = null) {
        Reason = reason;
        Ids = ids;
    }
}
