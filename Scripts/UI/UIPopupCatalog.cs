using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

/// <summary>
/// 공용 팝업 프리팹을 인스펙터에서 직접 할당해 보관하는 카탈로그 에셋.
/// 문자열 주소 키(과거 UIKeys) 대신 AssetReference 를 드래그로 연결하므로 오타/주소 불일치가 원천 차단된다.
/// 컴포지션 루트(GameLifetimeScope)가 이 에셋을 등록하고, 팝업을 띄우는 소비자가 주입받아
/// ShowPopupAsync 에 해당 참조를 넘긴다.
///
/// 새 팝업 추가 시: 1) 필드를 하나 더 만들고 2) 접근 프로퍼티를 노출한 뒤 3) 인스펙터에서 프리팹을 할당한다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/UI Popup Catalog", fileName = "UIPopupCatalog")]
public sealed class UIPopupCatalog : ScriptableObject {
    [Header("Common")]
    [SerializeField] private AssetReferenceGameObject confirmPopup;
    [SerializeField] private AssetReferenceGameObject confirmCancelPopup;
    [Tooltip("3선택 확인 팝업(진행/제외/취소 등). 각성 재료 경고에서 사용.")]
    [SerializeField] private AssetReferenceGameObject threeChoicePopup;

    [Header("Terms (약관 동의)")]
    [Tooltip("가입 시 약관 동의 팝업(필수/선택 항목 체크). 로그인 직후 부트 게이트가 연다.")]
    [SerializeField] private AssetReferenceGameObject termsAgreementPopup;
    [Tooltip("약관 전문 팝업(상세 보기). 공식 홈페이지의 문서를 앱 내부 웹뷰로 표시한다.")]
    [SerializeField] private AssetReferenceGameObject termsDetailPopup;

    [Header("Notice / Event")]
    [SerializeField] private AssetReferenceGameObject noticePopup;
    [SerializeField] private AssetReferenceGameObject noticeDetailPopup;
    [SerializeField] private AssetReferenceGameObject eventPopup;

    [Header("Main / Battle")]
    [SerializeField] private AssetReferenceGameObject dungeonSelectPopup;
    [Tooltip("던전 진행 중 다른 던전 선택 시 변경 예약/즉시 변경을 고르는 팝업.")]
    [SerializeField] private AssetReferenceGameObject dungeonChangePopup;
    [Tooltip("던전 클리어 획득 표시(Light 레이어, 자동 페이드).")]
    [SerializeField] private AssetReferenceGameObject dungeonClearRewardPopup;
    [Tooltip("스테이지 소탕 팝업. 소탕권 장수를 골라 진행 중인 던전을 한 번에 클리어한다.")]
    [SerializeField] private AssetReferenceGameObject stageSweepPopup;
    [Tooltip("재화 설명 팝업. 재화 아이콘을 누르면 이름/설명/보유량을 보여준다(CurrencyView 공용).")]
    [SerializeField] private AssetReferenceGameObject currencyInfoPopup;
    [SerializeField] private AssetReferenceGameObject deckPopup;
    // 육성/장비는 DeckPopup 내부 패널로 통합되어 별도 프리팹/참조를 사용하지 않는다.
    [SerializeField] private AssetReferenceGameObject encyclopediaPopup;
    [Tooltip("도감 보상(수집/별레벨) 팝업. 두 스크롤뷰에서 단계별 보상을 수령한다.")]
    [SerializeField] private AssetReferenceGameObject encyclopediaRewardPopup;
    [Tooltip("누적 충전 보상 팝업. 재화>다이아 페이지에서 연다.")]
    [SerializeField] private AssetReferenceGameObject chargeRewardPopup;
    [SerializeField] private AssetReferenceGameObject characterDetailPopup;
    [SerializeField] private AssetReferenceGameObject shopPopup;
    [Tooltip("한정 판매 안내 팝업(FlashSalePopup). 한정 판매가 새로 열릴 때와 메인 버튼으로 연다.")]
    [SerializeField] private AssetReferenceGameObject flashSalePopup;
    [Tooltip("상점 구매 확인 팝업(상품/필요 재화/보유 재화 표시).")]
    [SerializeField] private AssetReferenceGameObject buyConfirmPopup;
    [Tooltip("상점 구매 완료 팝업(획득한 구성품 표시). 비우면 기존처럼 토스트로 알린다.")]
    [SerializeField] private AssetReferenceGameObject purchaseCompletePopup;
    [SerializeField] private AssetReferenceGameObject gachaPopup;
    [Tooltip("장비 가챠/구매 시 뽑은 장비를 연출하는 팝업(캐릭터 GachaPopup 의 장비 버전).")]
    [SerializeField] private AssetReferenceGameObject equipGachaPopup;
    [Tooltip("가챠 확률 표시 팝업(풀 항목 이름/등급/확률 리스트).")]
    [SerializeField] private AssetReferenceGameObject gachaRatesPopup;
    [Tooltip("픽업 로테이션 안내 팝업(현재/다음 픽업 대상과 기간).")]
    [SerializeField] private AssetReferenceGameObject rotationInfoPopup;
    [SerializeField] private AssetReferenceGameObject shardPopup;
    [Tooltip("스탯포인트 일괄 투자 팝업(스탯 드롭다운 + 수량 슬라이더). 육성 팝업의 일괄 투자 버튼에서 연다.")]
    [SerializeField] private AssetReferenceGameObject growthStatBulkPopup;

