using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
	Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
		
	public void Register()
	{
		_makeFunc.Add((ushort)PacketID.S_Pong, MakePacket<S_Pong>);
		_handler.Add((ushort)PacketID.S_Pong, PacketHandler.S_PongHandler);
		_makeFunc.Add((ushort)PacketID.S_Version, MakePacket<S_Version>);
		_handler.Add((ushort)PacketID.S_Version, PacketHandler.S_VersionHandler);
		_makeFunc.Add((ushort)PacketID.S_Login, MakePacket<S_Login>);
		_handler.Add((ushort)PacketID.S_Login, PacketHandler.S_LoginHandler);
		_makeFunc.Add((ushort)PacketID.S_TermsInfo, MakePacket<S_TermsInfo>);
		_handler.Add((ushort)PacketID.S_TermsInfo, PacketHandler.S_TermsInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_WithdrawAccountResult, MakePacket<S_WithdrawAccountResult>);
		_handler.Add((ushort)PacketID.S_WithdrawAccountResult, PacketHandler.S_WithdrawAccountResultHandler);
		_makeFunc.Add((ushort)PacketID.S_AgreeTermsResult, MakePacket<S_AgreeTermsResult>);
		_handler.Add((ushort)PacketID.S_AgreeTermsResult, PacketHandler.S_AgreeTermsResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EventStatus, MakePacket<S_EventStatus>);
		_handler.Add((ushort)PacketID.S_EventStatus, PacketHandler.S_EventStatusHandler);
		_makeFunc.Add((ushort)PacketID.S_AttendanceInfo, MakePacket<S_AttendanceInfo>);
		_handler.Add((ushort)PacketID.S_AttendanceInfo, PacketHandler.S_AttendanceInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimAttendanceResult, MakePacket<S_ClaimAttendanceResult>);
		_handler.Add((ushort)PacketID.S_ClaimAttendanceResult, PacketHandler.S_ClaimAttendanceResultHandler);
		_makeFunc.Add((ushort)PacketID.S_MainData, MakePacket<S_MainData>);
		_handler.Add((ushort)PacketID.S_MainData, PacketHandler.S_MainDataHandler);
		_makeFunc.Add((ushort)PacketID.S_StartDungeonResult, MakePacket<S_StartDungeonResult>);
		_handler.Add((ushort)PacketID.S_StartDungeonResult, PacketHandler.S_StartDungeonResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DungeonReward, MakePacket<S_DungeonReward>);
		_handler.Add((ushort)PacketID.S_DungeonReward, PacketHandler.S_DungeonRewardHandler);
		_makeFunc.Add((ushort)PacketID.S_SweepStageResult, MakePacket<S_SweepStageResult>);
		_handler.Add((ushort)PacketID.S_SweepStageResult, PacketHandler.S_SweepStageResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DungeonRewardPreview, MakePacket<S_DungeonRewardPreview>);
		_handler.Add((ushort)PacketID.S_DungeonRewardPreview, PacketHandler.S_DungeonRewardPreviewHandler);
		_makeFunc.Add((ushort)PacketID.S_ChangeDeckResult, MakePacket<S_ChangeDeckResult>);
		_handler.Add((ushort)PacketID.S_ChangeDeckResult, PacketHandler.S_ChangeDeckResultHandler);
		_makeFunc.Add((ushort)PacketID.S_AllocateStatResult, MakePacket<S_AllocateStatResult>);
		_handler.Add((ushort)PacketID.S_AllocateStatResult, PacketHandler.S_AllocateStatResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ResetStatsResult, MakePacket<S_ResetStatsResult>);
		_handler.Add((ushort)PacketID.S_ResetStatsResult, PacketHandler.S_ResetStatsResultHandler);
		_makeFunc.Add((ushort)PacketID.S_SetAutoStatResult, MakePacket<S_SetAutoStatResult>);
		_handler.Add((ushort)PacketID.S_SetAutoStatResult, PacketHandler.S_SetAutoStatResultHandler);
		_makeFunc.Add((ushort)PacketID.S_UpgradeStarResult, MakePacket<S_UpgradeStarResult>);
		_handler.Add((ushort)PacketID.S_UpgradeStarResult, PacketHandler.S_UpgradeStarResultHandler);
		_makeFunc.Add((ushort)PacketID.S_LimitBreakResult, MakePacket<S_LimitBreakResult>);
		_handler.Add((ushort)PacketID.S_LimitBreakResult, PacketHandler.S_LimitBreakResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ShopCatalog, MakePacket<S_ShopCatalog>);
		_handler.Add((ushort)PacketID.S_ShopCatalog, PacketHandler.S_ShopCatalogHandler);
		_makeFunc.Add((ushort)PacketID.S_ShopOfferPage, MakePacket<S_ShopOfferPage>);
		_handler.Add((ushort)PacketID.S_ShopOfferPage, PacketHandler.S_ShopOfferPageHandler);
		_makeFunc.Add((ushort)PacketID.S_CanBuyOffer, MakePacket<S_CanBuyOffer>);
		_handler.Add((ushort)PacketID.S_CanBuyOffer, PacketHandler.S_CanBuyOfferHandler);
		_makeFunc.Add((ushort)PacketID.S_GachaMileagePickResult, MakePacket<S_GachaMileagePickResult>);
		_handler.Add((ushort)PacketID.S_GachaMileagePickResult, PacketHandler.S_GachaMileagePickResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GachaRates, MakePacket<S_GachaRates>);
		_handler.Add((ushort)PacketID.S_GachaRates, PacketHandler.S_GachaRatesHandler);
		_makeFunc.Add((ushort)PacketID.S_RotationInfo, MakePacket<S_RotationInfo>);
		_handler.Add((ushort)PacketID.S_RotationInfo, PacketHandler.S_RotationInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_BuyResult, MakePacket<S_BuyResult>);
		_handler.Add((ushort)PacketID.S_BuyResult, PacketHandler.S_BuyResultHandler);
		_makeFunc.Add((ushort)PacketID.S_Inventory, MakePacket<S_Inventory>);
		_handler.Add((ushort)PacketID.S_Inventory, PacketHandler.S_InventoryHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipResult, MakePacket<S_EquipResult>);
		_handler.Add((ushort)PacketID.S_EquipResult, PacketHandler.S_EquipResultHandler);
		_makeFunc.Add((ushort)PacketID.S_UnequipResult, MakePacket<S_UnequipResult>);
		_handler.Add((ushort)PacketID.S_UnequipResult, PacketHandler.S_UnequipResultHandler);
		_makeFunc.Add((ushort)PacketID.S_AwakenResult, MakePacket<S_AwakenResult>);
		_handler.Add((ushort)PacketID.S_AwakenResult, PacketHandler.S_AwakenResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipLimitBreakResult, MakePacket<S_EquipLimitBreakResult>);
		_handler.Add((ushort)PacketID.S_EquipLimitBreakResult, PacketHandler.S_EquipLimitBreakResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DisassembleEquipResult, MakePacket<S_DisassembleEquipResult>);
		_handler.Add((ushort)PacketID.S_DisassembleEquipResult, PacketHandler.S_DisassembleEquipResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ConvertUniversalShardResult, MakePacket<S_ConvertUniversalShardResult>);
		_handler.Add((ushort)PacketID.S_ConvertUniversalShardResult, PacketHandler.S_ConvertUniversalShardResultHandler);
		_makeFunc.Add((ushort)PacketID.S_MailList, MakePacket<S_MailList>);
		_handler.Add((ushort)PacketID.S_MailList, PacketHandler.S_MailListHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimMailResult, MakePacket<S_ClaimMailResult>);
		_handler.Add((ushort)PacketID.S_ClaimMailResult, PacketHandler.S_ClaimMailResultHandler);
		_makeFunc.Add((ushort)PacketID.S_MailNotify, MakePacket<S_MailNotify>);
		_handler.Add((ushort)PacketID.S_MailNotify, PacketHandler.S_MailNotifyHandler);
		_makeFunc.Add((ushort)PacketID.S_Profile, MakePacket<S_Profile>);
		_handler.Add((ushort)PacketID.S_Profile, PacketHandler.S_ProfileHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipProfileFrameResult, MakePacket<S_EquipProfileFrameResult>);
		_handler.Add((ushort)PacketID.S_EquipProfileFrameResult, PacketHandler.S_EquipProfileFrameResultHandler);
		_makeFunc.Add((ushort)PacketID.S_CheckNicknameResult, MakePacket<S_CheckNicknameResult>);
		_handler.Add((ushort)PacketID.S_CheckNicknameResult, PacketHandler.S_CheckNicknameResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ChangeNicknameResult, MakePacket<S_ChangeNicknameResult>);
		_handler.Add((ushort)PacketID.S_ChangeNicknameResult, PacketHandler.S_ChangeNicknameResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EncyclopediaRewards, MakePacket<S_EncyclopediaRewards>);
		_handler.Add((ushort)PacketID.S_EncyclopediaRewards, PacketHandler.S_EncyclopediaRewardsHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimEncyclopediaRewardResult, MakePacket<S_ClaimEncyclopediaRewardResult>);
		_handler.Add((ushort)PacketID.S_ClaimEncyclopediaRewardResult, PacketHandler.S_ClaimEncyclopediaRewardResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ChargeRewards, MakePacket<S_ChargeRewards>);
		_handler.Add((ushort)PacketID.S_ChargeRewards, PacketHandler.S_ChargeRewardsHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimChargeRewardResult, MakePacket<S_ClaimChargeRewardResult>);
		_handler.Add((ushort)PacketID.S_ClaimChargeRewardResult, PacketHandler.S_ClaimChargeRewardResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EventMissions, MakePacket<S_EventMissions>);
		_handler.Add((ushort)PacketID.S_EventMissions, PacketHandler.S_EventMissionsHandler);
		_makeFunc.Add((ushort)PacketID.S_HeartbeatResult, MakePacket<S_HeartbeatResult>);
		_handler.Add((ushort)PacketID.S_HeartbeatResult, PacketHandler.S_HeartbeatResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimEventMissionResult, MakePacket<S_ClaimEventMissionResult>);
		_handler.Add((ushort)PacketID.S_ClaimEventMissionResult, PacketHandler.S_ClaimEventMissionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_LevelUpEquipResult, MakePacket<S_LevelUpEquipResult>);
		_handler.Add((ushort)PacketID.S_LevelUpEquipResult, PacketHandler.S_LevelUpEquipResultHandler);
		_makeFunc.Add((ushort)PacketID.S_CommissionInfo, MakePacket<S_CommissionInfo>);
		_handler.Add((ushort)PacketID.S_CommissionInfo, PacketHandler.S_CommissionInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_StartCommissionResult, MakePacket<S_StartCommissionResult>);
		_handler.Add((ushort)PacketID.S_StartCommissionResult, PacketHandler.S_StartCommissionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ClearCommissionResult, MakePacket<S_ClearCommissionResult>);
		_handler.Add((ushort)PacketID.S_ClearCommissionResult, PacketHandler.S_ClearCommissionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_SweepCommissionResult, MakePacket<S_SweepCommissionResult>);
		_handler.Add((ushort)PacketID.S_SweepCommissionResult, PacketHandler.S_SweepCommissionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EngravingInfo, MakePacket<S_EngravingInfo>);
		_handler.Add((ushort)PacketID.S_EngravingInfo, PacketHandler.S_EngravingInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_UpgradeEngravingResult, MakePacket<S_UpgradeEngravingResult>);
		_handler.Add((ushort)PacketID.S_UpgradeEngravingResult, PacketHandler.S_UpgradeEngravingResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipEngravingResult, MakePacket<S_EquipEngravingResult>);
		_handler.Add((ushort)PacketID.S_EquipEngravingResult, PacketHandler.S_EquipEngravingResultHandler);
		_makeFunc.Add((ushort)PacketID.S_CostumeInfo, MakePacket<S_CostumeInfo>);
		_handler.Add((ushort)PacketID.S_CostumeInfo, PacketHandler.S_CostumeInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipUnitSkinResult, MakePacket<S_EquipUnitSkinResult>);
		_handler.Add((ushort)PacketID.S_EquipUnitSkinResult, PacketHandler.S_EquipUnitSkinResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipCostumeResult, MakePacket<S_EquipCostumeResult>);
		_handler.Add((ushort)PacketID.S_EquipCostumeResult, PacketHandler.S_EquipCostumeResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FlashSaleInfo, MakePacket<S_FlashSaleInfo>);
		_handler.Add((ushort)PacketID.S_FlashSaleInfo, PacketHandler.S_FlashSaleInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_FlashSaleNotify, MakePacket<S_FlashSaleNotify>);
		_handler.Add((ushort)PacketID.S_FlashSaleNotify, PacketHandler.S_FlashSaleNotifyHandler);
		_makeFunc.Add((ushort)PacketID.S_ChatSendResult, MakePacket<S_ChatSendResult>);
		_handler.Add((ushort)PacketID.S_ChatSendResult, PacketHandler.S_ChatSendResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ChatMessage, MakePacket<S_ChatMessage>);
		_handler.Add((ushort)PacketID.S_ChatMessage, PacketHandler.S_ChatMessageHandler);
		_makeFunc.Add((ushort)PacketID.S_DeleteWhisperResult, MakePacket<S_DeleteWhisperResult>);
		_handler.Add((ushort)PacketID.S_DeleteWhisperResult, PacketHandler.S_DeleteWhisperResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ReportChatResult, MakePacket<S_ReportChatResult>);
		_handler.Add((ushort)PacketID.S_ReportChatResult, PacketHandler.S_ReportChatResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ChatHistory, MakePacket<S_ChatHistory>);
		_handler.Add((ushort)PacketID.S_ChatHistory, PacketHandler.S_ChatHistoryHandler);
		_makeFunc.Add((ushort)PacketID.S_HeadquartersInfo, MakePacket<S_HeadquartersInfo>);
		_handler.Add((ushort)PacketID.S_HeadquartersInfo, PacketHandler.S_HeadquartersInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_UpgradeHqNodeResult, MakePacket<S_UpgradeHqNodeResult>);
		_handler.Add((ushort)PacketID.S_UpgradeHqNodeResult, PacketHandler.S_UpgradeHqNodeResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ItemInfo, MakePacket<S_ItemInfo>);
		_handler.Add((ushort)PacketID.S_ItemInfo, PacketHandler.S_ItemInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_UseItemResult, MakePacket<S_UseItemResult>);
		_handler.Add((ushort)PacketID.S_UseItemResult, PacketHandler.S_UseItemResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ItemUsePreview, MakePacket<S_ItemUsePreview>);
		_handler.Add((ushort)PacketID.S_ItemUsePreview, PacketHandler.S_ItemUsePreviewHandler);
		_makeFunc.Add((ushort)PacketID.S_DeckPresets, MakePacket<S_DeckPresets>);
		_handler.Add((ushort)PacketID.S_DeckPresets, PacketHandler.S_DeckPresetsHandler);
		_makeFunc.Add((ushort)PacketID.S_DeckPresetResult, MakePacket<S_DeckPresetResult>);
		_handler.Add((ushort)PacketID.S_DeckPresetResult, PacketHandler.S_DeckPresetResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildHome, MakePacket<S_GuildHome>);
		_handler.Add((ushort)PacketID.S_GuildHome, PacketHandler.S_GuildHomeHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildList, MakePacket<S_GuildList>);
		_handler.Add((ushort)PacketID.S_GuildList, PacketHandler.S_GuildListHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildNameCheck, MakePacket<S_GuildNameCheck>);
		_handler.Add((ushort)PacketID.S_GuildNameCheck, PacketHandler.S_GuildNameCheckHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildActionResult, MakePacket<S_GuildActionResult>);
		_handler.Add((ushort)PacketID.S_GuildActionResult, PacketHandler.S_GuildActionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildApplications, MakePacket<S_GuildApplications>);
		_handler.Add((ushort)PacketID.S_GuildApplications, PacketHandler.S_GuildApplicationsHandler);
		_makeFunc.Add((ushort)PacketID.S_UserProfile, MakePacket<S_UserProfile>);
		_handler.Add((ushort)PacketID.S_UserProfile, PacketHandler.S_UserProfileHandler);
		_makeFunc.Add((ushort)PacketID.S_Achievements, MakePacket<S_Achievements>);
		_handler.Add((ushort)PacketID.S_Achievements, PacketHandler.S_AchievementsHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimAchievementRewardResult, MakePacket<S_ClaimAchievementRewardResult>);
		_handler.Add((ushort)PacketID.S_ClaimAchievementRewardResult, PacketHandler.S_ClaimAchievementRewardResultHandler);
		_makeFunc.Add((ushort)PacketID.S_Ranking, MakePacket<S_Ranking>);
		_handler.Add((ushort)PacketID.S_Ranking, PacketHandler.S_RankingHandler);
		_makeFunc.Add((ushort)PacketID.S_FriendHome, MakePacket<S_FriendHome>);
		_handler.Add((ushort)PacketID.S_FriendHome, PacketHandler.S_FriendHomeHandler);
		_makeFunc.Add((ushort)PacketID.S_SearchFriendResult, MakePacket<S_SearchFriendResult>);
		_handler.Add((ushort)PacketID.S_SearchFriendResult, PacketHandler.S_SearchFriendResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FriendActionResult, MakePacket<S_FriendActionResult>);
		_handler.Add((ushort)PacketID.S_FriendActionResult, PacketHandler.S_FriendActionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FriendNotify, MakePacket<S_FriendNotify>);
		_handler.Add((ushort)PacketID.S_FriendNotify, PacketHandler.S_FriendNotifyHandler);
		_makeFunc.Add((ushort)PacketID.S_FriendStaminaSendResult, MakePacket<S_FriendStaminaSendResult>);
		_handler.Add((ushort)PacketID.S_FriendStaminaSendResult, PacketHandler.S_FriendStaminaSendResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FriendStaminaClaimResult, MakePacket<S_FriendStaminaClaimResult>);
		_handler.Add((ushort)PacketID.S_FriendStaminaClaimResult, PacketHandler.S_FriendStaminaClaimResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildRaidState, MakePacket<S_GuildRaidState>);
		_handler.Add((ushort)PacketID.S_GuildRaidState, PacketHandler.S_GuildRaidStateHandler);
		_makeFunc.Add((ushort)PacketID.S_EnterGuildRaidResult, MakePacket<S_EnterGuildRaidResult>);
		_handler.Add((ushort)PacketID.S_EnterGuildRaidResult, PacketHandler.S_EnterGuildRaidResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildRaidDamageResult, MakePacket<S_GuildRaidDamageResult>);
		_handler.Add((ushort)PacketID.S_GuildRaidDamageResult, PacketHandler.S_GuildRaidDamageResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildWarState, MakePacket<S_GuildWarState>);
		_handler.Add((ushort)PacketID.S_GuildWarState, PacketHandler.S_GuildWarStateHandler);
		_makeFunc.Add((ushort)PacketID.S_BeginGuildWarAttackResult, MakePacket<S_BeginGuildWarAttackResult>);
		_handler.Add((ushort)PacketID.S_BeginGuildWarAttackResult, PacketHandler.S_BeginGuildWarAttackResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildWarAttackResult, MakePacket<S_GuildWarAttackResult>);
		_handler.Add((ushort)PacketID.S_GuildWarAttackResult, PacketHandler.S_GuildWarAttackResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GuildWarLog, MakePacket<S_GuildWarLog>);
		_handler.Add((ushort)PacketID.S_GuildWarLog, PacketHandler.S_GuildWarLogHandler);
		_makeFunc.Add((ushort)PacketID.S_UserDeck, MakePacket<S_UserDeck>);
		_handler.Add((ushort)PacketID.S_UserDeck, PacketHandler.S_UserDeckHandler);
		_makeFunc.Add((ushort)PacketID.S_RedeemCouponResult, MakePacket<S_RedeemCouponResult>);
		_handler.Add((ushort)PacketID.S_RedeemCouponResult, PacketHandler.S_RedeemCouponResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ArenaHome, MakePacket<S_ArenaHome>);
		_handler.Add((ushort)PacketID.S_ArenaHome, PacketHandler.S_ArenaHomeHandler);
		_makeFunc.Add((ushort)PacketID.S_ArenaSetRunesResult, MakePacket<S_ArenaSetRunesResult>);
		_handler.Add((ushort)PacketID.S_ArenaSetRunesResult, PacketHandler.S_ArenaSetRunesResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ArenaHistory, MakePacket<S_ArenaHistory>);
		_handler.Add((ushort)PacketID.S_ArenaHistory, PacketHandler.S_ArenaHistoryHandler);
		_makeFunc.Add((ushort)PacketID.S_ArenaMatchResult, MakePacket<S_ArenaMatchResult>);
		_handler.Add((ushort)PacketID.S_ArenaMatchResult, PacketHandler.S_ArenaMatchResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ArenaReportResult, MakePacket<S_ArenaReportResult>);
		_handler.Add((ushort)PacketID.S_ArenaReportResult, PacketHandler.S_ArenaReportResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ArenaLog, MakePacket<S_ArenaLog>);
		_handler.Add((ushort)PacketID.S_ArenaLog, PacketHandler.S_ArenaLogHandler);
		_makeFunc.Add((ushort)PacketID.S_TrainingInfo, MakePacket<S_TrainingInfo>);
		_handler.Add((ushort)PacketID.S_TrainingInfo, PacketHandler.S_TrainingInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_TrainingActionResult, MakePacket<S_TrainingActionResult>);
		_handler.Add((ushort)PacketID.S_TrainingActionResult, PacketHandler.S_TrainingActionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DevTool, MakePacket<S_DevTool>);
		_handler.Add((ushort)PacketID.S_DevTool, PacketHandler.S_DevToolHandler);
		_makeFunc.Add((ushort)PacketID.S_OfflineReward, MakePacket<S_OfflineReward>);
		_handler.Add((ushort)PacketID.S_OfflineReward, PacketHandler.S_OfflineRewardHandler);
		_makeFunc.Add((ushort)PacketID.S_DiamondDetail, MakePacket<S_DiamondDetail>);
		_handler.Add((ushort)PacketID.S_DiamondDetail, PacketHandler.S_DiamondDetailHandler);
		_makeFunc.Add((ushort)PacketID.S_PushTokenResult, MakePacket<S_PushTokenResult>);
		_handler.Add((ushort)PacketID.S_PushTokenResult, PacketHandler.S_PushTokenResultHandler);
		_makeFunc.Add((ushort)PacketID.S_StoryProgress, MakePacket<S_StoryProgress>);
		_handler.Add((ushort)PacketID.S_StoryProgress, PacketHandler.S_StoryProgressHandler);
		_makeFunc.Add((ushort)PacketID.S_CompleteStoryResult, MakePacket<S_CompleteStoryResult>);
		_handler.Add((ushort)PacketID.S_CompleteStoryResult, PacketHandler.S_CompleteStoryResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimStoryRewardResult, MakePacket<S_ClaimStoryRewardResult>);
		_handler.Add((ushort)PacketID.S_ClaimStoryRewardResult, PacketHandler.S_ClaimStoryRewardResultHandler);
		_makeFunc.Add((ushort)PacketID.S_Investment, MakePacket<S_Investment>);
		_handler.Add((ushort)PacketID.S_Investment, PacketHandler.S_InvestmentHandler);
		_makeFunc.Add((ushort)PacketID.S_UpgradeInvestmentResult, MakePacket<S_UpgradeInvestmentResult>);
		_handler.Add((ushort)PacketID.S_UpgradeInvestmentResult, PacketHandler.S_UpgradeInvestmentResultHandler);
		_makeFunc.Add((ushort)PacketID.S_MainQuest, MakePacket<S_MainQuest>);
		_handler.Add((ushort)PacketID.S_MainQuest, PacketHandler.S_MainQuestHandler);
		_makeFunc.Add((ushort)PacketID.S_MainQuestNotify, MakePacket<S_MainQuestNotify>);
		_handler.Add((ushort)PacketID.S_MainQuestNotify, PacketHandler.S_MainQuestNotifyHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimMainQuestResult, MakePacket<S_ClaimMainQuestResult>);
		_handler.Add((ushort)PacketID.S_ClaimMainQuestResult, PacketHandler.S_ClaimMainQuestResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DispatchInfo, MakePacket<S_DispatchInfo>);
		_handler.Add((ushort)PacketID.S_DispatchInfo, PacketHandler.S_DispatchInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_DispatchActionResult, MakePacket<S_DispatchActionResult>);
		_handler.Add((ushort)PacketID.S_DispatchActionResult, PacketHandler.S_DispatchActionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DispatchNotify, MakePacket<S_DispatchNotify>);
		_handler.Add((ushort)PacketID.S_DispatchNotify, PacketHandler.S_DispatchNotifyHandler);
		_makeFunc.Add((ushort)PacketID.S_SocialSettings, MakePacket<S_SocialSettings>);
		_handler.Add((ushort)PacketID.S_SocialSettings, PacketHandler.S_SocialSettingsHandler);
		_makeFunc.Add((ushort)PacketID.S_DoomInfo, MakePacket<S_DoomInfo>);
		_handler.Add((ushort)PacketID.S_DoomInfo, PacketHandler.S_DoomInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_DoomResult, MakePacket<S_DoomResult>);
		_handler.Add((ushort)PacketID.S_DoomResult, PacketHandler.S_DoomResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DoomClaimResult, MakePacket<S_DoomClaimResult>);
		_handler.Add((ushort)PacketID.S_DoomClaimResult, PacketHandler.S_DoomClaimResultHandler);
		_makeFunc.Add((ushort)PacketID.S_PassInfo, MakePacket<S_PassInfo>);
		_handler.Add((ushort)PacketID.S_PassInfo, PacketHandler.S_PassInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_PassClaimResult, MakePacket<S_PassClaimResult>);
		_handler.Add((ushort)PacketID.S_PassClaimResult, PacketHandler.S_PassClaimResultHandler);
		_makeFunc.Add((ushort)PacketID.S_EventRouletteInfo, MakePacket<S_EventRouletteInfo>);
		_handler.Add((ushort)PacketID.S_EventRouletteInfo, PacketHandler.S_EventRouletteInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_EventRouletteSpinResult, MakePacket<S_EventRouletteSpinResult>);
		_handler.Add((ushort)PacketID.S_EventRouletteSpinResult, PacketHandler.S_EventRouletteSpinResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ExpeditionHome, MakePacket<S_ExpeditionHome>);
		_handler.Add((ushort)PacketID.S_ExpeditionHome, PacketHandler.S_ExpeditionHomeHandler);
		_makeFunc.Add((ushort)PacketID.S_EnterExpeditionResult, MakePacket<S_EnterExpeditionResult>);
		_handler.Add((ushort)PacketID.S_EnterExpeditionResult, PacketHandler.S_EnterExpeditionResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ExpeditionDamageResult, MakePacket<S_ExpeditionDamageResult>);
		_handler.Add((ushort)PacketID.S_ExpeditionDamageResult, PacketHandler.S_ExpeditionDamageResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ExpeditionLog, MakePacket<S_ExpeditionLog>);
		_handler.Add((ushort)PacketID.S_ExpeditionLog, PacketHandler.S_ExpeditionLogHandler);
		_makeFunc.Add((ushort)PacketID.S_DoomEntryInfo, MakePacket<S_DoomEntryInfo>);
		_handler.Add((ushort)PacketID.S_DoomEntryInfo, PacketHandler.S_DoomEntryInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_DoomEnterResult, MakePacket<S_DoomEnterResult>);
		_handler.Add((ushort)PacketID.S_DoomEnterResult, PacketHandler.S_DoomEnterResultHandler);
		_makeFunc.Add((ushort)PacketID.S_DoomFinishResult, MakePacket<S_DoomFinishResult>);
		_handler.Add((ushort)PacketID.S_DoomFinishResult, PacketHandler.S_DoomFinishResultHandler);
		_makeFunc.Add((ushort)PacketID.S_TowerInfo, MakePacket<S_TowerInfo>);
		_handler.Add((ushort)PacketID.S_TowerInfo, PacketHandler.S_TowerInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_TowerEnterResult, MakePacket<S_TowerEnterResult>);
		_handler.Add((ushort)PacketID.S_TowerEnterResult, PacketHandler.S_TowerEnterResultHandler);
		_makeFunc.Add((ushort)PacketID.S_TowerClearResult, MakePacket<S_TowerClearResult>);
		_handler.Add((ushort)PacketID.S_TowerClearResult, PacketHandler.S_TowerClearResultHandler);
		_makeFunc.Add((ushort)PacketID.S_AttendanceEventInfo, MakePacket<S_AttendanceEventInfo>);
		_handler.Add((ushort)PacketID.S_AttendanceEventInfo, PacketHandler.S_AttendanceEventInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimAttendanceEventResult, MakePacket<S_ClaimAttendanceEventResult>);
		_handler.Add((ushort)PacketID.S_ClaimAttendanceEventResult, PacketHandler.S_ClaimAttendanceEventResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FrontlineInfo, MakePacket<S_FrontlineInfo>);
		_handler.Add((ushort)PacketID.S_FrontlineInfo, PacketHandler.S_FrontlineInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_FrontlineSetMembersResult, MakePacket<S_FrontlineSetMembersResult>);
		_handler.Add((ushort)PacketID.S_FrontlineSetMembersResult, PacketHandler.S_FrontlineSetMembersResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FrontlineResetResult, MakePacket<S_FrontlineResetResult>);
		_handler.Add((ushort)PacketID.S_FrontlineResetResult, PacketHandler.S_FrontlineResetResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FrontlineEnterResult, MakePacket<S_FrontlineEnterResult>);
		_handler.Add((ushort)PacketID.S_FrontlineEnterResult, PacketHandler.S_FrontlineEnterResultHandler);
		_makeFunc.Add((ushort)PacketID.S_FrontlineFinishResult, MakePacket<S_FrontlineFinishResult>);
		_handler.Add((ushort)PacketID.S_FrontlineFinishResult, PacketHandler.S_FrontlineFinishResultHandler);
		_makeFunc.Add((ushort)PacketID.S_HeroesInfo, MakePacket<S_HeroesInfo>);
		_handler.Add((ushort)PacketID.S_HeroesInfo, PacketHandler.S_HeroesInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_HeroesEnterResult, MakePacket<S_HeroesEnterResult>);
		_handler.Add((ushort)PacketID.S_HeroesEnterResult, PacketHandler.S_HeroesEnterResultHandler);
		_makeFunc.Add((ushort)PacketID.S_HeroesClearResult, MakePacket<S_HeroesClearResult>);
		_handler.Add((ushort)PacketID.S_HeroesClearResult, PacketHandler.S_HeroesClearResultHandler);

	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
		if (_makeFunc.TryGetValue(id, out func))
		{
			IPacket packet = func.Invoke(session, buffer);
			if (onRecvCallback != null)
				onRecvCallback.Invoke(session, packet);
			else
				HandlePacket(session, packet);
		}
	}

	T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
	{
		T pkt = new T();
		pkt.Read(buffer);
		return pkt;
	}

	public void HandlePacket(PacketSession session, IPacket packet)
	{
		Action<PacketSession, IPacket> action = null;
		if (_handler.TryGetValue(packet.Protocol, out action))
			action.Invoke(session, packet);
	}
}