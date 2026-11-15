using ServerCore;

/// <summary>
/// 정적 패킷 핸들러. PacketManager 가 수신 패킷을 디스패치한다.
/// 이 클래스는 인스턴스(매니저)에 접근할 수 없으므로, 인스턴스 상태가 필요한 패킷은
/// 각 매니저가 자체 수신 루프에서 직접 처리한다(예: S_Pong -> NetworkManager.Update).
/// 이렇게 해서 정적 핸들러가 전역 싱글톤을 역참조하는 순환 의존을 제거한다.
/// </summary>
class PacketHandler {
    public static void S_PongHandler(PacketSession session, IPacket packet) {
        // RTT 측정은 NetworkManager 가 자체 수신 루프에서 직접 처리한다.
    }

    public static void S_VersionHandler(PacketSession session, IPacket packet) {
        // S_Version 응답은 VersionCheckStep 의 WaitPacketAsync<S_Version> 에서 처리한다.
    }

    public static void S_LoginHandler(PacketSession session, IPacket packet) {
        // S_Login 응답은 LoginManager 의 WaitPacketAsync<S_Login> 에서 처리한다.
    }

    // 약관 응답들은 TermsService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_TermsInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_AgreeTermsResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_WithdrawAccountResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EventStatusHandler(PacketSession session, IPacket packet) {
        // S_EventStatus 응답은 EventStatusSync 의 WaitPacketAsync<S_EventStatus> 에서 처리한다.
    }

    public static void S_AttendanceInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimAttendanceResultHandler(PacketSession session, IPacket packet) {
    }

    // 이벤트 미션 응답들은 EventMissionService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_EventMissionsHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimEventMissionResultHandler(PacketSession session, IPacket packet) {
    }

    // 하트비트 응답도 HeartbeatService 의 WaitPacketAsync<T> 에서 처리한다.
    public static void S_HeartbeatResultHandler(PacketSession session, IPacket packet) {
    }

    // 방치형 게임 응답들은 각각 MainDataService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_MainDataHandler(PacketSession session, IPacket packet) {
    }

    public static void S_StartDungeonResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DungeonRewardHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DungeonRewardPreviewHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ChangeDeckResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_AllocateStatResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_SweepStageResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ResetStatsResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_SetAutoStatResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UpgradeStarResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_LimitBreakResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ShopCatalogHandler(PacketSession session, IPacket packet) {
    }

    // 상점 상품 페이지는 ShopService 가 PacketReceived 로 모은다(정적 핸들러는 무동작).
    public static void S_ShopOfferPageHandler(PacketSession session, IPacket packet) {
    }

    public static void S_BuyResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_CanBuyOfferHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ReportChatResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DeleteWhisperResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GachaRatesHandler(PacketSession session, IPacket packet) {
        // S_GachaRates 응답은 ShopService 의 WaitPacketAsync<S_GachaRates> 에서 처리한다.
    }

    public static void S_GachaMileagePickResultHandler(PacketSession session, IPacket packet) {
        // 마일리지 픽업 응답은 ShopService 의 WaitPacketAsync<S_GachaMileagePickResult> 에서 처리한다.
    }

    // 멸망전 응답들은 DoomService 의 WaitPacketAsync 에서 처리한다.
    public static void S_DoomInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DoomResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DoomClaimResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DoomEntryInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DoomEnterResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DoomFinishResultHandler(PacketSession session, IPacket packet) {
    }

    // 무한 타워 응답은 전부 TowerService 가 WaitPacketAsync 로 받는다.
    public static void S_TowerInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_TowerEnterResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_TowerClearResultHandler(PacketSession session, IPacket packet) {
    }

    // 전선 응답은 전부 FrontlineService 가 WaitPacketAsync 로 받는다.
    public static void S_FrontlineInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_FrontlineSetMembersResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_FrontlineResetResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_FrontlineEnterResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_FrontlineFinishResultHandler(PacketSession session, IPacket packet) {
    }

    // 영웅전 응답은 전부 HeroesService 가 WaitPacketAsync 로 받는다.
    public static void S_HeroesInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_HeroesEnterResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_HeroesClearResultHandler(PacketSession session, IPacket packet) {
    }

    // 출석형 이벤트 응답은 AttendanceEventService 가 WaitPacketAsync 로 받는다.
    public static void S_AttendanceEventInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimAttendanceEventResultHandler(PacketSession session, IPacket packet) {
    }

    // 패스 현황(조회 응답 + 서버 푸시)은 PassService 가 PacketReceived 로, 수령 응답은 WaitPacketAsync 로 처리한다.
    public static void S_PassInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_PassClaimResultHandler(PacketSession session, IPacket packet) {
    }

    // 이벤트 돌림판 정보/돌리기 응답은 EventRouletteService 가 WaitPacketAsync 로 처리한다.
    public static void S_EventRouletteInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EventRouletteSpinResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_RotationInfoHandler(PacketSession session, IPacket packet) {
        // S_RotationInfo 응답은 ShopService 의 WaitPacketAsync<S_RotationInfo> 에서 처리한다.
    }

    // 프로필/프로필 테두리/닉네임 응답들은 ProfileService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ProfileHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EquipProfileFrameResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_CheckNicknameResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ChangeNicknameResultHandler(PacketSession session, IPacket packet) {
    }

    // 도감 보상 응답은 EncyclopediaService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_EncyclopediaRewardsHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimEncyclopediaRewardResultHandler(PacketSession session, IPacket packet) {
    }

    // 누적 충전 보상 응답은 ChargeRewardService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ChargeRewardsHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimChargeRewardResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_InventoryHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EquipResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UnequipResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_AwakenResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EquipLimitBreakResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_LevelUpEquipResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DisassembleEquipResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ConvertUniversalShardResultHandler(PacketSession session, IPacket packet) {
    }

    // 우편함 응답들은 MailService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_MailListHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimMailResultHandler(PacketSession session, IPacket packet) {
    }

    // 우편 도착 푸시(서버 발신). 특정 대기자 없이 상시 수신해야 하므로 MailService 가
    // INetworkService.PacketReceived 구독으로 처리한다(정적 핸들러는 무동작).
    public static void S_MailNotifyHandler(PacketSession session, IPacket packet) {
    }

    // 의뢰 응답들은 CommissionService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_CommissionInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_StartCommissionResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClearCommissionResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_SweepCommissionResultHandler(PacketSession session, IPacket packet) {
    }

    // 각인 응답들은 EngravingService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_EngravingInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UpgradeEngravingResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EquipEngravingResultHandler(PacketSession session, IPacket packet) {
    }

    // 채팅 응답/알림은 ChatService 가 처리한다(조회/발언은 WaitPacketAsync, 수신은 PacketReceived 상시 구독).
    public static void S_ChatSendResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ChatHistoryHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ChatMessageHandler(PacketSession session, IPacket packet) {
    }

    // 소셜 설정(귓속말 수신 범위) 응답도 ChatService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_SocialSettingsHandler(PacketSession session, IPacket packet) {
    }

    // 한정 판매 조회 응답은 FlashSaleService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_FlashSaleInfoHandler(PacketSession session, IPacket packet) {
    }

    // 한정 판매 알림은 요청 없는 서버 푸시라 대기자가 없다. FlashSaleService 가 PacketReceived 로 상시 수신한다.
    public static void S_FlashSaleNotifyHandler(PacketSession session, IPacket packet) {
    }

    // 코스튬 응답들은 CostumeService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_CostumeInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EquipCostumeResultHandler(PacketSession session, IPacket packet) {
    }

    // 아웃핏 장착 응답은 CostumeService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_EquipUnitSkinResultHandler(PacketSession session, IPacket packet) {
    }

    // 쿠폰 응답은 CouponService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_RedeemCouponResultHandler(PacketSession session, IPacket packet) {
    }

    // 쟁탈전 응답들은 ArenaService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ArenaHomeHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ArenaMatchResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ArenaReportResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ArenaLogHandler(PacketSession session, IPacket packet) {
    }

    // 룬 저장 / 역대 전적 응답은 ArenaService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ArenaSetRunesResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ArenaHistoryHandler(PacketSession session, IPacket packet) {
    }

    // 수련 응답들은 TrainingService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_TrainingInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_TrainingActionResultHandler(PacketSession session, IPacket packet) {
    }

    // 파견 응답들은 DispatchService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_DispatchInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DispatchActionResultHandler(PacketSession session, IPacket packet) {
    }

    // 파견 상태 푸시도 DispatchService 가 PacketReceived 로 상시 수신한다(우편 알림과 같은 방식).
    public static void S_DispatchNotifyHandler(PacketSession session, IPacket packet) {
    }

    // 용병단 본부 응답들은 HeadquartersService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_HeadquartersInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UpgradeHqNodeResultHandler(PacketSession session, IPacket packet) {
    }

    // 소모품 아이템 응답들은 ItemService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ItemInfoHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UseItemResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ItemUsePreviewHandler(PacketSession session, IPacket packet) {
    }

    // 편성 프리셋 응답들은 MainDataService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_DeckPresetsHandler(PacketSession session, IPacket packet) {
    }

    public static void S_DeckPresetResultHandler(PacketSession session, IPacket packet) {
    }

    // 길드 응답들은 GuildService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_GuildHomeHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildListHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildNameCheckHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildActionResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildApplicationsHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UserProfileHandler(PacketSession session, IPacket packet) {
    }

    // 길드 레이드 응답들은 GuildService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_GuildRaidStateHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EnterGuildRaidResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildRaidDamageResultHandler(PacketSession session, IPacket packet) {
    }

    // 길드 대항전 응답들은 GuildService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_GuildWarStateHandler(PacketSession session, IPacket packet) {
    }

    public static void S_BeginGuildWarAttackResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildWarAttackResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UserDeckHandler(PacketSession session, IPacket packet) {
    }

    public static void S_GuildWarLogHandler(PacketSession session, IPacket packet) {
    }

    // 친구 응답들은 FriendService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_FriendHomeHandler(PacketSession session, IPacket packet) {
    }

    public static void S_FriendActionResultHandler(PacketSession session, IPacket packet) {
    }

    // 투자 응답들은 InvestmentService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_InvestmentHandler(PacketSession session, IPacket packet) {
    }

    public static void S_UpgradeInvestmentResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_SearchFriendResultHandler(PacketSession session, IPacket packet) {
    }

    // 친구 신청 도착/취소 푸시(서버 발신). 특정 대기자 없이 상시 수신해야 하므로 FriendService 가
    // INetworkService.PacketReceived 구독으로 처리한다(정적 핸들러는 무동작).
    public static void S_FriendNotifyHandler(PacketSession session, IPacket packet) {
    }

    // 친구 기력 선물 응답은 FriendService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_FriendStaminaSendResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_FriendStaminaClaimResultHandler(PacketSession session, IPacket packet) {
    }

    // 업적 응답은 AchievementService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_AchievementsHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimAchievementRewardResultHandler(PacketSession session, IPacket packet) {
    }

    // 랭킹 응답은 RankingService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_RankingHandler(PacketSession session, IPacket packet) {
    }

    // 개발용 도구 응답도 DevelopTool 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_DevToolHandler(PacketSession session, IPacket packet) {
    }

    // 오프라인 보상 응답은 OfflineRewardService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_OfflineRewardHandler(PacketSession session, IPacket packet) {
    }

    // 푸시 발송 주소 등록/해제 응답은 PushTokenService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_PushTokenResultHandler(PacketSession session, IPacket packet) {
    }

    // 다이아 보유 내역 응답은 DiamondDetailService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_DiamondDetailHandler(PacketSession session, IPacket packet) {
    }

    // 스토리 응답은 StoryService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_StoryProgressHandler(PacketSession session, IPacket packet) {
    }

    public static void S_CompleteStoryResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ClaimStoryRewardResultHandler(PacketSession session, IPacket packet) {
    }

    // 메인 퀘스트 현황/완료 알림은 MainQuestService 가 PacketReceived 로 상시 수신한다(정적 핸들러는 무동작).
    public static void S_MainQuestHandler(PacketSession session, IPacket packet) {
    }

    public static void S_MainQuestNotifyHandler(PacketSession session, IPacket packet) {
    }

    // 메인 퀘스트 수령 응답은 MainQuestService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ClaimMainQuestResultHandler(PacketSession session, IPacket packet) {
    }

    // 원정대 응답들은 ExpeditionService 의 WaitPacketAsync<T> 에서 처리한다(정적 핸들러는 무동작).
    public static void S_ExpeditionHomeHandler(PacketSession session, IPacket packet) {
    }

    public static void S_EnterExpeditionResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ExpeditionDamageResultHandler(PacketSession session, IPacket packet) {
    }

    public static void S_ExpeditionLogHandler(PacketSession session, IPacket packet) {
    }
}