    [Header("Commission (의뢰)")]
    [Tooltip("의뢰 목록 팝업. 메인 HUD 햄버거 메뉴의 의뢰 버튼(HudMenuButton)에서 연다.")]
    [SerializeField] private AssetReferenceGameObject commissionPopup;
    [Tooltip("의뢰 즉시해결 팝업(사용 장수 선택 + 예정 보상).")]
    [SerializeField] private AssetReferenceGameObject commissionSweepPopup;
    [Tooltip("의뢰 씬 일시정지 팝업(계속/나가기).")]
    [SerializeField] private AssetReferenceGameObject commissionPausePopup;
    [Tooltip("의뢰 클리어/실패 팝업(다음 단계/나가기). 즉시해결 결과 표시에도 재사용한다.")]
    [SerializeField] private AssetReferenceGameObject commissionResultPopup;

    [Header("Growth (각인 / 용병단 본부)")]
    [Tooltip("용병단 본부 노드 트리 팝업.")]
    [SerializeField] private AssetReferenceGameObject headquartersPopup;
    [Tooltip("각인 선택 팝업(슬롯에 끼울 각인 고르기).")]
    [SerializeField] private AssetReferenceGameObject engravingSelectPopup;

    [Header("Coupon (쿠폰)")]
    [Tooltip("쿠폰 팝업(번호 입력 -> 보상 우편 발송). 쿠폰 버튼(HudMenuButton)에서 연다.")]
    [SerializeField] private AssetReferenceGameObject couponPopup;

    [Header("Investment (투자)")]
    [Tooltip("투자 팝업(골드 소모처 + 계정 전체 효과). 슬라이드 메뉴의 리더십 버튼에서 연다.")]
    [SerializeField] private AssetReferenceGameObject investmentPopup;

    [Header("Arena (쟁탈전)")]
    [Tooltip("쟁탈전 팝업(쟁탈전 / 상점 탭). 메인 HUD 햄버거 메뉴의 쟁탈전 버튼에서 연다.")]
    [SerializeField] private AssetReferenceGameObject arenaPopup;
    [Tooltip("쟁탈전 매칭 연출 팝업(매칭중 -> 양쪽 편성 비교).")]
    [SerializeField] private AssetReferenceGameObject arenaMatchPopup;
    [Tooltip("쟁탈전 내역 팝업(내가 공격했거나 방어자인 기록).")]
    [SerializeField] private AssetReferenceGameObject arenaLogPopup;

    [Header("Reward Info (보상 정보)")]
    [Tooltip("보상 정보 팝업(공용). 조건별로 무엇을 받는지 표로 보여 준다. " +
             "길드 레이드(보스 레벨별)/대항전(승패)/쟁탈전 등 여러 컨텐츠가 이 프리팹 하나를 함께 쓴다.")]
    [SerializeField] private AssetReferenceGameObject rewardInfoPopup;

    [Header("Training (수련)")]
    [Tooltip("수련 팝업(슬롯 5칸 + 소녀 목록). 메인 HUD 의 수련 버튼(HudMenuButton)에서 연다.")]
    [SerializeField] private AssetReferenceGameObject trainingPopup;
    [Tooltip("수련 효과 확률 팝업(효과별 등장 확률/수치 범위/최종 확률).")]
    [SerializeField] private AssetReferenceGameObject trainingRatesPopup;

    [Header("Dispatch (파견)")]
    [Tooltip("파견 팝업(소녀 목록 + 파견 슬롯 5칸). 슬라이드 메뉴의 파견 버튼(HudMenuButton)에서 연다.")]
    [SerializeField] private AssetReferenceGameObject dispatchPopup;

    [Header("Deck")]
    [Tooltip("편성 프리셋 팝업(3칸 저장/적용).")]
    [SerializeField] private AssetReferenceGameObject deckPresetPopup;
    [Tooltip("프리셋 이름 변경 팝업(입력 칸 + 확인/취소).")]
    [SerializeField] private AssetReferenceGameObject deckPresetRenamePopup;
    [Tooltip("아이템 사용 팝업(선택 캐릭터에게 경험치 쿠폰 등을 사용).")]
    [SerializeField] private AssetReferenceGameObject itemUsePopup;

    [Header("Mail")]
    [Tooltip("우편함 팝업. HUD 메뉴 버튼(HudMenuButton)에서 열며, 카탈로그에도 등록해 코드에서 열 수도 있게 둔다.")]
    [SerializeField] private AssetReferenceGameObject mailboxPopup;

    [Header("Profile / Profile Frame")]
    [Tooltip("프로필 팝업(닉네임/프로필 테두리 변경 진입).")]
    [SerializeField] private AssetReferenceGameObject profilePopup;
    [Tooltip("프로필 테두리 장착/변경/해제 팝업.")]
    [FormerlySerializedAs("nameplatePopup")]
    [SerializeField] private AssetReferenceGameObject profileFramePopup;
    [Tooltip("닉네임 변경 팝업(중복확인 + 변경권/다이아 결제).")]
    [SerializeField] private AssetReferenceGameObject nicknameChangePopup;

    [Header("Guild")]
    [Tooltip("길드 팝업(없을때/있을때 그룹 + 정보ㆍ길드원ㆍ컨텐츠ㆍ관리 탭).")]
    [SerializeField] private AssetReferenceGameObject guildPopup;
    [Tooltip("길드 생성 팝업(이름 중복확인 + 가입방식).")]
    [SerializeField] private AssetReferenceGameObject guildCreatePopup;
    [Tooltip("길드 검색 팝업(이름 검색 + 가입/신청).")]
    [SerializeField] private AssetReferenceGameObject guildSearchPopup;
    [Tooltip("다른 유저 프로필 팝업(길드원 목록에서 연다). 본인용 profilePopup 과 다른 프리팹이다.")]
    [SerializeField] private AssetReferenceGameObject userProfilePopup;
    [Tooltip("길드 프로필 팝업(마크/이름/길드장/생성일/전투력/레벨 + 가입 버튼). 길드 id 만 있으면 어디서든 연다.")]
    [SerializeField] private AssetReferenceGameObject guildProfilePopup;
    [Tooltip("채팅 팝업(전체/길드/귓속말). 슬라이드 메뉴 버튼 외에 유저 프로필의 귓속말 버튼도 이 참조로 연다.")]
    [SerializeField] private AssetReferenceGameObject chatPopup;
    [Tooltip("길드장 위임 대상 선택 팝업(길드원 목록에서 고른다). 길드 관리 탭의 위임 버튼에서 연다.")]
    [SerializeField] private AssetReferenceGameObject guildTransferMasterPopup;
    [Tooltip("길드 레이드 결과 팝업(깎은 피해량 + 보스 현재 상태).")]
    [SerializeField] private AssetReferenceGameObject guildRaidResultPopup;
    [Tooltip("대항전 교전 기록 팝업(누가 누구를 쳐서 이겼는지).")]
    [SerializeField] private AssetReferenceGameObject guildWarLogPopup;
    [Tooltip("유저 대 유저 전투 결과 팝업(승패 + 상대 이름). 대항전/향후 아레나 공용.")]
    [SerializeField] private AssetReferenceGameObject pvpBattleResultPopup;

    [Header("Friend")]
    [Tooltip("친구 팝업(친구 목록ㆍ친구 신청ㆍ차단 유저 탭).")]
    [SerializeField] private AssetReferenceGameObject friendPopup;
    [Tooltip("친구 검색 팝업(닉네임으로 찾아 신청). 친구 팝업의 검색 버튼에서 연다.")]
    [SerializeField] private AssetReferenceGameObject friendSearchPopup;

    [Header("Achievement / Ranking")]
    [Tooltip("업적 팝업(요약ㆍ게임ㆍ길드ㆍ성장ㆍ모집 탭).")]
    [SerializeField] private AssetReferenceGameObject achievementPopup;
    [Tooltip("랭킹 팝업(유저ㆍ길드ㆍ업적 탭).")]
    [SerializeField] private AssetReferenceGameObject rankingPopup;

    [Header("Screen Lock")]
    [Tooltip("화면 잠금(잠수) 팝업. 메뉴의 잠금 버튼(HudMenuButton)과 자동 잠금(ScreenLockWatcher)이 같은 프리팹을 띄운다.")]
    [SerializeField] private AssetReferenceGameObject screenLockPopup;

    [Header("Offline Reward")]
    [Tooltip("오프라인 보상 팝업. 메인 진입 직후 서버가 지급한 결과가 있을 때만 뜬다. 비워 두면 토스트로 알린다.")]
    [SerializeField] private AssetReferenceGameObject offlineRewardPopup;

    public AssetReference ConfirmPopup => confirmPopup;
    public AssetReference ConfirmCancelPopup => confirmCancelPopup;
    public AssetReference ThreeChoicePopup => threeChoicePopup;
    public AssetReference TermsAgreementPopup => termsAgreementPopup;
    public AssetReference TermsDetailPopup => termsDetailPopup;
    public AssetReference NoticePopup => noticePopup;
    public AssetReference NoticeDetailPopup => noticeDetailPopup;
    public AssetReference EventPopup => eventPopup;
    public AssetReference DungeonSelectPopup => dungeonSelectPopup;
    public AssetReference DungeonChangePopup => dungeonChangePopup;
    public AssetReference DungeonClearRewardPopup => dungeonClearRewardPopup;
    public AssetReference StageSweepPopup => stageSweepPopup;
    public AssetReference CurrencyInfoPopup => currencyInfoPopup;
    public AssetReference DeckPopup => deckPopup;
    public AssetReference EncyclopediaPopup => encyclopediaPopup;
    public AssetReference EncyclopediaRewardPopup => encyclopediaRewardPopup;
    public AssetReference ChargeRewardPopup => chargeRewardPopup;
    public AssetReference CharacterDetailPopup => characterDetailPopup;
    public AssetReference ShopPopup => shopPopup;
    public AssetReference FlashSalePopup => flashSalePopup;
    public AssetReference BuyConfirmPopup => buyConfirmPopup;
    public AssetReference PurchaseCompletePopup => purchaseCompletePopup;
    public AssetReference GachaPopup => gachaPopup;
    public AssetReference EquipGachaPopup => equipGachaPopup;
    public AssetReference GachaRatesPopup => gachaRatesPopup;
    public AssetReference RotationInfoPopup => rotationInfoPopup;
    public AssetReference ShardPopup => shardPopup;
    public AssetReference GrowthStatBulkPopup => growthStatBulkPopup;
    public AssetReference CommissionPopup => commissionPopup;
    public AssetReference CommissionSweepPopup => commissionSweepPopup;
    public AssetReference CommissionPausePopup => commissionPausePopup;
    public AssetReference CommissionResultPopup => commissionResultPopup;
    public AssetReference HeadquartersPopup => headquartersPopup;
    public AssetReference CouponPopup => couponPopup;
    public AssetReference InvestmentPopup => investmentPopup;
    public AssetReference ArenaPopup => arenaPopup;
    public AssetReference ArenaMatchPopup => arenaMatchPopup;
    public AssetReference ArenaLogPopup => arenaLogPopup;
    public AssetReference RewardInfoPopup => rewardInfoPopup;
    public AssetReference TrainingPopup => trainingPopup;
    public AssetReference TrainingRatesPopup => trainingRatesPopup;
    public AssetReference DispatchPopup => dispatchPopup;
    public AssetReference EngravingSelectPopup => engravingSelectPopup;
    public AssetReference DeckPresetPopup => deckPresetPopup;
    public AssetReference DeckPresetRenamePopup => deckPresetRenamePopup;
    public AssetReference ItemUsePopup => itemUsePopup;
    public AssetReference MailboxPopup => mailboxPopup;
    public AssetReference ProfilePopup => profilePopup;
    public AssetReference ProfileFramePopup => profileFramePopup;
    public AssetReference NicknameChangePopup => nicknameChangePopup;
    public AssetReference ScreenLockPopup => screenLockPopup;
    public AssetReference OfflineRewardPopup => offlineRewardPopup;
    public AssetReference GuildPopup => guildPopup;
    public AssetReference GuildCreatePopup => guildCreatePopup;
    public AssetReference GuildSearchPopup => guildSearchPopup;
    public AssetReference UserProfilePopup => userProfilePopup;
    public AssetReference GuildProfilePopup => guildProfilePopup;
    public AssetReference ChatPopup => chatPopup;
    public AssetReference GuildTransferMasterPopup => guildTransferMasterPopup;
    public AssetReference GuildRaidResultPopup => guildRaidResultPopup;
    public AssetReference GuildWarLogPopup => guildWarLogPopup;
    public AssetReference PvpBattleResultPopup => pvpBattleResultPopup;
    public AssetReference FriendPopup => friendPopup;
    public AssetReference FriendSearchPopup => friendSearchPopup;
    public AssetReference AchievementPopup => achievementPopup;
    public AssetReference RankingPopup => rankingPopup;
}
