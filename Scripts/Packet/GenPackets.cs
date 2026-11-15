using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

public enum PacketID
{
	C_Ping = 1,
	S_Pong = 2,
	C_Version = 3,
	S_Version = 4,
	C_Login = 5,
	S_Login = 6,
	C_TermsInfo = 7,
	S_TermsInfo = 8,
	C_WithdrawAccount = 9,
	S_WithdrawAccountResult = 10,
	C_AgreeTerms = 11,
	S_AgreeTermsResult = 12,
	C_EventStatus = 13,
	S_EventStatus = 14,
	C_AttendanceInfo = 15,
	S_AttendanceInfo = 16,
	C_ClaimAttendance = 17,
	S_ClaimAttendanceResult = 18,
	C_EnterMain = 19,
	S_MainData = 20,
	C_StartDungeon = 21,
	S_StartDungeonResult = 22,
	C_ClearDungeon = 23,
	S_DungeonReward = 24,
	C_SweepStage = 25,
	S_SweepStageResult = 26,
	C_DungeonRewardPreview = 27,
	S_DungeonRewardPreview = 28,
	C_ChangeDeck = 29,
	S_ChangeDeckResult = 30,
	C_AllocateStat = 31,
	S_AllocateStatResult = 32,
	C_ResetStats = 33,
	S_ResetStatsResult = 34,
	C_SetAutoStat = 35,
	S_SetAutoStatResult = 36,
	C_UpgradeStar = 37,
	S_UpgradeStarResult = 38,
	C_LimitBreak = 39,
	S_LimitBreakResult = 40,
	C_RequestShop = 41,
	S_ShopCatalog = 42,
	S_ShopOfferPage = 43,
	C_BuyOffer = 44,
	C_PurchaseGate = 45,
	C_CanBuyOffer = 46,
	S_CanBuyOffer = 47,
	C_GachaMileagePick = 48,
	S_GachaMileagePickResult = 49,
	C_GachaRates = 50,
	S_GachaRates = 51,
	C_RotationInfo = 52,
	S_RotationInfo = 53,
	S_BuyResult = 54,
	C_RequestInventory = 55,
	S_Inventory = 56,
	C_EquipItem = 57,
	S_EquipResult = 58,
	C_UnequipItem = 59,
	S_UnequipResult = 60,
	C_AwakenEquip = 61,
	S_AwakenResult = 62,
	C_EquipLimitBreak = 63,
	S_EquipLimitBreakResult = 64,
	C_DisassembleEquip = 65,
	C_DisassembleEquipByGrade = 66,
	S_DisassembleEquipResult = 67,
	C_ConvertUniversalShard = 68,
	S_ConvertUniversalShardResult = 69,
	C_RequestMail = 70,
	S_MailList = 71,
	C_ClaimMail = 72,
	S_ClaimMailResult = 73,
	S_MailNotify = 74,
	C_Profile = 75,
	S_Profile = 76,
	C_EquipProfileFrame = 77,
	S_EquipProfileFrameResult = 78,
	C_CheckNickname = 79,
	S_CheckNicknameResult = 80,
	C_ChangeNickname = 81,
	S_ChangeNicknameResult = 82,
	C_EncyclopediaRewards = 83,
	S_EncyclopediaRewards = 84,
	C_ClaimEncyclopediaReward = 85,
	S_ClaimEncyclopediaRewardResult = 86,
	C_ChargeRewards = 87,
	S_ChargeRewards = 88,
	C_ClaimChargeReward = 89,
	S_ClaimChargeRewardResult = 90,
	C_EventMissions = 91,
	S_EventMissions = 92,
	C_Heartbeat = 93,
	S_HeartbeatResult = 94,
	C_ClaimEventMission = 95,
	S_ClaimEventMissionResult = 96,
	C_LevelUpEquip = 97,
	S_LevelUpEquipResult = 98,
	C_CommissionInfo = 99,
	S_CommissionInfo = 100,
	C_StartCommission = 101,
	S_StartCommissionResult = 102,
	C_ClearCommission = 103,
	S_ClearCommissionResult = 104,
	C_SweepCommission = 105,
	S_SweepCommissionResult = 106,
	C_EngravingInfo = 107,
	S_EngravingInfo = 108,
	C_UpgradeEngraving = 109,
	S_UpgradeEngravingResult = 110,
	C_EquipEngraving = 111,
	S_EquipEngravingResult = 112,
	C_CostumeInfo = 113,
	S_CostumeInfo = 114,
	C_EquipUnitSkin = 115,
	S_EquipUnitSkinResult = 116,
	C_EquipCostume = 117,
	S_EquipCostumeResult = 118,
	C_FlashSaleInfo = 119,
	S_FlashSaleInfo = 120,
	S_FlashSaleNotify = 121,
	C_ChatSend = 122,
	S_ChatSendResult = 123,
	S_ChatMessage = 124,
	C_DeleteWhisper = 125,
	S_DeleteWhisperResult = 126,
	C_ReportChat = 127,
	S_ReportChatResult = 128,
	C_ChatHistory = 129,
	S_ChatHistory = 130,
	C_HeadquartersInfo = 131,
	S_HeadquartersInfo = 132,
	C_UpgradeHqNode = 133,
	S_UpgradeHqNodeResult = 134,
	C_ItemInfo = 135,
	S_ItemInfo = 136,
	C_UseItem = 137,
	S_UseItemResult = 138,
	C_ItemUsePreview = 139,
	S_ItemUsePreview = 140,
	C_DeckPresets = 141,
	S_DeckPresets = 142,
	C_SaveDeckPreset = 143,
	C_ApplyDeckPreset = 144,
	C_RenameDeckPreset = 145,
	S_DeckPresetResult = 146,
	C_GuildHome = 147,
	S_GuildHome = 148,
	C_GuildList = 149,
	S_GuildList = 150,
	C_GuildNameCheck = 151,
	S_GuildNameCheck = 152,
	C_CreateGuild = 153,
	C_GuildAction = 154,
	S_GuildActionResult = 155,
	C_GuildApplications = 156,
	S_GuildApplications = 157,
	C_ReportCombatPower = 158,
	C_UserProfile = 159,
	S_UserProfile = 160,
	C_Achievements = 161,
	S_Achievements = 162,
	C_ClaimAchievementReward = 163,
	S_ClaimAchievementRewardResult = 164,
	C_Ranking = 165,
	S_Ranking = 166,
	C_FriendHome = 167,
	S_FriendHome = 168,
	C_SearchFriend = 169,
	S_SearchFriendResult = 170,
	C_FriendAction = 171,
	S_FriendActionResult = 172,
	S_FriendNotify = 173,
	C_FriendStaminaSend = 174,
	S_FriendStaminaSendResult = 175,
	C_FriendStaminaClaim = 176,
	S_FriendStaminaClaimResult = 177,
	C_GuildRaidState = 178,
	S_GuildRaidState = 179,
	C_EnterGuildRaid = 180,
	S_EnterGuildRaidResult = 181,
	C_ReportGuildRaidDamage = 182,
	S_GuildRaidDamageResult = 183,
	C_GuildWarState = 184,
	S_GuildWarState = 185,
	C_BeginGuildWarAttack = 186,
	S_BeginGuildWarAttackResult = 187,
	C_ReportGuildWarAttack = 188,
	S_GuildWarAttackResult = 189,
	C_GuildWarLog = 190,
	S_GuildWarLog = 191,
	C_UserDeck = 192,
	S_UserDeck = 193,
	C_RedeemCoupon = 194,
	S_RedeemCouponResult = 195,
	C_ArenaHome = 196,
	S_ArenaHome = 197,
	C_ArenaSetRunes = 198,
	S_ArenaSetRunesResult = 199,
	C_ArenaHistory = 200,
	S_ArenaHistory = 201,
	C_ArenaMatch = 202,
	S_ArenaMatchResult = 203,
	C_ArenaReport = 204,
	S_ArenaReportResult = 205,
	C_ArenaLog = 206,
	S_ArenaLog = 207,
	C_TrainingInfo = 208,
	S_TrainingInfo = 209,
	C_TrainingAction = 210,
	S_TrainingActionResult = 211,
	C_DevTool = 212,
	S_DevTool = 213,
	C_OfflineReward = 214,
	S_OfflineReward = 215,
	C_DiamondDetail = 216,
	S_DiamondDetail = 217,
	C_RegisterPushToken = 218,
	C_UnregisterPushToken = 219,
	S_PushTokenResult = 220,
	C_CommissionAttempt = 221,
	C_StoryProgress = 222,
	S_StoryProgress = 223,
	C_CompleteStory = 224,
	S_CompleteStoryResult = 225,
	C_ClaimStoryReward = 226,
	S_ClaimStoryRewardResult = 227,
	C_Investment = 228,
	S_Investment = 229,
	C_UpgradeInvestment = 230,
	S_UpgradeInvestmentResult = 231,
	C_MainQuest = 232,
	C_MainQuestAction = 233,
	S_MainQuest = 234,
	S_MainQuestNotify = 235,
	C_ClaimMainQuest = 236,
	S_ClaimMainQuestResult = 237,
	C_DispatchInfo = 238,
	S_DispatchInfo = 239,
	C_DispatchAction = 240,
	S_DispatchActionResult = 241,
	S_DispatchNotify = 242,
	C_SocialSettings = 243,
	S_SocialSettings = 244,
	C_Test = 245,
	C_DoomInfo = 246,
	S_DoomInfo = 247,
	C_DoomResult = 248,
	S_DoomResult = 249,
	C_DoomClaim = 250,
	S_DoomClaimResult = 251,
	C_BattleFinished = 252,
	C_PassInfo = 253,
	S_PassInfo = 254,
	C_PassClaim = 255,
	S_PassClaimResult = 256,
	C_EventRouletteInfo = 257,
	S_EventRouletteInfo = 258,
	C_EventRouletteSpin = 259,
	S_EventRouletteSpinResult = 260,
	C_ReportCharacterPower = 261,
	C_ExpeditionHome = 262,
	S_ExpeditionHome = 263,
	C_EnterExpedition = 264,
	S_EnterExpeditionResult = 265,
	C_ExpeditionMemberDeck = 266,
	C_ReportExpeditionDamage = 267,
	S_ExpeditionDamageResult = 268,
	C_ExpeditionLog = 269,
	S_ExpeditionLog = 270,
	C_DoomEntryInfo = 271,
	S_DoomEntryInfo = 272,
	C_DoomEnter = 273,
	S_DoomEnterResult = 274,
	C_DoomFinish = 275,
	S_DoomFinishResult = 276,
	C_TowerInfo = 277,
	S_TowerInfo = 278,
	C_TowerEnter = 279,
	S_TowerEnterResult = 280,
	C_TowerClear = 281,
	S_TowerClearResult = 282,
	C_AttendanceEventInfo = 283,
	S_AttendanceEventInfo = 284,
	C_ClaimAttendanceEvent = 285,
	S_ClaimAttendanceEventResult = 286,
	C_FrontlineInfo = 287,
	S_FrontlineInfo = 288,
	C_FrontlineSetMembers = 289,
	S_FrontlineSetMembersResult = 290,
	C_FrontlineReset = 291,
	S_FrontlineResetResult = 292,
	C_FrontlineEnter = 293,
	S_FrontlineEnterResult = 294,
	C_FrontlineFinish = 295,
	S_FrontlineFinishResult = 296,
	C_HeroesInfo = 297,
	S_HeroesInfo = 298,
	C_HeroesEnter = 299,
	S_HeroesEnterResult = 300,
	C_HeroesClear = 301,
	S_HeroesClearResult = 302,
	
}

public interface IPacket
{
	ushort Protocol { get; }
	void Read(ArraySegment<byte> segment);
	ArraySegment<byte> Write();
}


public class C_Ping : IPacket
{
	public int seq;
	public int clientSentTick;

	public ushort Protocol { get { return (ushort)PacketID.C_Ping; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.seq = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.clientSentTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Ping), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.seq), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.clientSentTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Pong : IPacket
{
	public int seq;
	public int serverTick;

	public ushort Protocol { get { return (ushort)PacketID.S_Pong; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.seq = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Pong), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.seq), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Version : IPacket
{
	public int platform;
	public string version;

	public ushort Protocol { get { return (ushort)PacketID.C_Version; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.platform = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort versionLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.version = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, versionLen);
		count += versionLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Version), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.platform), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string versionValue = this.version ?? string.Empty;
		ushort versionLen = (ushort)Encoding.Unicode.GetBytes(versionValue, 0, versionValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(versionLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += versionLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Version : IPacket
{
	public bool needUpdate;
	public bool maintenance;

	public ushort Protocol { get { return (ushort)PacketID.S_Version; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.needUpdate = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.maintenance = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Version), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.needUpdate), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.maintenance), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Login : IPacket
{
	public int provider;
	public string authToken;
	public string sessionToken;

	public ushort Protocol { get { return (ushort)PacketID.C_Login; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.provider = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort authTokenLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.authToken = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, authTokenLen);
		count += authTokenLen;
		ushort sessionTokenLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.sessionToken = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, sessionTokenLen);
		count += sessionTokenLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Login), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.provider), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string authTokenValue = this.authToken ?? string.Empty;
		ushort authTokenLen = (ushort)Encoding.Unicode.GetBytes(authTokenValue, 0, authTokenValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(authTokenLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += authTokenLen;
		string sessionTokenValue = this.sessionToken ?? string.Empty;
		ushort sessionTokenLen = (ushort)Encoding.Unicode.GetBytes(sessionTokenValue, 0, sessionTokenValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(sessionTokenLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += sessionTokenLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Login : IPacket
{
	public int resultCode;
	public long userId;
	public string playerGuid;
	public string sessionToken;
	public bool isNewUser;
	public string nickname;

	public ushort Protocol { get { return (ushort)PacketID.S_Login; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.resultCode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.userId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		ushort playerGuidLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.playerGuid = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, playerGuidLen);
		count += playerGuidLen;
		ushort sessionTokenLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.sessionToken = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, sessionTokenLen);
		count += sessionTokenLen;
		this.isNewUser = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Login), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.resultCode), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.userId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		string playerGuidValue = this.playerGuid ?? string.Empty;
		ushort playerGuidLen = (ushort)Encoding.Unicode.GetBytes(playerGuidValue, 0, playerGuidValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(playerGuidLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += playerGuidLen;
		string sessionTokenValue = this.sessionToken ?? string.Empty;
		ushort sessionTokenLen = (ushort)Encoding.Unicode.GetBytes(sessionTokenValue, 0, sessionTokenValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(sessionTokenLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += sessionTokenLen;
		Array.Copy(BitConverter.GetBytes(this.isNewUser), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TermsInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_TermsInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TermsInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_TermsInfo : IPacket
{
	public bool needAgreement;
	public class Term
	{
		public string termsKey;
		public int version;
		public bool isRequired;
		public string title;
		public string prerequisiteKey;
		public bool isAgreed;
		public int agreedVersion;
		public string url;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort termsKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.termsKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, termsKeyLen);
			count += termsKeyLen;
			this.version = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.isRequired = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			ushort titleLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.title = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, titleLen);
			count += titleLen;
			ushort prerequisiteKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.prerequisiteKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, prerequisiteKeyLen);
			count += prerequisiteKeyLen;
			this.isAgreed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.agreedVersion = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort urlLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.url = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, urlLen);
			count += urlLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string termsKeyValue = this.termsKey ?? string.Empty;
			ushort termsKeyLen = (ushort)Encoding.Unicode.GetBytes(termsKeyValue, 0, termsKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(termsKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += termsKeyLen;
			Array.Copy(BitConverter.GetBytes(this.version), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.isRequired), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			string titleValue = this.title ?? string.Empty;
			ushort titleLen = (ushort)Encoding.Unicode.GetBytes(titleValue, 0, titleValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(titleLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += titleLen;
			string prerequisiteKeyValue = this.prerequisiteKey ?? string.Empty;
			ushort prerequisiteKeyLen = (ushort)Encoding.Unicode.GetBytes(prerequisiteKeyValue, 0, prerequisiteKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(prerequisiteKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += prerequisiteKeyLen;
			Array.Copy(BitConverter.GetBytes(this.isAgreed), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.agreedVersion), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string urlValue = this.url ?? string.Empty;
			ushort urlLen = (ushort)Encoding.Unicode.GetBytes(urlValue, 0, urlValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(urlLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += urlLen;
			return success;
		}	
	}
	public List<Term> terms = new List<Term>();
	public class Document
	{
		public string documentKey;
		public int version;
		public string title;
		public string url;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort documentKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.documentKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, documentKeyLen);
			count += documentKeyLen;
			this.version = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort titleLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.title = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, titleLen);
			count += titleLen;
			ushort urlLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.url = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, urlLen);
			count += urlLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string documentKeyValue = this.documentKey ?? string.Empty;
			ushort documentKeyLen = (ushort)Encoding.Unicode.GetBytes(documentKeyValue, 0, documentKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(documentKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += documentKeyLen;
			Array.Copy(BitConverter.GetBytes(this.version), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string titleValue = this.title ?? string.Empty;
			ushort titleLen = (ushort)Encoding.Unicode.GetBytes(titleValue, 0, titleValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(titleLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += titleLen;
			string urlValue = this.url ?? string.Empty;
			ushort urlLen = (ushort)Encoding.Unicode.GetBytes(urlValue, 0, urlValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(urlLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += urlLen;
			return success;
		}	
	}
	public List<Document> documents = new List<Document>();

	public ushort Protocol { get { return (ushort)PacketID.S_TermsInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.needAgreement = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.terms.Clear();
		ushort termLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < termLen; i++)
		{
			Term term = new Term();
			term.Read(segment, ref count);
			terms.Add(term);
		}
		this.documents.Clear();
		ushort documentLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < documentLen; i++)
		{
			Document document = new Document();
			document.Read(segment, ref count);
			documents.Add(document);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_TermsInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.needAgreement), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.terms.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Term term in this.terms)
			term.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.documents.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Document document in this.documents)
			document.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_WithdrawAccount : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_WithdrawAccount; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_WithdrawAccount), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_WithdrawAccountResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_WithdrawAccountResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_WithdrawAccountResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AgreeTerms : IPacket
{
	public class Agreement
	{
		public string termsKey;
		public int version;
		public bool isAgreed;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort termsKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.termsKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, termsKeyLen);
			count += termsKeyLen;
			this.version = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.isAgreed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string termsKeyValue = this.termsKey ?? string.Empty;
			ushort termsKeyLen = (ushort)Encoding.Unicode.GetBytes(termsKeyValue, 0, termsKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(termsKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += termsKeyLen;
			Array.Copy(BitConverter.GetBytes(this.version), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.isAgreed), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Agreement> agreements = new List<Agreement>();

	public ushort Protocol { get { return (ushort)PacketID.C_AgreeTerms; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.agreements.Clear();
		ushort agreementLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < agreementLen; i++)
		{
			Agreement agreement = new Agreement();
			agreement.Read(segment, ref count);
			agreements.Add(agreement);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AgreeTerms), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.agreements.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Agreement agreement in this.agreements)
			agreement.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AgreeTermsResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public bool needAgreement;

	public ushort Protocol { get { return (ushort)PacketID.S_AgreeTermsResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.needAgreement = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AgreeTermsResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.needAgreement), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EventStatus : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EventStatus; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EventStatus), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EventStatus : IPacket
{
	public class Statuses
	{
		public string rewardKey;
		public int state;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort rewardKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.rewardKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, rewardKeyLen);
			count += rewardKeyLen;
			this.state = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string rewardKeyValue = this.rewardKey ?? string.Empty;
			ushort rewardKeyLen = (ushort)Encoding.Unicode.GetBytes(rewardKeyValue, 0, rewardKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(rewardKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += rewardKeyLen;
			Array.Copy(BitConverter.GetBytes(this.state), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Statuses> statusess = new List<Statuses>();

	public ushort Protocol { get { return (ushort)PacketID.S_EventStatus; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.statusess.Clear();
		ushort statusesLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < statusesLen; i++)
		{
			Statuses statuses = new Statuses();
			statuses.Read(segment, ref count);
			statusess.Add(statuses);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EventStatus), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.statusess.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Statuses statuses in this.statusess)
			statuses.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AttendanceInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_AttendanceInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AttendanceInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AttendanceInfo : IPacket
{
	public int claimedDays;
	public bool canClaimToday;
	public int maxDays;
	public class DayReward
	{
		public int day;
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.day = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.day), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<DayReward> dayRewards = new List<DayReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_AttendanceInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.claimedDays = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClaimToday = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.maxDays = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dayRewards.Clear();
		ushort dayRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < dayRewardLen; i++)
		{
			DayReward dayReward = new DayReward();
			dayReward.Read(segment, ref count);
			dayRewards.Add(dayReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AttendanceInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.claimedDays), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClaimToday), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.maxDays), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.dayRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (DayReward dayReward in this.dayRewards)
			dayReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimAttendance : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimAttendance; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimAttendance), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimAttendanceResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int claimedDays;
	public bool canClaimToday;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimAttendanceResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedDays = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClaimToday = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimAttendanceResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedDays), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClaimToday), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EnterMain : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EnterMain; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EnterMain), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MainData : IPacket
{
	public string nickname;
	public int gold;
	public int diamond;
	public int stamina;
	public int staminaMax;
	public int staminaBoostTicket;
	public int staminaBoostTicketDaily;
	public int currentDungeonIndex;
	public int highestClearedDungeonIndex;
	public int representativeCharId;
	public int equippedProfileFrameId;
	public class Character
	{
		public int charDataId;
		public int level;
		public int exp;
		public int maxExp;
		public int starLevel;
		public int statPoints;
		public int shards;
		public int invAttack;
		public int invDefense;
		public int invHealth;
		public int invSkillAccel;
		public int invAttackSpeed;
		public int invMoveSpeed;
		public int autoStatType;
		public bool limitBroken;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.statPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.statPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Character> characters = new List<Character>();
	public class Deck
	{
		public int charDataId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Deck> decks = new List<Deck>();

	public ushort Protocol { get { return (ushort)PacketID.S_MainData; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.gold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.diamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaMax = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaBoostTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaBoostTicketDaily = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.currentDungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.highestClearedDungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.equippedProfileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.characters.Clear();
		ushort characterLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < characterLen; i++)
		{
			Character character = new Character();
			character.Read(segment, ref count);
			characters.Add(character);
		}
		this.decks.Clear();
		ushort deckLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < deckLen; i++)
		{
			Deck deck = new Deck();
			deck.Read(segment, ref count);
			decks.Add(deck);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MainData), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.gold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.diamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaMax), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaBoostTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaBoostTicketDaily), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.currentDungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.highestClearedDungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.equippedProfileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.characters.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Character character in this.characters)
			character.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.decks.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Deck deck in this.decks)
			deck.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_StartDungeon : IPacket
{
	public int dungeonIndex;

	public ushort Protocol { get { return (ushort)PacketID.C_StartDungeon; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.dungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_StartDungeon), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.dungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_StartDungeonResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int remainingStamina;

	public ushort Protocol { get { return (ushort)PacketID.S_StartDungeonResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.remainingStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_StartDungeonResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.remainingStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClearDungeon : IPacket
{
	public int dungeonIndex;
	public bool boost;

	public ushort Protocol { get { return (ushort)PacketID.C_ClearDungeon; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.dungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.boost = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClearDungeon), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.dungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.boost), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DungeonReward : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int clearCount;
	public int gainedGold;
	public int newGold;
	public int newStamina;
	public int newBoostTicket;
	public bool staminaShort;
	public int highestClearedDungeonIndex;
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();
	public class Drop
	{
		public int equipDataId;
		public int qty;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.equipDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.equipDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Drop> drops = new List<Drop>();

	public ushort Protocol { get { return (ushort)PacketID.S_DungeonReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.clearCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newBoostTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaShort = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.highestClearedDungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
		this.drops.Clear();
		ushort dropLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < dropLen; i++)
		{
			Drop drop = new Drop();
			drop.Read(segment, ref count);
			drops.Add(drop);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DungeonReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.clearCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newBoostTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaShort), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.highestClearedDungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.drops.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Drop drop in this.drops)
			drop.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SweepStage : IPacket
{
	public int count;

	public ushort Protocol { get { return (ushort)PacketID.C_SweepStage; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SweepStage), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SweepStageResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int dungeonIndex;
	public int count;
	public int gainedGold;
	public int newGold;
	public int newTicketQty;
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_SweepStageResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newTicketQty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SweepStageResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newTicketQty), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DungeonRewardPreview : IPacket
{
	public class Dungeon
	{
		public int dungeonIndex;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.dungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.dungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Dungeon> dungeons = new List<Dungeon>();

	public ushort Protocol { get { return (ushort)PacketID.C_DungeonRewardPreview; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.dungeons.Clear();
		ushort dungeonLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < dungeonLen; i++)
		{
			Dungeon dungeon = new Dungeon();
			dungeon.Read(segment, ref count);
			dungeons.Add(dungeon);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DungeonRewardPreview), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.dungeons.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Dungeon dungeon in this.dungeons)
			dungeon.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DungeonRewardPreview : IPacket
{
	public class Preview
	{
		public int dungeonIndex;
		public int gold;
		public int expPerCharacter;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.dungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.expPerCharacter = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.dungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gold), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.expPerCharacter), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Preview> previews = new List<Preview>();

	public ushort Protocol { get { return (ushort)PacketID.S_DungeonRewardPreview; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.previews.Clear();
		ushort previewLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < previewLen; i++)
		{
			Preview preview = new Preview();
			preview.Read(segment, ref count);
			previews.Add(preview);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DungeonRewardPreview), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.previews.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Preview preview in this.previews)
			preview.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ChangeDeck : IPacket
{
	public int representativeCharId;
	public bool immediate;
	public class DeckChar
	{
		public int charDataId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<DeckChar> deckChars = new List<DeckChar>();

	public ushort Protocol { get { return (ushort)PacketID.C_ChangeDeck; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.immediate = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.deckChars.Clear();
		ushort deckCharLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < deckCharLen; i++)
		{
			DeckChar deckChar = new DeckChar();
			deckChar.Read(segment, ref count);
			deckChars.Add(deckChar);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ChangeDeck), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.immediate), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.deckChars.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (DeckChar deckChar in this.deckChars)
			deckChar.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ChangeDeckResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_ChangeDeckResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ChangeDeckResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AllocateStat : IPacket
{
	public int charDataId;
	public int statType;
	public int count;

	public ushort Protocol { get { return (ushort)PacketID.C_AllocateStat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AllocateStat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AllocateStatResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int level;
	public int exp;
	public int maxExp;
	public int starLevel;
	public int statPoints;
	public int shards;
	public int invAttack;
	public int invDefense;
	public int invHealth;
	public int invSkillAccel;
	public int invAttackSpeed;
	public int invMoveSpeed;
	public int autoStatType;
	public bool limitBroken;

	public ushort Protocol { get { return (ushort)PacketID.S_AllocateStatResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AllocateStatResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ResetStats : IPacket
{
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_ResetStats; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ResetStats), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ResetStatsResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int newDiamond;
	public int level;
	public int exp;
	public int maxExp;
	public int starLevel;
	public int statPoints;
	public int shards;
	public int invAttack;
	public int invDefense;
	public int invHealth;
	public int invSkillAccel;
	public int invAttackSpeed;
	public int invMoveSpeed;
	public int autoStatType;
	public bool limitBroken;

	public ushort Protocol { get { return (ushort)PacketID.S_ResetStatsResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ResetStatsResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SetAutoStat : IPacket
{
	public int charDataId;
	public int statType;

	public ushort Protocol { get { return (ushort)PacketID.C_SetAutoStat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SetAutoStat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SetAutoStatResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int autoStatType;

	public ushort Protocol { get { return (ushort)PacketID.S_SetAutoStatResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SetAutoStatResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UpgradeStar : IPacket
{
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_UpgradeStar; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UpgradeStar), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UpgradeStarResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newDiamond;
	public int charDataId;
	public int level;
	public int exp;
	public int maxExp;
	public int starLevel;
	public int statPoints;
	public int shards;
	public int invAttack;
	public int invDefense;
	public int invHealth;
	public int invSkillAccel;
	public int invAttackSpeed;
	public int invMoveSpeed;
	public int autoStatType;
	public bool limitBroken;

	public ushort Protocol { get { return (ushort)PacketID.S_UpgradeStarResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UpgradeStarResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_LimitBreak : IPacket
{
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_LimitBreak; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_LimitBreak), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_LimitBreakResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newLimitBreakTicket;
	public int charDataId;
	public int level;
	public int exp;
	public int maxExp;
	public int starLevel;
	public int statPoints;
	public int shards;
	public int invAttack;
	public int invDefense;
	public int invHealth;
	public int invSkillAccel;
	public int invAttackSpeed;
	public int invMoveSpeed;
	public int autoStatType;
	public bool limitBroken;

	public ushort Protocol { get { return (ushort)PacketID.S_LimitBreakResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newLimitBreakTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_LimitBreakResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newLimitBreakTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestShop : IPacket
{
	public int requestId;

	public ushort Protocol { get { return (ushort)PacketID.C_RequestShop; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.requestId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestShop), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.requestId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ShopCatalog : IPacket
{
	public int requestId;
	public int gold;
	public int diamond;
	public int rotationCharId;
	public long rotationStartUnix;
	public long rotationEndUnix;
	public int rotationEquipId;
	public long rotationEquipStartUnix;
	public long rotationEquipEndUnix;
	public class Wallet
	{
		public string key;
		public int qty;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort keyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.key = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, keyLen);
			count += keyLen;
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string keyValue = this.key ?? string.Empty;
			ushort keyLen = (ushort)Encoding.Unicode.GetBytes(keyValue, 0, keyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(keyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += keyLen;
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Wallet> wallets = new List<Wallet>();
	public class Category
	{
		public string key;
		public string displayName;
		public bool hasAllTab;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort keyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.key = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, keyLen);
			count += keyLen;
			ushort displayNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.displayName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, displayNameLen);
			count += displayNameLen;
			this.hasAllTab = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string keyValue = this.key ?? string.Empty;
			ushort keyLen = (ushort)Encoding.Unicode.GetBytes(keyValue, 0, keyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(keyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += keyLen;
			string displayNameValue = this.displayName ?? string.Empty;
			ushort displayNameLen = (ushort)Encoding.Unicode.GetBytes(displayNameValue, 0, displayNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(displayNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += displayNameLen;
			Array.Copy(BitConverter.GetBytes(this.hasAllTab), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Category> categorys = new List<Category>();
	public class Pity
	{
		public string poolKey;
		public int count;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort poolKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.poolKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, poolKeyLen);
			count += poolKeyLen;
			this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string poolKeyValue = this.poolKey ?? string.Empty;
			ushort poolKeyLen = (ushort)Encoding.Unicode.GetBytes(poolKeyValue, 0, poolKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(poolKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += poolKeyLen;
			Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Pity> pitys = new List<Pity>();
	public class FreeGacha
	{
		public string key;
		public long nextUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort keyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.key = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, keyLen);
			count += keyLen;
			this.nextUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string keyValue = this.key ?? string.Empty;
			ushort keyLen = (ushort)Encoding.Unicode.GetBytes(keyValue, 0, keyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(keyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += keyLen;
			Array.Copy(BitConverter.GetBytes(this.nextUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<FreeGacha> freeGachas = new List<FreeGacha>();

	public ushort Protocol { get { return (ushort)PacketID.S_ShopCatalog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.requestId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.diamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rotationCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rotationStartUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.rotationEndUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.rotationEquipId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rotationEquipStartUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.rotationEquipEndUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.wallets.Clear();
		ushort walletLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < walletLen; i++)
		{
			Wallet wallet = new Wallet();
			wallet.Read(segment, ref count);
			wallets.Add(wallet);
		}
		this.categorys.Clear();
		ushort categoryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < categoryLen; i++)
		{
			Category category = new Category();
			category.Read(segment, ref count);
			categorys.Add(category);
		}
		this.pitys.Clear();
		ushort pityLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < pityLen; i++)
		{
			Pity pity = new Pity();
			pity.Read(segment, ref count);
			pitys.Add(pity);
		}
		this.freeGachas.Clear();
		ushort freeGachaLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < freeGachaLen; i++)
		{
			FreeGacha freeGacha = new FreeGacha();
			freeGacha.Read(segment, ref count);
			freeGachas.Add(freeGacha);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ShopCatalog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.requestId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.diamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rotationCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rotationStartUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.rotationEndUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.rotationEquipId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rotationEquipStartUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.rotationEquipEndUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.wallets.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Wallet wallet in this.wallets)
			wallet.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.categorys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Category category in this.categorys)
			category.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.pitys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Pity pity in this.pitys)
			pity.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.freeGachas.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (FreeGacha freeGacha in this.freeGachas)
			freeGacha.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ShopOfferPage : IPacket
{
	public int requestId;
	public class Offer
	{
		public string offerId;
		public string displayName;
		public string nameColorHex;
		public string description;
		public string major;
		public string sub;
		public string imageKey;
		public string currency;
		public int price;
		public bool isGacha;
		public bool gachaIsEquip;
		public string ticketKey;
		public string ticketOfferId;
		public int pullPrice;
		public int gachaCount;
		public string poolKey;
		public bool hasPickup;
		public int limitType;
		public int limitCount;
		public bool soldOut;
		public bool locked;
		public int remaining;
		public bool hasRates;
		public bool allowQty;
		public int maxQty;
		public long saleStartUnix;
		public long saleEndUnix;
		public bool hot;
		public bool flash;
		public bool nonConsumable;
		public int requiredGuildLevel;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
			count += offerIdLen;
			ushort displayNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.displayName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, displayNameLen);
			count += displayNameLen;
			ushort nameColorHexLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nameColorHex = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nameColorHexLen);
			count += nameColorHexLen;
			ushort descriptionLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.description = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, descriptionLen);
			count += descriptionLen;
			ushort majorLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.major = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, majorLen);
			count += majorLen;
			ushort subLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.sub = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, subLen);
			count += subLen;
			ushort imageKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.imageKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, imageKeyLen);
			count += imageKeyLen;
			ushort currencyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currency = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyLen);
			count += currencyLen;
			this.price = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.isGacha = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.gachaIsEquip = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			ushort ticketKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.ticketKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, ticketKeyLen);
			count += ticketKeyLen;
			ushort ticketOfferIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.ticketOfferId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, ticketOfferIdLen);
			count += ticketOfferIdLen;
			this.pullPrice = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gachaCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort poolKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.poolKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, poolKeyLen);
			count += poolKeyLen;
			this.hasPickup = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.limitType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.limitCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.soldOut = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.locked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.remaining = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.hasRates = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.allowQty = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.maxQty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.saleStartUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.saleEndUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.hot = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.flash = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.nonConsumable = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.requiredGuildLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string offerIdValue = this.offerId ?? string.Empty;
			ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += offerIdLen;
			string displayNameValue = this.displayName ?? string.Empty;
			ushort displayNameLen = (ushort)Encoding.Unicode.GetBytes(displayNameValue, 0, displayNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(displayNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += displayNameLen;
			string nameColorHexValue = this.nameColorHex ?? string.Empty;
			ushort nameColorHexLen = (ushort)Encoding.Unicode.GetBytes(nameColorHexValue, 0, nameColorHexValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nameColorHexLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nameColorHexLen;
			string descriptionValue = this.description ?? string.Empty;
			ushort descriptionLen = (ushort)Encoding.Unicode.GetBytes(descriptionValue, 0, descriptionValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(descriptionLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += descriptionLen;
			string majorValue = this.major ?? string.Empty;
			ushort majorLen = (ushort)Encoding.Unicode.GetBytes(majorValue, 0, majorValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(majorLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += majorLen;
			string subValue = this.sub ?? string.Empty;
			ushort subLen = (ushort)Encoding.Unicode.GetBytes(subValue, 0, subValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(subLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += subLen;
			string imageKeyValue = this.imageKey ?? string.Empty;
			ushort imageKeyLen = (ushort)Encoding.Unicode.GetBytes(imageKeyValue, 0, imageKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(imageKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += imageKeyLen;
			string currencyValue = this.currency ?? string.Empty;
			ushort currencyLen = (ushort)Encoding.Unicode.GetBytes(currencyValue, 0, currencyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyLen;
			Array.Copy(BitConverter.GetBytes(this.price), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.isGacha), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.gachaIsEquip), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			string ticketKeyValue = this.ticketKey ?? string.Empty;
			ushort ticketKeyLen = (ushort)Encoding.Unicode.GetBytes(ticketKeyValue, 0, ticketKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(ticketKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += ticketKeyLen;
			string ticketOfferIdValue = this.ticketOfferId ?? string.Empty;
			ushort ticketOfferIdLen = (ushort)Encoding.Unicode.GetBytes(ticketOfferIdValue, 0, ticketOfferIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(ticketOfferIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += ticketOfferIdLen;
			Array.Copy(BitConverter.GetBytes(this.pullPrice), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gachaCount), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string poolKeyValue = this.poolKey ?? string.Empty;
			ushort poolKeyLen = (ushort)Encoding.Unicode.GetBytes(poolKeyValue, 0, poolKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(poolKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += poolKeyLen;
			Array.Copy(BitConverter.GetBytes(this.hasPickup), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.limitType), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.limitCount), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.soldOut), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.locked), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.remaining), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.hasRates), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.allowQty), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.maxQty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.saleStartUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.saleEndUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.hot), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.flash), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.nonConsumable), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.requiredGuildLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Offer> offers = new List<Offer>();
	public class OfferGrant
	{
		public string offerId;
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
			count += offerIdLen;
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string offerIdValue = this.offerId ?? string.Empty;
			ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += offerIdLen;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<OfferGrant> offerGrants = new List<OfferGrant>();

	public ushort Protocol { get { return (ushort)PacketID.S_ShopOfferPage; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.requestId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.offers.Clear();
		ushort offerLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < offerLen; i++)
		{
			Offer offer = new Offer();
			offer.Read(segment, ref count);
			offers.Add(offer);
		}
		this.offerGrants.Clear();
		ushort offerGrantLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < offerGrantLen; i++)
		{
			OfferGrant offerGrant = new OfferGrant();
			offerGrant.Read(segment, ref count);
			offerGrants.Add(offerGrant);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ShopOfferPage), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.requestId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.offers.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Offer offer in this.offers)
			offer.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.offerGrants.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (OfferGrant offerGrant in this.offerGrants)
			offerGrant.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_BuyOffer : IPacket
{
	public string offerId;
	public string idempotencyKey;
	public int qty;
	public bool free;
	public int storeKind;
	public string receipt;

	public ushort Protocol { get { return (ushort)PacketID.C_BuyOffer; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
		count += offerIdLen;
		ushort idempotencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.idempotencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, idempotencyKeyLen);
		count += idempotencyKeyLen;
		this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.free = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.storeKind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort receiptLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.receipt = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, receiptLen);
		count += receiptLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_BuyOffer), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string offerIdValue = this.offerId ?? string.Empty;
		ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += offerIdLen;
		string idempotencyKeyValue = this.idempotencyKey ?? string.Empty;
		ushort idempotencyKeyLen = (ushort)Encoding.Unicode.GetBytes(idempotencyKeyValue, 0, idempotencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(idempotencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += idempotencyKeyLen;
		Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.free), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.storeKind), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string receiptValue = this.receipt ?? string.Empty;
		ushort receiptLen = (ushort)Encoding.Unicode.GetBytes(receiptValue, 0, receiptValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(receiptLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += receiptLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_PurchaseGate : IPacket
{
	public bool open;

	public ushort Protocol { get { return (ushort)PacketID.C_PurchaseGate; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.open = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_PurchaseGate), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.open), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CanBuyOffer : IPacket
{
	public string offerId;
	public int qty;

	public ushort Protocol { get { return (ushort)PacketID.C_CanBuyOffer; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
		count += offerIdLen;
		this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CanBuyOffer), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string offerIdValue = this.offerId ?? string.Empty;
		ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += offerIdLen;
		Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_CanBuyOffer : IPacket
{
	public string offerId;
	public bool ok;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_CanBuyOffer; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
		count += offerIdLen;
		this.ok = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_CanBuyOffer), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string offerIdValue = this.offerId ?? string.Empty;
		ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += offerIdLen;
		Array.Copy(BitConverter.GetBytes(this.ok), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GachaMileagePick : IPacket
{
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_GachaMileagePick; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GachaMileagePick), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GachaMileagePickResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newMileage;
	public int charDataId;
	public bool isNew;
	public int shardsGained;
	public int maxExp;

	public ushort Protocol { get { return (ushort)PacketID.S_GachaMileagePickResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newMileage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isNew = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.shardsGained = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GachaMileagePickResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newMileage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isNew), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.shardsGained), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GachaRates : IPacket
{
	public string offerId;

	public ushort Protocol { get { return (ushort)PacketID.C_GachaRates; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
		count += offerIdLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GachaRates), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string offerIdValue = this.offerId ?? string.Empty;
		ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += offerIdLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GachaRates : IPacket
{
	public string offerId;
	public class Entry
	{
		public int id;
		public bool isEquip;
		public int grade;
		public double rate;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.id = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.isEquip = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.grade = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.rate = BitConverter.ToDouble(segment.Array, segment.Offset + count);
			count += sizeof(double);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.id), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.isEquip), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.grade), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.rate), 0, segment.Array, segment.Offset + count, sizeof(double));
			count += sizeof(double);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();

	public ushort Protocol { get { return (ushort)PacketID.S_GachaRates; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
		count += offerIdLen;
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GachaRates), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string offerIdValue = this.offerId ?? string.Empty;
		ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += offerIdLen;
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RotationInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_RotationInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RotationInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_RotationInfo : IPacket
{
	public class Entry
	{
		public bool isEquip;
		public int order;
		public int id;
		public int grade;
		public long startUnix;
		public long endUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.isEquip = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.order = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.id = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.grade = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.isEquip), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.order), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.id), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.grade), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();

	public ushort Protocol { get { return (ushort)PacketID.S_RotationInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_RotationInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BuyResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class GachaRoll
	{
		public int charDataId;
		public bool isNew;
		public int shardsGained;
		public int maxExp;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.isNew = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.shardsGained = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.isNew), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.shardsGained), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<GachaRoll> gachaRolls = new List<GachaRoll>();
	public class EquipRoll
	{
		public long instanceId;
		public int equipDataId;
		public int starLevel;
		public int level;
		public int mainPoints;
		public int opt1Points;
		public int opt2Points;
		public int opt3Points;
		public int opt4Points;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.equipDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.mainPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt1Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt2Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt3Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt4Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.equipDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.mainPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt1Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt2Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt3Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt4Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<EquipRoll> equipRolls = new List<EquipRoll>();
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_BuyResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gachaRolls.Clear();
		ushort gachaRollLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < gachaRollLen; i++)
		{
			GachaRoll gachaRoll = new GachaRoll();
			gachaRoll.Read(segment, ref count);
			gachaRolls.Add(gachaRoll);
		}
		this.equipRolls.Clear();
		ushort equipRollLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equipRollLen; i++)
		{
			EquipRoll equipRoll = new EquipRoll();
			equipRoll.Read(segment, ref count);
			equipRolls.Add(equipRoll);
		}
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BuyResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.gachaRolls.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GachaRoll gachaRoll in this.gachaRolls)
			gachaRoll.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.equipRolls.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (EquipRoll equipRoll in this.equipRolls)
			equipRoll.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestInventory : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_RequestInventory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestInventory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Inventory : IPacket
{
	public class Equip
	{
		public long instanceId;
		public int equipDataId;
		public int starLevel;
		public int level;
		public int mainPoints;
		public int opt1Points;
		public int opt2Points;
		public int opt3Points;
		public int opt4Points;
		public int equippedCharDataId;
		public bool limitBroken;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.equipDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.mainPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt1Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt2Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt3Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt4Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.equippedCharDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.equipDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.mainPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt1Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt2Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt3Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt4Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.equippedCharDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Equip> equips = new List<Equip>();
	public class UniversalShard
	{
		public int grade;
		public int qty;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.grade = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.grade), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<UniversalShard> universalShards = new List<UniversalShard>();

	public ushort Protocol { get { return (ushort)PacketID.S_Inventory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.equips.Clear();
		ushort equipLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equipLen; i++)
		{
			Equip equip = new Equip();
			equip.Read(segment, ref count);
			equips.Add(equip);
		}
		this.universalShards.Clear();
		ushort universalShardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < universalShardLen; i++)
		{
			UniversalShard universalShard = new UniversalShard();
			universalShard.Read(segment, ref count);
			universalShards.Add(universalShard);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Inventory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.equips.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Equip equip in this.equips)
			equip.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.universalShards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (UniversalShard universalShard in this.universalShards)
			universalShard.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipItem : IPacket
{
	public long instanceId;
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipItem; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipItem), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UnequipItem : IPacket
{
	public long instanceId;

	public ushort Protocol { get { return (ushort)PacketID.C_UnequipItem; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UnequipItem), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UnequipResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_UnequipResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UnequipResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AwakenEquip : IPacket
{
	public long instanceId;
	public class Material
	{
		public long instanceId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Material> materials = new List<Material>();

	public ushort Protocol { get { return (ushort)PacketID.C_AwakenEquip; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.materials.Clear();
		ushort materialLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < materialLen; i++)
		{
			Material material = new Material();
			material.Read(segment, ref count);
			materials.Add(material);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AwakenEquip), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.materials.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Material material in this.materials)
			material.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AwakenResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newStarLevel;
	public int newDiamond;

	public ushort Protocol { get { return (ushort)PacketID.S_AwakenResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStarLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AwakenResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStarLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipLimitBreak : IPacket
{
	public long instanceId;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipLimitBreak; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipLimitBreak), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipLimitBreakResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public long instanceId;
	public int newLimitBreakTicket;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipLimitBreakResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.newLimitBreakTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipLimitBreakResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.newLimitBreakTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DisassembleEquip : IPacket
{
	public class Target
	{
		public long instanceId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Target> targets = new List<Target>();

	public ushort Protocol { get { return (ushort)PacketID.C_DisassembleEquip; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.targets.Clear();
		ushort targetLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < targetLen; i++)
		{
			Target target = new Target();
			target.Read(segment, ref count);
			targets.Add(target);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DisassembleEquip), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.targets.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Target target in this.targets)
			target.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DisassembleEquipByGrade : IPacket
{
	public class Target
	{
		public int grade;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.grade = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.grade), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Target> targets = new List<Target>();

	public ushort Protocol { get { return (ushort)PacketID.C_DisassembleEquipByGrade; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.targets.Clear();
		ushort targetLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < targetLen; i++)
		{
			Target target = new Target();
			target.Read(segment, ref count);
			targets.Add(target);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DisassembleEquipByGrade), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.targets.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Target target in this.targets)
			target.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DisassembleEquipResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int count;
	public int gainedGold;
	public int newGold;

	public ushort Protocol { get { return (ushort)PacketID.S_DisassembleEquipResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DisassembleEquipResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ConvertUniversalShard : IPacket
{
	public int charDataId;
	public int amount;

	public ushort Protocol { get { return (ushort)PacketID.C_ConvertUniversalShard; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.amount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ConvertUniversalShard), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.amount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ConvertUniversalShardResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int newCharShards;
	public int grade;
	public int newUniversalQty;

	public ushort Protocol { get { return (ushort)PacketID.S_ConvertUniversalShardResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newCharShards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grade = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newUniversalQty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ConvertUniversalShardResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newCharShards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.grade), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newUniversalQty), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestMail : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_RequestMail; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestMail), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MailList : IPacket
{
	public class Mail
	{
		public long mailId;
		public string title;
		public string body;
		public long createdUnix;
		public long expiresUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.mailId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			ushort titleLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.title = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, titleLen);
			count += titleLen;
			ushort bodyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.body = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, bodyLen);
			count += bodyLen;
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.expiresUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.mailId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			string titleValue = this.title ?? string.Empty;
			ushort titleLen = (ushort)Encoding.Unicode.GetBytes(titleValue, 0, titleValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(titleLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += titleLen;
			string bodyValue = this.body ?? string.Empty;
			ushort bodyLen = (ushort)Encoding.Unicode.GetBytes(bodyValue, 0, bodyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(bodyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += bodyLen;
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.expiresUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Mail> mails = new List<Mail>();
	public class Reward
	{
		public long mailId;
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.mailId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.mailId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Reward> rewards = new List<Reward>();

	public ushort Protocol { get { return (ushort)PacketID.S_MailList; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.mails.Clear();
		ushort mailLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < mailLen; i++)
		{
			Mail mail = new Mail();
			mail.Read(segment, ref count);
			mails.Add(mail);
		}
		this.rewards.Clear();
		ushort rewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < rewardLen; i++)
		{
			Reward reward = new Reward();
			reward.Read(segment, ref count);
			rewards.Add(reward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MailList), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.mails.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Mail mail in this.mails)
			mail.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.rewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Reward reward in this.rewards)
			reward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimMail : IPacket
{
	public long mailId;
	public bool claimAll;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimMail; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.mailId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.claimAll = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimMail), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.mailId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.claimAll), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimMailResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();
	public class ClaimedMailId
	{
		public long mailId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.mailId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.mailId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<ClaimedMailId> claimedMailIds = new List<ClaimedMailId>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimMailResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
		this.claimedMailIds.Clear();
		ushort claimedMailIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < claimedMailIdLen; i++)
		{
			ClaimedMailId claimedMailId = new ClaimedMailId();
			claimedMailId.Read(segment, ref count);
			claimedMailIds.Add(claimedMailId);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimMailResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.claimedMailIds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (ClaimedMailId claimedMailId in this.claimedMailIds)
			claimedMailId.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MailNotify : IPacket
{
	public int unreadCount;

	public ushort Protocol { get { return (ushort)PacketID.S_MailNotify; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.unreadCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MailNotify), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.unreadCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Profile : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_Profile; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Profile), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Profile : IPacket
{
	public string nickname;
	public int equippedProfileFrameId;
	public int nicknameTicketQty;
	public long createdUnix;
	public long achievementScore;
	public int combatPowerRank;
	public int achievementRank;
	public class ProfileFrame
	{
		public int profileFrameId;
		public long expiresUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.expiresUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.expiresUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<ProfileFrame> profileFrames = new List<ProfileFrame>();

	public ushort Protocol { get { return (ushort)PacketID.S_Profile; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.equippedProfileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nicknameTicketQty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.achievementScore = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.combatPowerRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.achievementRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.profileFrames.Clear();
		ushort profileFrameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < profileFrameLen; i++)
		{
			ProfileFrame profileFrame = new ProfileFrame();
			profileFrame.Read(segment, ref count);
			profileFrames.Add(profileFrame);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Profile), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.equippedProfileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nicknameTicketQty), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.achievementScore), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.combatPowerRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.achievementRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.profileFrames.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (ProfileFrame profileFrame in this.profileFrames)
			profileFrame.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipProfileFrame : IPacket
{
	public int profileFrameId;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipProfileFrame; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipProfileFrame), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipProfileFrameResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int equippedProfileFrameId;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipProfileFrameResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.equippedProfileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipProfileFrameResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.equippedProfileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CheckNickname : IPacket
{
	public string nickname;

	public ushort Protocol { get { return (ushort)PacketID.C_CheckNickname; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CheckNickname), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_CheckNicknameResult : IPacket
{
	public string nickname;
	public bool available;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_CheckNicknameResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.available = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_CheckNicknameResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.available), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ChangeNickname : IPacket
{
	public string nickname;

	public ushort Protocol { get { return (ushort)PacketID.C_ChangeNickname; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ChangeNickname), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ChangeNicknameResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public string nickname;
	public int newCertificate;
	public int newTicketQty;

	public ushort Protocol { get { return (ushort)PacketID.S_ChangeNicknameResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.newCertificate = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newTicketQty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ChangeNicknameResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.newCertificate), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newTicketQty), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EncyclopediaRewards : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EncyclopediaRewards; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EncyclopediaRewards), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EncyclopediaRewards : IPacket
{
	public int collectionCount;
	public int totalStar;
	public class ClaimedCollection
	{
		public int step;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.step = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.step), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<ClaimedCollection> claimedCollections = new List<ClaimedCollection>();
	public class ClaimedStar
	{
		public int step;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.step = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.step), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<ClaimedStar> claimedStars = new List<ClaimedStar>();

	public ushort Protocol { get { return (ushort)PacketID.S_EncyclopediaRewards; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.collectionCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.totalStar = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedCollections.Clear();
		ushort claimedCollectionLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < claimedCollectionLen; i++)
		{
			ClaimedCollection claimedCollection = new ClaimedCollection();
			claimedCollection.Read(segment, ref count);
			claimedCollections.Add(claimedCollection);
		}
		this.claimedStars.Clear();
		ushort claimedStarLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < claimedStarLen; i++)
		{
			ClaimedStar claimedStar = new ClaimedStar();
			claimedStar.Read(segment, ref count);
			claimedStars.Add(claimedStar);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EncyclopediaRewards), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.collectionCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.totalStar), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.claimedCollections.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (ClaimedCollection claimedCollection in this.claimedCollections)
			claimedCollection.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.claimedStars.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (ClaimedStar claimedStar in this.claimedStars)
			claimedStar.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimEncyclopediaReward : IPacket
{
	public int rewardType;
	public int step;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimEncyclopediaReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.rewardType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.step = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimEncyclopediaReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.rewardType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.step), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimEncyclopediaRewardResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int rewardType;
	public int step;
	public int newDiamond;
	public int grantedGrade;

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimEncyclopediaRewardResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rewardType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.step = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedGrade = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimEncyclopediaRewardResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rewardType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.step), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.grantedGrade), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ChargeRewards : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ChargeRewards; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ChargeRewards), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ChargeRewards : IPacket
{
	public int cumulativeWon;
	public class ClaimedTier
	{
		public int tier;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<ClaimedTier> claimedTiers = new List<ClaimedTier>();

	public ushort Protocol { get { return (ushort)PacketID.S_ChargeRewards; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.cumulativeWon = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedTiers.Clear();
		ushort claimedTierLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < claimedTierLen; i++)
		{
			ClaimedTier claimedTier = new ClaimedTier();
			claimedTier.Read(segment, ref count);
			claimedTiers.Add(claimedTier);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ChargeRewards), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.cumulativeWon), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.claimedTiers.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (ClaimedTier claimedTier in this.claimedTiers)
			claimedTier.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimChargeReward : IPacket
{
	public int tier;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimChargeReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimChargeReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimChargeRewardResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int tier;
	public int newDiamond;

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimChargeRewardResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimChargeRewardResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EventMissions : IPacket
{
	public string eventKey;

	public ushort Protocol { get { return (ushort)PacketID.C_EventMissions; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EventMissions), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EventMissions : IPacket
{
	public string eventKey;
	public long endUnix;
	public int totalMissions;
	public int achievedMissions;
	public int unlockedDay;
	public class Mission
	{
		public int id;
		public bool daily;
		public int metric;
		public int param;
		public int threshold;
		public int progress;
		public bool claimed;
		public bool locked;
		public int group;
		public int unlockDay;
		public int rewardKind;
		public int rewardValue;
		public int rewardQty;
		public string rewardCurrencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.id = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.daily = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.metric = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.param = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.threshold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.progress = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.claimed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.locked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.group = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.unlockDay = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.rewardKind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.rewardValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.rewardQty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort rewardCurrencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.rewardCurrencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, rewardCurrencyKeyLen);
			count += rewardCurrencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.id), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.daily), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.metric), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.param), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.threshold), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.progress), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.claimed), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.locked), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.group), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.unlockDay), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.rewardKind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.rewardValue), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.rewardQty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string rewardCurrencyKeyValue = this.rewardCurrencyKey ?? string.Empty;
			ushort rewardCurrencyKeyLen = (ushort)Encoding.Unicode.GetBytes(rewardCurrencyKeyValue, 0, rewardCurrencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(rewardCurrencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += rewardCurrencyKeyLen;
			return success;
		}	
	}
	public List<Mission> missions = new List<Mission>();

	public ushort Protocol { get { return (ushort)PacketID.S_EventMissions; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
		this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.totalMissions = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.achievedMissions = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.unlockedDay = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.missions.Clear();
		ushort missionLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < missionLen; i++)
		{
			Mission mission = new Mission();
			mission.Read(segment, ref count);
			missions.Add(mission);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EventMissions), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;
		Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.totalMissions), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.achievedMissions), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.unlockedDay), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.missions.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Mission mission in this.missions)
			mission.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Heartbeat : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_Heartbeat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Heartbeat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_HeartbeatResult : IPacket
{
	public int sessionSecondsToday;
	public int staminaBoostTicket;

	public ushort Protocol { get { return (ushort)PacketID.S_HeartbeatResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.sessionSecondsToday = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaBoostTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_HeartbeatResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.sessionSecondsToday), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaBoostTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimEventMission : IPacket
{
	public int missionId;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimEventMission; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.missionId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimEventMission), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.missionId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimEventMissionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int missionId;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimEventMissionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.missionId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimEventMissionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.missionId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_LevelUpEquip : IPacket
{
	public long instanceId;
	public int count;

	public ushort Protocol { get { return (ushort)PacketID.C_LevelUpEquip; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_LevelUpEquip), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_LevelUpEquipResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newLevel;
	public int newGold;
	public int gainedLevels;

	public ushort Protocol { get { return (ushort)PacketID.S_LevelUpEquipResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedLevels = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_LevelUpEquipResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedLevels), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CommissionInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_CommissionInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CommissionInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_CommissionInfo : IPacket
{
	public int tickets;
	public int ticketMax;
	public int renown;
	public int engravingStone;
	public class Stage
	{
		public int commissionType;
		public int clearedStage;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.clearedStage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.clearedStage), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Stage> stages = new List<Stage>();

	public ushort Protocol { get { return (ushort)PacketID.S_CommissionInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.tickets = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.ticketMax = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.renown = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.engravingStone = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stages.Clear();
		ushort stageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < stageLen; i++)
		{
			Stage stage = new Stage();
			stage.Read(segment, ref count);
			stages.Add(stage);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_CommissionInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.tickets), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.ticketMax), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.renown), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.engravingStone), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.stages.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Stage stage in this.stages)
			stage.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_StartCommission : IPacket
{
	public int commissionType;
	public int stage;

	public ushort Protocol { get { return (ushort)PacketID.C_StartCommission; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_StartCommission), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_StartCommissionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newTickets;

	public ushort Protocol { get { return (ushort)PacketID.S_StartCommissionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newTickets = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_StartCommissionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newTickets), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClearCommission : IPacket
{
	public int commissionType;
	public int stage;

	public ushort Protocol { get { return (ushort)PacketID.C_ClearCommission; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClearCommission), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClearCommissionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int commissionType;
	public int clearedStage;
	public int newGold;
	public int newDiamond;
	public int newRenown;
	public int newEngravingStone;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClearCommissionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.clearedStage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newRenown = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newEngravingStone = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClearCommissionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.clearedStage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newRenown), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newEngravingStone), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SweepCommission : IPacket
{
	public int commissionType;
	public int count;

	public ushort Protocol { get { return (ushort)PacketID.C_SweepCommission; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SweepCommission), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SweepCommissionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int commissionType;
	public int count;
	public int newTickets;
	public int newGold;
	public int newDiamond;
	public int newRenown;
	public int newEngravingStone;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_SweepCommissionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newTickets = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newRenown = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newEngravingStone = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SweepCommissionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newTickets), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newRenown), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newEngravingStone), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EngravingInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EngravingInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EngravingInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EngravingInfo : IPacket
{
	public int engravingStone;
	public class Engraving
	{
		public int engravingId;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Engraving> engravings = new List<Engraving>();
	public class Slot
	{
		public int charDataId;
		public int slotIndex;
		public int engravingId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Slot> slots = new List<Slot>();

	public ushort Protocol { get { return (ushort)PacketID.S_EngravingInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.engravingStone = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.engravings.Clear();
		ushort engravingLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < engravingLen; i++)
		{
			Engraving engraving = new Engraving();
			engraving.Read(segment, ref count);
			engravings.Add(engraving);
		}
		this.slots.Clear();
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < slotLen; i++)
		{
			Slot slot = new Slot();
			slot.Read(segment, ref count);
			slots.Add(slot);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EngravingInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.engravingStone), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.engravings.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Engraving engraving in this.engravings)
			engraving.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.slots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Slot slot in this.slots)
			slot.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UpgradeEngraving : IPacket
{
	public int engravingId;

	public ushort Protocol { get { return (ushort)PacketID.C_UpgradeEngraving; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UpgradeEngraving), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UpgradeEngravingResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int engravingId;
	public int level;
	public int newEngravingStone;

	public ushort Protocol { get { return (ushort)PacketID.S_UpgradeEngravingResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newEngravingStone = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UpgradeEngravingResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newEngravingStone), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipEngraving : IPacket
{
	public int charDataId;
	public int slotIndex;
	public int engravingId;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipEngraving; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipEngraving), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipEngravingResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int slotIndex;
	public int engravingId;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipEngravingResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipEngravingResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CostumeInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_CostumeInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CostumeInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_CostumeInfo : IPacket
{
	public class OwnedCostume
	{
		public int costumeId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<OwnedCostume> ownedCostumes = new List<OwnedCostume>();
	public class EquippedCostume
	{
		public int charDataId;
		public int costumeId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<EquippedCostume> equippedCostumes = new List<EquippedCostume>();
	public class OwnedUnitSkin
	{
		public int unitSkinId;
		public int count;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.unitSkinId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.unitSkinId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<OwnedUnitSkin> ownedUnitSkins = new List<OwnedUnitSkin>();
	public class EquippedUnitSkin
	{
		public int charDataId;
		public int unitSkinId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.unitSkinId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.unitSkinId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<EquippedUnitSkin> equippedUnitSkins = new List<EquippedUnitSkin>();

	public ushort Protocol { get { return (ushort)PacketID.S_CostumeInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.ownedCostumes.Clear();
		ushort ownedCostumeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < ownedCostumeLen; i++)
		{
			OwnedCostume ownedCostume = new OwnedCostume();
			ownedCostume.Read(segment, ref count);
			ownedCostumes.Add(ownedCostume);
		}
		this.equippedCostumes.Clear();
		ushort equippedCostumeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equippedCostumeLen; i++)
		{
			EquippedCostume equippedCostume = new EquippedCostume();
			equippedCostume.Read(segment, ref count);
			equippedCostumes.Add(equippedCostume);
		}
		this.ownedUnitSkins.Clear();
		ushort ownedUnitSkinLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < ownedUnitSkinLen; i++)
		{
			OwnedUnitSkin ownedUnitSkin = new OwnedUnitSkin();
			ownedUnitSkin.Read(segment, ref count);
			ownedUnitSkins.Add(ownedUnitSkin);
		}
		this.equippedUnitSkins.Clear();
		ushort equippedUnitSkinLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equippedUnitSkinLen; i++)
		{
			EquippedUnitSkin equippedUnitSkin = new EquippedUnitSkin();
			equippedUnitSkin.Read(segment, ref count);
			equippedUnitSkins.Add(equippedUnitSkin);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_CostumeInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.ownedCostumes.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (OwnedCostume ownedCostume in this.ownedCostumes)
			ownedCostume.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.equippedCostumes.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (EquippedCostume equippedCostume in this.equippedCostumes)
			equippedCostume.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.ownedUnitSkins.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (OwnedUnitSkin ownedUnitSkin in this.ownedUnitSkins)
			ownedUnitSkin.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.equippedUnitSkins.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (EquippedUnitSkin equippedUnitSkin in this.equippedUnitSkins)
			equippedUnitSkin.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipUnitSkin : IPacket
{
	public int charDataId;
	public int unitSkinId;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipUnitSkin; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.unitSkinId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipUnitSkin), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.unitSkinId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipUnitSkinResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int unitSkinId;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipUnitSkinResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.unitSkinId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipUnitSkinResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.unitSkinId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipCostume : IPacket
{
	public int charDataId;
	public int costumeId;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipCostume; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipCostume), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipCostumeResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int charDataId;
	public int costumeId;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipCostumeResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipCostumeResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FlashSaleInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_FlashSaleInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FlashSaleInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FlashSaleInfo : IPacket
{
	public class FlashSale
	{
		public string offerId;
		public long startUnix;
		public long endUnix;
		public bool soldOut;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
			count += offerIdLen;
			this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.soldOut = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string offerIdValue = this.offerId ?? string.Empty;
			ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += offerIdLen;
			Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.soldOut), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<FlashSale> flashSales = new List<FlashSale>();

	public ushort Protocol { get { return (ushort)PacketID.S_FlashSaleInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.flashSales.Clear();
		ushort flashSaleLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < flashSaleLen; i++)
		{
			FlashSale flashSale = new FlashSale();
			flashSale.Read(segment, ref count);
			flashSales.Add(flashSale);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FlashSaleInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.flashSales.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (FlashSale flashSale in this.flashSales)
			flashSale.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FlashSaleNotify : IPacket
{
	public class FlashSale
	{
		public string offerId;
		public long startUnix;
		public long endUnix;
		public bool soldOut;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
			count += offerIdLen;
			this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.soldOut = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string offerIdValue = this.offerId ?? string.Empty;
			ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(offerIdValue, 0, offerIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += offerIdLen;
			Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.soldOut), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<FlashSale> flashSales = new List<FlashSale>();

	public ushort Protocol { get { return (ushort)PacketID.S_FlashSaleNotify; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.flashSales.Clear();
		ushort flashSaleLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < flashSaleLen; i++)
		{
			FlashSale flashSale = new FlashSale();
			flashSale.Read(segment, ref count);
			flashSales.Add(flashSale);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FlashSaleNotify), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.flashSales.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (FlashSale flashSale in this.flashSales)
			flashSale.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ChatSend : IPacket
{
	public int channel;
	public string text;
	public int targetAccountId;

	public ushort Protocol { get { return (ushort)PacketID.C_ChatSend; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.channel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort textLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.text = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, textLen);
		count += textLen;
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ChatSend), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.channel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string textValue = this.text ?? string.Empty;
		ushort textLen = (ushort)Encoding.Unicode.GetBytes(textValue, 0, textValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(textLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += textLen;
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ChatSendResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_ChatSendResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ChatSendResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ChatMessage : IPacket
{
	public long messageId;
	public int channel;
	public int type;
	public int noticeKind;
	public int accountId;
	public string nickname;
	public int profileFrameId;
	public int representativeCharId;
	public int representativeCostumeId;
	public string text;
	public int refId;
	public int refValue;
	public long createdUnix;
	public int targetAccountId;
	public string targetNickname;

	public ushort Protocol { get { return (ushort)PacketID.S_ChatMessage; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.messageId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.channel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.type = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.noticeKind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort textLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.text = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, textLen);
		count += textLen;
		this.refId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.refValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort targetNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.targetNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, targetNicknameLen);
		count += targetNicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ChatMessage), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.messageId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.channel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.type), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.noticeKind), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string textValue = this.text ?? string.Empty;
		ushort textLen = (ushort)Encoding.Unicode.GetBytes(textValue, 0, textValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(textLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += textLen;
		Array.Copy(BitConverter.GetBytes(this.refId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.refValue), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string targetNicknameValue = this.targetNickname ?? string.Empty;
		ushort targetNicknameLen = (ushort)Encoding.Unicode.GetBytes(targetNicknameValue, 0, targetNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(targetNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += targetNicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DeleteWhisper : IPacket
{
	public int partnerAccountId;

	public ushort Protocol { get { return (ushort)PacketID.C_DeleteWhisper; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.partnerAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DeleteWhisper), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.partnerAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DeleteWhisperResult : IPacket
{
	public bool isSuccess;
	public int partnerAccountId;

	public ushort Protocol { get { return (ushort)PacketID.S_DeleteWhisperResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.partnerAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DeleteWhisperResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.partnerAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReportChat : IPacket
{
	public long messageId;
	public int reason;
	public string reasonText;

	public ushort Protocol { get { return (ushort)PacketID.C_ReportChat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.messageId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.reason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort reasonTextLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.reasonText = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, reasonTextLen);
		count += reasonTextLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReportChat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.messageId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.reason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string reasonTextValue = this.reasonText ?? string.Empty;
		ushort reasonTextLen = (ushort)Encoding.Unicode.GetBytes(reasonTextValue, 0, reasonTextValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(reasonTextLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += reasonTextLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ReportChatResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_ReportChatResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ReportChatResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ChatHistory : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ChatHistory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ChatHistory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ChatHistory : IPacket
{
	public int myAccountId;
	public class Message
	{
		public long messageId;
		public int channel;
		public int type;
		public int noticeKind;
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public string text;
		public int refId;
		public int refValue;
		public long createdUnix;
		public int targetAccountId;
		public string targetNickname;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.messageId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.channel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.type = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.noticeKind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort textLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.text = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, textLen);
			count += textLen;
			this.refId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.refValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort targetNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.targetNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, targetNicknameLen);
			count += targetNicknameLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.messageId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.channel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.type), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.noticeKind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string textValue = this.text ?? string.Empty;
			ushort textLen = (ushort)Encoding.Unicode.GetBytes(textValue, 0, textValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(textLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += textLen;
			Array.Copy(BitConverter.GetBytes(this.refId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.refValue), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string targetNicknameValue = this.targetNickname ?? string.Empty;
			ushort targetNicknameLen = (ushort)Encoding.Unicode.GetBytes(targetNicknameValue, 0, targetNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(targetNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += targetNicknameLen;
			return success;
		}	
	}
	public List<Message> messages = new List<Message>();

	public ushort Protocol { get { return (ushort)PacketID.S_ChatHistory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.myAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.messages.Clear();
		ushort messageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < messageLen; i++)
		{
			Message message = new Message();
			message.Read(segment, ref count);
			messages.Add(message);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ChatHistory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.myAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.messages.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Message message in this.messages)
			message.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_HeadquartersInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_HeadquartersInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_HeadquartersInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_HeadquartersInfo : IPacket
{
	public int renown;
	public class Node
	{
		public int nodeId;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.nodeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.nodeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Node> nodes = new List<Node>();

	public ushort Protocol { get { return (ushort)PacketID.S_HeadquartersInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.renown = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nodes.Clear();
		ushort nodeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < nodeLen; i++)
		{
			Node node = new Node();
			node.Read(segment, ref count);
			nodes.Add(node);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_HeadquartersInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.renown), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.nodes.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Node node in this.nodes)
			node.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UpgradeHqNode : IPacket
{
	public int nodeId;

	public ushort Protocol { get { return (ushort)PacketID.C_UpgradeHqNode; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.nodeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UpgradeHqNode), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.nodeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UpgradeHqNodeResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int nodeId;
	public int level;
	public int newRenown;

	public ushort Protocol { get { return (ushort)PacketID.S_UpgradeHqNodeResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nodeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newRenown = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UpgradeHqNodeResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nodeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newRenown), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ItemInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ItemInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ItemInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ItemInfo : IPacket
{
	public class Item
	{
		public int itemId;
		public int count;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Item> items = new List<Item>();

	public ushort Protocol { get { return (ushort)PacketID.S_ItemInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.items.Clear();
		ushort itemLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < itemLen; i++)
		{
			Item item = new Item();
			item.Read(segment, ref count);
			items.Add(item);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ItemInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.items.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Item item in this.items)
			item.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UseItem : IPacket
{
	public int itemId;
	public int charDataId;
	public int count;

	public ushort Protocol { get { return (ushort)PacketID.C_UseItem; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UseItem), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UseItemResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int itemId;
	public int newCount;
	public int charDataId;
	public long gainedExp;
	public int newLevel;
	public int newExp;
	public int maxExp;
	public int gainedStatPoints;
	public bool hasCharacter;
	public int starLevel;
	public int statPoints;
	public int shards;
	public int invAttack;
	public int invDefense;
	public int invHealth;
	public int invSkillAccel;
	public int invAttackSpeed;
	public int invMoveSpeed;
	public int autoStatType;
	public bool limitBroken;
	public long extractedExp;
	public class GrantedItem
	{
		public int itemId;
		public int count;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<GrantedItem> grantedItems = new List<GrantedItem>();
	public int refundShards;
	public int refundDiamond;
	public int refundLimitBreakTicket;
	public int newDiamond;
	public int newLimitBreakTicket;

	public ushort Protocol { get { return (ushort)PacketID.S_UseItemResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedExp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hasCharacter = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.statPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.autoStatType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.extractedExp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.grantedItems.Clear();
		ushort grantedItemLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedItemLen; i++)
		{
			GrantedItem grantedItem = new GrantedItem();
			grantedItem.Read(segment, ref count);
			grantedItems.Add(grantedItem);
		}
		this.refundShards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.refundDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.refundLimitBreakTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newLimitBreakTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UseItemResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hasCharacter), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.statPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.autoStatType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.extractedExp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedItems.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedItem grantedItem in this.grantedItems)
			grantedItem.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes(this.refundShards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.refundDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.refundLimitBreakTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newLimitBreakTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ItemUsePreview : IPacket
{
	public int itemId;
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_ItemUsePreview; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ItemUsePreview), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ItemUsePreview : IPacket
{
	public int failReason;
	public int itemId;
	public int charDataId;
	public long value;
	public int currentLevel;
	public int resultLevel;
	public class GrantItem
	{
		public int itemId;
		public int count;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.count = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<GrantItem> grantItems = new List<GrantItem>();
	public int refundShards;
	public int refundDiamond;
	public int refundLimitBreakTicket;
	public int currentStar;

	public ushort Protocol { get { return (ushort)PacketID.S_ItemUsePreview; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.itemId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.value = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.currentLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.resultLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantItems.Clear();
		ushort grantItemLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantItemLen; i++)
		{
			GrantItem grantItem = new GrantItem();
			grantItem.Read(segment, ref count);
			grantItems.Add(grantItem);
		}
		this.refundShards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.refundDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.refundLimitBreakTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.currentStar = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ItemUsePreview), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.itemId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.currentLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.resultLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantItems.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantItem grantItem in this.grantItems)
			grantItem.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes(this.refundShards), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.refundDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.refundLimitBreakTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.currentStar), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DeckPresets : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DeckPresets; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DeckPresets), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DeckPresets : IPacket
{
	public class Preset
	{
		public int slot;
		public int representativeCharId;
		public string charIdsCsv;
		public string presetName;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort charIdsCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.charIdsCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, charIdsCsvLen);
			count += charIdsCsvLen;
			ushort presetNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.presetName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, presetNameLen);
			count += presetNameLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string charIdsCsvValue = this.charIdsCsv ?? string.Empty;
			ushort charIdsCsvLen = (ushort)Encoding.Unicode.GetBytes(charIdsCsvValue, 0, charIdsCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(charIdsCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += charIdsCsvLen;
			string presetNameValue = this.presetName ?? string.Empty;
			ushort presetNameLen = (ushort)Encoding.Unicode.GetBytes(presetNameValue, 0, presetNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(presetNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += presetNameLen;
			return success;
		}	
	}
	public List<Preset> presets = new List<Preset>();

	public ushort Protocol { get { return (ushort)PacketID.S_DeckPresets; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.presets.Clear();
		ushort presetLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < presetLen; i++)
		{
			Preset preset = new Preset();
			preset.Read(segment, ref count);
			presets.Add(preset);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DeckPresets), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.presets.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Preset preset in this.presets)
			preset.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SaveDeckPreset : IPacket
{
	public int slot;

	public ushort Protocol { get { return (ushort)PacketID.C_SaveDeckPreset; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SaveDeckPreset), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ApplyDeckPreset : IPacket
{
	public int slot;

	public ushort Protocol { get { return (ushort)PacketID.C_ApplyDeckPreset; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ApplyDeckPreset), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RenameDeckPreset : IPacket
{
	public int slot;
	public string presetName;

	public ushort Protocol { get { return (ushort)PacketID.C_RenameDeckPreset; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort presetNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.presetName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, presetNameLen);
		count += presetNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RenameDeckPreset), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string presetNameValue = this.presetName ?? string.Empty;
		ushort presetNameLen = (ushort)Encoding.Unicode.GetBytes(presetNameValue, 0, presetNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(presetNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += presetNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DeckPresetResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int action;
	public int slot;
	public int representativeCharId;
	public string charIdsCsv;
	public string presetName;

	public ushort Protocol { get { return (ushort)PacketID.S_DeckPresetResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort charIdsCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.charIdsCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, charIdsCsvLen);
		count += charIdsCsvLen;
		ushort presetNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.presetName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, presetNameLen);
		count += presetNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DeckPresetResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string charIdsCsvValue = this.charIdsCsv ?? string.Empty;
		ushort charIdsCsvLen = (ushort)Encoding.Unicode.GetBytes(charIdsCsvValue, 0, charIdsCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(charIdsCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += charIdsCsvLen;
		string presetNameValue = this.presetName ?? string.Empty;
		ushort presetNameLen = (ushort)Encoding.Unicode.GetBytes(presetNameValue, 0, presetNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(presetNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += presetNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildHome : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_GuildHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildHome : IPacket
{
	public int guildId;
	public int myRole;
	public int pendingApplications;
	public long myCombatPower;
	public class AppliedGuild
	{
		public int guildId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<AppliedGuild> appliedGuilds = new List<AppliedGuild>();
	public class Recommend
	{
		public int guildId;
		public string guildName;
		public int markId;
		public int level;
		public long combatPower;
		public int memberCount;
		public int joinType;
		public string intro;
		public string masterNickname;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
			count += guildNameLen;
			this.markId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.memberCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.joinType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort introLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.intro = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, introLen);
			count += introLen;
			ushort masterNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.masterNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, masterNicknameLen);
			count += masterNicknameLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string guildNameValue = this.guildName ?? string.Empty;
			ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += guildNameLen;
			Array.Copy(BitConverter.GetBytes(this.markId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.memberCount), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.joinType), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string introValue = this.intro ?? string.Empty;
			ushort introLen = (ushort)Encoding.Unicode.GetBytes(introValue, 0, introValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(introLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += introLen;
			string masterNicknameValue = this.masterNickname ?? string.Empty;
			ushort masterNicknameLen = (ushort)Encoding.Unicode.GetBytes(masterNicknameValue, 0, masterNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(masterNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += masterNicknameLen;
			return success;
		}	
	}
	public List<Recommend> recommends = new List<Recommend>();
	public string guildName;
	public int markId;
	public int level;
	public long combatPower;
	public int memberCount;
	public int joinType;
	public string intro;
	public string notice;
	public string masterNickname;
	public long createdUnix;
	public int expInLevel;
	public int expPerLevel;
	public int raidFreeEntries;
	public class UnlockedMark
	{
		public int markId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.markId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.markId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<UnlockedMark> unlockedMarks = new List<UnlockedMark>();
	public class Member
	{
		public int accountId;
		public string nickname;
		public int role;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public long joinedUnix;
		public int contribution;
		public long lastLoginUnix;
		public bool online;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.role = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.joinedUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.contribution = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.lastLoginUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.online = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.role), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.joinedUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.contribution), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.lastLoginUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.online), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Member> members = new List<Member>();

	public ushort Protocol { get { return (ushort)PacketID.S_GuildHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myRole = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.pendingApplications = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myCombatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.appliedGuilds.Clear();
		ushort appliedGuildLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < appliedGuildLen; i++)
		{
			AppliedGuild appliedGuild = new AppliedGuild();
			appliedGuild.Read(segment, ref count);
			appliedGuilds.Add(appliedGuild);
		}
		this.recommends.Clear();
		ushort recommendLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < recommendLen; i++)
		{
			Recommend recommend = new Recommend();
			recommend.Read(segment, ref count);
			recommends.Add(recommend);
		}
		ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
		count += guildNameLen;
		this.markId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.memberCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.joinType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort introLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.intro = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, introLen);
		count += introLen;
		ushort noticeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.notice = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, noticeLen);
		count += noticeLen;
		ushort masterNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.masterNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, masterNicknameLen);
		count += masterNicknameLen;
		this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.expInLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.expPerLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.raidFreeEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.unlockedMarks.Clear();
		ushort unlockedMarkLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < unlockedMarkLen; i++)
		{
			UnlockedMark unlockedMark = new UnlockedMark();
			unlockedMark.Read(segment, ref count);
			unlockedMarks.Add(unlockedMark);
		}
		this.members.Clear();
		ushort memberLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < memberLen; i++)
		{
			Member member = new Member();
			member.Read(segment, ref count);
			members.Add(member);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myRole), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.pendingApplications), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myCombatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.appliedGuilds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (AppliedGuild appliedGuild in this.appliedGuilds)
			appliedGuild.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.recommends.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Recommend recommend in this.recommends)
			recommend.Write(segment, ref count);
		string guildNameValue = this.guildName ?? string.Empty;
		ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += guildNameLen;
		Array.Copy(BitConverter.GetBytes(this.markId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.memberCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.joinType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string introValue = this.intro ?? string.Empty;
		ushort introLen = (ushort)Encoding.Unicode.GetBytes(introValue, 0, introValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(introLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += introLen;
		string noticeValue = this.notice ?? string.Empty;
		ushort noticeLen = (ushort)Encoding.Unicode.GetBytes(noticeValue, 0, noticeValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(noticeLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += noticeLen;
		string masterNicknameValue = this.masterNickname ?? string.Empty;
		ushort masterNicknameLen = (ushort)Encoding.Unicode.GetBytes(masterNicknameValue, 0, masterNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(masterNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += masterNicknameLen;
		Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.expInLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.expPerLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.raidFreeEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.unlockedMarks.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (UnlockedMark unlockedMark in this.unlockedMarks)
			unlockedMark.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.members.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Member member in this.members)
			member.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildList : IPacket
{
	public int mode;
	public string keyword;
	public int guildId;

	public ushort Protocol { get { return (ushort)PacketID.C_GuildList; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.mode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort keywordLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.keyword = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, keywordLen);
		count += keywordLen;
		this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildList), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.mode), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string keywordValue = this.keyword ?? string.Empty;
		ushort keywordLen = (ushort)Encoding.Unicode.GetBytes(keywordValue, 0, keywordValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(keywordLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += keywordLen;
		Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildList : IPacket
{
	public int mode;
	public class Guild
	{
		public int guildId;
		public string guildName;
		public int markId;
		public int level;
		public long combatPower;
		public int memberCount;
		public int joinType;
		public string intro;
		public string masterNickname;
		public long createdUnix;
		public int rank;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
			count += guildNameLen;
			this.markId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.memberCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.joinType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort introLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.intro = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, introLen);
			count += introLen;
			ushort masterNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.masterNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, masterNicknameLen);
			count += masterNicknameLen;
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string guildNameValue = this.guildName ?? string.Empty;
			ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += guildNameLen;
			Array.Copy(BitConverter.GetBytes(this.markId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.memberCount), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.joinType), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string introValue = this.intro ?? string.Empty;
			ushort introLen = (ushort)Encoding.Unicode.GetBytes(introValue, 0, introValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(introLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += introLen;
			string masterNicknameValue = this.masterNickname ?? string.Empty;
			ushort masterNicknameLen = (ushort)Encoding.Unicode.GetBytes(masterNicknameValue, 0, masterNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(masterNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += masterNicknameLen;
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Guild> guilds = new List<Guild>();

	public ushort Protocol { get { return (ushort)PacketID.S_GuildList; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.mode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.guilds.Clear();
		ushort guildLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < guildLen; i++)
		{
			Guild guild = new Guild();
			guild.Read(segment, ref count);
			guilds.Add(guild);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildList), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.mode), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.guilds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Guild guild in this.guilds)
			guild.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildNameCheck : IPacket
{
	public string guildName;

	public ushort Protocol { get { return (ushort)PacketID.C_GuildNameCheck; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
		count += guildNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildNameCheck), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string guildNameValue = this.guildName ?? string.Empty;
		ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += guildNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildNameCheck : IPacket
{
	public string guildName;
	public bool available;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_GuildNameCheck; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
		count += guildNameLen;
		this.available = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildNameCheck), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string guildNameValue = this.guildName ?? string.Empty;
		ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += guildNameLen;
		Array.Copy(BitConverter.GetBytes(this.available), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CreateGuild : IPacket
{
	public string guildName;
	public int joinType;
	public long combatPower;

	public ushort Protocol { get { return (ushort)PacketID.C_CreateGuild; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
		count += guildNameLen;
		this.joinType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CreateGuild), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string guildNameValue = this.guildName ?? string.Empty;
		ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += guildNameLen;
		Array.Copy(BitConverter.GetBytes(this.joinType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildAction : IPacket
{
	public int action;
	public int guildId;
	public int targetAccountId;
	public int intValue;
	public string textValue;
	public long combatPower;

	public ushort Protocol { get { return (ushort)PacketID.C_GuildAction; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.intValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort textValueLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.textValue = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, textValueLen);
		count += textValueLen;
		this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildAction), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.intValue), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string textValueValue = this.textValue ?? string.Empty;
		ushort textValueLen = (ushort)Encoding.Unicode.GetBytes(textValueValue, 0, textValueValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(textValueLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += textValueLen;
		Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildActionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int action;
	public int guildId;
	public int targetAccountId;
	public int newDiamond;

	public ushort Protocol { get { return (ushort)PacketID.S_GuildActionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildActionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildApplications : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_GuildApplications; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildApplications), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildApplications : IPacket
{
	public class Application
	{
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public long appliedUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.appliedUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.appliedUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Application> applications = new List<Application>();

	public ushort Protocol { get { return (ushort)PacketID.S_GuildApplications; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.applications.Clear();
		ushort applicationLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < applicationLen; i++)
		{
			Application application = new Application();
			application.Read(segment, ref count);
			applications.Add(application);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildApplications), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.applications.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Application application in this.applications)
			application.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReportCombatPower : IPacket
{
	public long combatPower;

	public ushort Protocol { get { return (ushort)PacketID.C_ReportCombatPower; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReportCombatPower), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UserProfile : IPacket
{
	public int accountId;

	public ushort Protocol { get { return (ushort)PacketID.C_UserProfile; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UserProfile), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UserProfile : IPacket
{
	public int accountId;
	public string nickname;
	public int profileFrameId;
	public int representativeCharId;
	public int representativeCostumeId;
	public long combatPower;
	public int highestClearedDungeonIndex;
	public long createdUnix;
	public string guildName;
	public int guildMarkId;
	public int guildRole;
	public int relation;
	public bool blocked;
	public long achievementScore;
	public int combatPowerRank;
	public int achievementRank;
	public bool canWhisper;
	public class DeckMember
	{
		public int charDataId;
		public int starLevel;
		public int level;
		public int costumeId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<DeckMember> deckMembers = new List<DeckMember>();

	public ushort Protocol { get { return (ushort)PacketID.S_UserProfile; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.highestClearedDungeonIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		ushort guildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.guildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, guildNameLen);
		count += guildNameLen;
		this.guildMarkId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.guildRole = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.relation = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.blocked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.achievementScore = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.combatPowerRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.achievementRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canWhisper = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.deckMembers.Clear();
		ushort deckMemberLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < deckMemberLen; i++)
		{
			DeckMember deckMember = new DeckMember();
			deckMember.Read(segment, ref count);
			deckMembers.Add(deckMember);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UserProfile), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.highestClearedDungeonIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		string guildNameValue = this.guildName ?? string.Empty;
		ushort guildNameLen = (ushort)Encoding.Unicode.GetBytes(guildNameValue, 0, guildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(guildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += guildNameLen;
		Array.Copy(BitConverter.GetBytes(this.guildMarkId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.guildRole), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.relation), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.blocked), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.achievementScore), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.combatPowerRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.achievementRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canWhisper), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.deckMembers.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (DeckMember deckMember in this.deckMembers)
			deckMember.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Achievements : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_Achievements; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Achievements), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Achievements : IPacket
{
	public long totalScore;
	public class Entry
	{
		public int metric;
		public int category;
		public long count;
		public int points;
		public long score;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.metric = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.category = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.count = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.score = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.metric), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.category), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.count), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();
	public class RewardTier
	{
		public int tier;
		public long threshold;
		public bool claimed;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.threshold = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.claimed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.threshold), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.claimed), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<RewardTier> rewardTiers = new List<RewardTier>();
	public class RewardGrant
	{
		public int tier;
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<RewardGrant> rewardGrants = new List<RewardGrant>();

	public ushort Protocol { get { return (ushort)PacketID.S_Achievements; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.totalScore = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
		this.rewardTiers.Clear();
		ushort rewardTierLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < rewardTierLen; i++)
		{
			RewardTier rewardTier = new RewardTier();
			rewardTier.Read(segment, ref count);
			rewardTiers.Add(rewardTier);
		}
		this.rewardGrants.Clear();
		ushort rewardGrantLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < rewardGrantLen; i++)
		{
			RewardGrant rewardGrant = new RewardGrant();
			rewardGrant.Read(segment, ref count);
			rewardGrants.Add(rewardGrant);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Achievements), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.totalScore), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.rewardTiers.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (RewardTier rewardTier in this.rewardTiers)
			rewardTier.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.rewardGrants.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (RewardGrant rewardGrant in this.rewardGrants)
			rewardGrant.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimAchievementReward : IPacket
{
	public int tier;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimAchievementReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimAchievementReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimAchievementRewardResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int tier;
	public int newDiamond;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimAchievementRewardResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimAchievementRewardResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Ranking : IPacket
{
	public int category;

	public ushort Protocol { get { return (ushort)PacketID.C_Ranking; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.category = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Ranking), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.category), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Ranking : IPacket
{
	public int category;
	public long updatedUnix;
	public int myRank;
	public long myScore;
	public class Entry
	{
		public int rank;
		public int targetId;
		public long score;
		public string name;
		public string subName;
		public int iconId;
		public int subIconId;
		public int representativeCharId;
		public int representativeCostumeId;
		public int subValue;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.targetId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.score = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			ushort nameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.name = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nameLen);
			count += nameLen;
			ushort subNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.subName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, subNameLen);
			count += subNameLen;
			this.iconId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.subIconId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.subValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.targetId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			string nameValue = this.name ?? string.Empty;
			ushort nameLen = (ushort)Encoding.Unicode.GetBytes(nameValue, 0, nameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nameLen;
			string subNameValue = this.subName ?? string.Empty;
			ushort subNameLen = (ushort)Encoding.Unicode.GetBytes(subNameValue, 0, subNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(subNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += subNameLen;
			Array.Copy(BitConverter.GetBytes(this.iconId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.subIconId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.subValue), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();
	public bool hasMine;
	public string myName;
	public string mySubName;
	public int myIconId;
	public int mySubIconId;
	public int myRepresentativeCharId;
	public int myRepresentativeCostumeId;
	public int mySubValue;

	public ushort Protocol { get { return (ushort)PacketID.S_Ranking; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.category = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.updatedUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.myRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myScore = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
		this.hasMine = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort myNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.myName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, myNameLen);
		count += myNameLen;
		ushort mySubNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.mySubName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, mySubNameLen);
		count += mySubNameLen;
		this.myIconId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.mySubIconId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myRepresentativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myRepresentativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.mySubValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Ranking), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.category), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.updatedUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.myRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myScore), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes(this.hasMine), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		string myNameValue = this.myName ?? string.Empty;
		ushort myNameLen = (ushort)Encoding.Unicode.GetBytes(myNameValue, 0, myNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(myNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += myNameLen;
		string mySubNameValue = this.mySubName ?? string.Empty;
		ushort mySubNameLen = (ushort)Encoding.Unicode.GetBytes(mySubNameValue, 0, mySubNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(mySubNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += mySubNameLen;
		Array.Copy(BitConverter.GetBytes(this.myIconId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.mySubIconId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myRepresentativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myRepresentativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.mySubValue), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FriendHome : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_FriendHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FriendHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FriendHome : IPacket
{
	public int maxFriends;
	public int staminaSendable;
	public int staminaClaimable;
	public class Friend
	{
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public bool online;
		public bool staminaSentToMe;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.online = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.staminaSentToMe = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.online), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.staminaSentToMe), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Friend> friends = new List<Friend>();
	public class Request
	{
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public long createdUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Request> requests = new List<Request>();
	public class Blocked
	{
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public long createdUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Blocked> blockeds = new List<Blocked>();

	public ushort Protocol { get { return (ushort)PacketID.S_FriendHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.maxFriends = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaSendable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaClaimable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.friends.Clear();
		ushort friendLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < friendLen; i++)
		{
			Friend friend = new Friend();
			friend.Read(segment, ref count);
			friends.Add(friend);
		}
		this.requests.Clear();
		ushort requestLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < requestLen; i++)
		{
			Request request = new Request();
			request.Read(segment, ref count);
			requests.Add(request);
		}
		this.blockeds.Clear();
		ushort blockedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < blockedLen; i++)
		{
			Blocked blocked = new Blocked();
			blocked.Read(segment, ref count);
			blockeds.Add(blocked);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FriendHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.maxFriends), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaSendable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaClaimable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.friends.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Friend friend in this.friends)
			friend.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.requests.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Request request in this.requests)
			request.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.blockeds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Blocked blocked in this.blockeds)
			blocked.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SearchFriend : IPacket
{
	public string nickname;

	public ushort Protocol { get { return (ushort)PacketID.C_SearchFriend; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SearchFriend), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SearchFriendResult : IPacket
{
	public int failReason;
	public class User
	{
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public bool online;
		public int relation;
		public bool blocked;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.online = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.relation = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.blocked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.online), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.relation), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.blocked), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<User> users = new List<User>();

	public ushort Protocol { get { return (ushort)PacketID.S_SearchFriendResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.users.Clear();
		ushort userLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < userLen; i++)
		{
			User user = new User();
			user.Read(segment, ref count);
			users.Add(user);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SearchFriendResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.users.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (User user in this.users)
			user.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FriendAction : IPacket
{
	public int action;
	public int targetAccountId;

	public ushort Protocol { get { return (ushort)PacketID.C_FriendAction; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FriendAction), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FriendActionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int action;
	public int targetAccountId;
	public int relation;
	public bool blocked;
	public int pendingRequests;

	public ushort Protocol { get { return (ushort)PacketID.S_FriendActionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.relation = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.blocked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.pendingRequests = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FriendActionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.relation), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.blocked), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.pendingRequests), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FriendNotify : IPacket
{
	public int pendingRequests;
	public int staminaSendable;
	public int staminaClaimable;

	public ushort Protocol { get { return (ushort)PacketID.S_FriendNotify; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.pendingRequests = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaSendable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaClaimable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FriendNotify), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.pendingRequests), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaSendable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaClaimable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FriendStaminaSend : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_FriendStaminaSend; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FriendStaminaSend), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FriendStaminaSendResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int sentCount;
	public int staminaSendable;
	public int staminaClaimable;

	public ushort Protocol { get { return (ushort)PacketID.S_FriendStaminaSendResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.sentCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaSendable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaClaimable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FriendStaminaSendResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.sentCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaSendable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaClaimable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FriendStaminaClaim : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_FriendStaminaClaim; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FriendStaminaClaim), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FriendStaminaClaimResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int claimedStamina;
	public int newStamina;
	public int staminaSendable;
	public int staminaClaimable;

	public ushort Protocol { get { return (ushort)PacketID.S_FriendStaminaClaimResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaSendable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaClaimable = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FriendStaminaClaimResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaSendable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaClaimable), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildRaidState : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_GuildRaidState; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildRaidState), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildRaidState : IPacket
{
	public bool hasGuild;
	public int bossLevel;
	public long bossHp;
	public long bossMaxHp;
	public int myEntries;
	public int maxEntries;
	public int nextEntryCost;
	public long myDamageToday;
	public long resetUnix;
	public class Contribution
	{
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long damage;
		public int entries;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.damage = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.entries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.damage), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.entries), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Contribution> contributions = new List<Contribution>();
	public string occupantNickname;

	public ushort Protocol { get { return (ushort)PacketID.S_GuildRaidState; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.hasGuild = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.bossLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bossHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.bossMaxHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.myEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nextEntryCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myDamageToday = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.resetUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.contributions.Clear();
		ushort contributionLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < contributionLen; i++)
		{
			Contribution contribution = new Contribution();
			contribution.Read(segment, ref count);
			contributions.Add(contribution);
		}
		ushort occupantNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.occupantNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, occupantNicknameLen);
		count += occupantNicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildRaidState), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.hasGuild), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.bossLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bossHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.bossMaxHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.myEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nextEntryCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myDamageToday), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.resetUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.contributions.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Contribution contribution in this.contributions)
			contribution.Write(segment, ref count);
		string occupantNicknameValue = this.occupantNickname ?? string.Empty;
		ushort occupantNicknameLen = (ushort)Encoding.Unicode.GetBytes(occupantNicknameValue, 0, occupantNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(occupantNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += occupantNicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EnterGuildRaid : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EnterGuildRaid; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EnterGuildRaid), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EnterGuildRaidResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int bossLevel;
	public long bossHp;
	public long bossMaxHp;
	public int newDiamond;
	public string occupantNickname;

	public ushort Protocol { get { return (ushort)PacketID.S_EnterGuildRaidResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bossLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bossHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.bossMaxHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort occupantNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.occupantNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, occupantNicknameLen);
		count += occupantNicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EnterGuildRaidResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bossLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bossHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.bossMaxHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string occupantNicknameValue = this.occupantNickname ?? string.Empty;
		ushort occupantNicknameLen = (ushort)Encoding.Unicode.GetBytes(occupantNicknameValue, 0, occupantNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(occupantNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += occupantNicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReportGuildRaidDamage : IPacket
{
	public long damage;

	public ushort Protocol { get { return (ushort)PacketID.C_ReportGuildRaidDamage; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.damage = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReportGuildRaidDamage), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.damage), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildRaidDamageResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int bossLevel;
	public long bossHp;
	public long bossMaxHp;
	public bool bossDefeated;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();
	public int defeatedCount;

	public ushort Protocol { get { return (ushort)PacketID.S_GuildRaidDamageResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bossLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bossHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.bossMaxHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.bossDefeated = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
		this.defeatedCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildRaidDamageResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bossLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bossHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.bossMaxHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.bossDefeated), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes(this.defeatedCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildWarState : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_GuildWarState; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildWarState), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildWarState : IPacket
{
	public int phase;
	public int matchId;
	public long battleStartUnix;
	public long battleEndUnix;
	public long nextMatchUnix;
	public int noMatchReason;
	public int myAttacksLeft;
	public bool myDead;
	public int myAccountId;
	public int winnerGuildId;
	public int myGuildId;
	public string myGuildName;
	public int myMarkId;
	public int myAliveCount;
	public int myTotalCount;
	public int enemyGuildId;
	public string enemyGuildName;
	public int enemyMarkId;
	public int enemyAliveCount;
	public int enemyTotalCount;
	public class Participant
	{
		public int guildId;
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int representativeCharId;
		public int representativeCostumeId;
		public long combatPower;
		public bool dead;
		public int attacksUsed;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.guildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.dead = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.attacksUsed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.guildId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.dead), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.attacksUsed), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Participant> participants = new List<Participant>();

	public ushort Protocol { get { return (ushort)PacketID.S_GuildWarState; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.phase = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.matchId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.battleStartUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.battleEndUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.nextMatchUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.noMatchReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myAttacksLeft = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myDead = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.myAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.winnerGuildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myGuildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort myGuildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.myGuildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, myGuildNameLen);
		count += myGuildNameLen;
		this.myMarkId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myAliveCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.myTotalCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.enemyGuildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort enemyGuildNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.enemyGuildName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, enemyGuildNameLen);
		count += enemyGuildNameLen;
		this.enemyMarkId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.enemyAliveCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.enemyTotalCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.participants.Clear();
		ushort participantLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < participantLen; i++)
		{
			Participant participant = new Participant();
			participant.Read(segment, ref count);
			participants.Add(participant);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildWarState), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.phase), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.matchId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.battleStartUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.battleEndUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.nextMatchUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.noMatchReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myAttacksLeft), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myDead), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.myAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.winnerGuildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myGuildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string myGuildNameValue = this.myGuildName ?? string.Empty;
		ushort myGuildNameLen = (ushort)Encoding.Unicode.GetBytes(myGuildNameValue, 0, myGuildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(myGuildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += myGuildNameLen;
		Array.Copy(BitConverter.GetBytes(this.myMarkId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myAliveCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.myTotalCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.enemyGuildId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string enemyGuildNameValue = this.enemyGuildName ?? string.Empty;
		ushort enemyGuildNameLen = (ushort)Encoding.Unicode.GetBytes(enemyGuildNameValue, 0, enemyGuildNameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(enemyGuildNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += enemyGuildNameLen;
		Array.Copy(BitConverter.GetBytes(this.enemyMarkId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.enemyAliveCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.enemyTotalCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.participants.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Participant participant in this.participants)
			participant.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_BeginGuildWarAttack : IPacket
{
	public int targetAccountId;

	public ushort Protocol { get { return (ushort)PacketID.C_BeginGuildWarAttack; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_BeginGuildWarAttack), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BeginGuildWarAttackResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int targetAccountId;
	public int attacksLeft;

	public ushort Protocol { get { return (ushort)PacketID.S_BeginGuildWarAttackResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.attacksLeft = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BeginGuildWarAttackResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.attacksLeft), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReportGuildWarAttack : IPacket
{
	public int targetAccountId;
	public bool win;

	public ushort Protocol { get { return (ushort)PacketID.C_ReportGuildWarAttack; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.win = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReportGuildWarAttack), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.win), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildWarAttackResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int targetAccountId;
	public bool targetDead;
	public int myAliveCount;
	public int enemyAliveCount;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_GuildWarAttackResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetDead = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.myAliveCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.enemyAliveCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildWarAttackResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetDead), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.myAliveCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.enemyAliveCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_GuildWarLog : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_GuildWarLog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_GuildWarLog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GuildWarLog : IPacket
{
	public class Entry
	{
		public int attackerAccountId;
		public string attackerNickname;
		public int attackerGuildId;
		public int targetAccountId;
		public string targetNickname;
		public int targetGuildId;
		public bool attackerWon;
		public long createdUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.attackerAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort attackerNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.attackerNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, attackerNicknameLen);
			count += attackerNicknameLen;
			this.attackerGuildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort targetNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.targetNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, targetNicknameLen);
			count += targetNicknameLen;
			this.targetGuildId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.attackerWon = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.attackerAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string attackerNicknameValue = this.attackerNickname ?? string.Empty;
			ushort attackerNicknameLen = (ushort)Encoding.Unicode.GetBytes(attackerNicknameValue, 0, attackerNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(attackerNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += attackerNicknameLen;
			Array.Copy(BitConverter.GetBytes(this.attackerGuildId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string targetNicknameValue = this.targetNickname ?? string.Empty;
			ushort targetNicknameLen = (ushort)Encoding.Unicode.GetBytes(targetNicknameValue, 0, targetNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(targetNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += targetNicknameLen;
			Array.Copy(BitConverter.GetBytes(this.targetGuildId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.attackerWon), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();

	public ushort Protocol { get { return (ushort)PacketID.S_GuildWarLog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GuildWarLog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UserDeck : IPacket
{
	public int accountId;

	public ushort Protocol { get { return (ushort)PacketID.C_UserDeck; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UserDeck), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UserDeck : IPacket
{
	public int accountId;
	public string nickname;
	public int representativeCharId;
	public int representativeCostumeId;
	public int profileFrameId;
	public class Member
	{
		public int charDataId;
		public int level;
		public int starLevel;
		public int invAttack;
		public int invDefense;
		public int invHealth;
		public int invAttackSpeed;
		public int invMoveSpeed;
		public int invSkillAccel;
		public int costumeId;
		public int unitSkinId;
		public bool limitBroken;
		public int shards;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invAttack = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invDefense = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invHealth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invAttackSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invMoveSpeed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.invSkillAccel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.unitSkinId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.shards = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invAttack), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invDefense), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invHealth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invAttackSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invMoveSpeed), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.invSkillAccel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.unitSkinId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.shards), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Member> members = new List<Member>();
	public class Equip
	{
		public int charDataId;
		public int equipDataId;
		public int starLevel;
		public int level;
		public int mainPoints;
		public int opt1Points;
		public int opt2Points;
		public int opt3Points;
		public int opt4Points;
		public bool limitBroken;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.equipDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.mainPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt1Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt2Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt3Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt4Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.equipDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.mainPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt1Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt2Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt3Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt4Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Equip> equips = new List<Equip>();
	public class EngravingSlot
	{
		public int charDataId;
		public int slotIndex;
		public int engravingId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<EngravingSlot> engravingSlots = new List<EngravingSlot>();
	public class EngravingLevel
	{
		public int engravingId;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.engravingId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.engravingId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<EngravingLevel> engravingLevels = new List<EngravingLevel>();
	public class HqNode
	{
		public int nodeId;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.nodeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.nodeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<HqNode> hqNodes = new List<HqNode>();
	public class Training
	{
		public int charDataId;
		public int effect;
		public int valueTenth;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.effect = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.valueTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.effect), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.valueTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Training> trainings = new List<Training>();
	public class Investment
	{
		public int slotIndex;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Investment> investments = new List<Investment>();

	public ushort Protocol { get { return (ushort)PacketID.S_UserDeck; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
		count += nicknameLen;
		this.representativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.representativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.members.Clear();
		ushort memberLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < memberLen; i++)
		{
			Member member = new Member();
			member.Read(segment, ref count);
			members.Add(member);
		}
		this.equips.Clear();
		ushort equipLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equipLen; i++)
		{
			Equip equip = new Equip();
			equip.Read(segment, ref count);
			equips.Add(equip);
		}
		this.engravingSlots.Clear();
		ushort engravingSlotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < engravingSlotLen; i++)
		{
			EngravingSlot engravingSlot = new EngravingSlot();
			engravingSlot.Read(segment, ref count);
			engravingSlots.Add(engravingSlot);
		}
		this.engravingLevels.Clear();
		ushort engravingLevelLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < engravingLevelLen; i++)
		{
			EngravingLevel engravingLevel = new EngravingLevel();
			engravingLevel.Read(segment, ref count);
			engravingLevels.Add(engravingLevel);
		}
		this.hqNodes.Clear();
		ushort hqNodeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < hqNodeLen; i++)
		{
			HqNode hqNode = new HqNode();
			hqNode.Read(segment, ref count);
			hqNodes.Add(hqNode);
		}
		this.trainings.Clear();
		ushort trainingLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < trainingLen; i++)
		{
			Training training = new Training();
			training.Read(segment, ref count);
			trainings.Add(training);
		}
		this.investments.Clear();
		ushort investmentLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < investmentLen; i++)
		{
			Investment investment = new Investment();
			investment.Read(segment, ref count);
			investments.Add(investment);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UserDeck), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string nicknameValue = this.nickname ?? string.Empty;
		ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nicknameLen;
		Array.Copy(BitConverter.GetBytes(this.representativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.representativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.members.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Member member in this.members)
			member.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.equips.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Equip equip in this.equips)
			equip.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.engravingSlots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (EngravingSlot engravingSlot in this.engravingSlots)
			engravingSlot.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.engravingLevels.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (EngravingLevel engravingLevel in this.engravingLevels)
			engravingLevel.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.hqNodes.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (HqNode hqNode in this.hqNodes)
			hqNode.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.trainings.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Training training in this.trainings)
			training.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.investments.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Investment investment in this.investments)
			investment.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RedeemCoupon : IPacket
{
	public string code;

	public ushort Protocol { get { return (ushort)PacketID.C_RedeemCoupon; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort codeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.code = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, codeLen);
		count += codeLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RedeemCoupon), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string codeValue = this.code ?? string.Empty;
		ushort codeLen = (ushort)Encoding.Unicode.GetBytes(codeValue, 0, codeValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(codeLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += codeLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_RedeemCouponResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public string code;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_RedeemCouponResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort codeLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.code = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, codeLen);
		count += codeLen;
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_RedeemCouponResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string codeValue = this.code ?? string.Empty;
		ushort codeLen = (ushort)Encoding.Unicode.GetBytes(codeValue, 0, codeValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(codeLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += codeLen;
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ArenaHome : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ArenaHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ArenaHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ArenaHome : IPacket
{
	public int coin;
	public int score;
	public int wins;
	public int losses;
	public int rank;
	public int usedEntries;
	public int freeEntries;
	public int maxEntries;
	public int nextEntryCost;
	public long seasonEndUnix;
	public long lastDefenseLogId;
	public string runesCsv;

	public ushort Protocol { get { return (ushort)PacketID.S_ArenaHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.coin = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.score = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.wins = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.losses = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.usedEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.freeEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nextEntryCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.seasonEndUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.lastDefenseLogId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		ushort runesCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.runesCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, runesCsvLen);
		count += runesCsvLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ArenaHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.coin), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.wins), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.losses), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.usedEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.freeEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nextEntryCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.seasonEndUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.lastDefenseLogId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		string runesCsvValue = this.runesCsv ?? string.Empty;
		ushort runesCsvLen = (ushort)Encoding.Unicode.GetBytes(runesCsvValue, 0, runesCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(runesCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += runesCsvLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ArenaSetRunes : IPacket
{
	public string runesCsv;

	public ushort Protocol { get { return (ushort)PacketID.C_ArenaSetRunes; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort runesCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.runesCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, runesCsvLen);
		count += runesCsvLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ArenaSetRunes), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string runesCsvValue = this.runesCsv ?? string.Empty;
		ushort runesCsvLen = (ushort)Encoding.Unicode.GetBytes(runesCsvValue, 0, runesCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(runesCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += runesCsvLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ArenaSetRunesResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public string runesCsv;

	public ushort Protocol { get { return (ushort)PacketID.S_ArenaSetRunesResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort runesCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.runesCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, runesCsvLen);
		count += runesCsvLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ArenaSetRunesResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string runesCsvValue = this.runesCsv ?? string.Empty;
		ushort runesCsvLen = (ushort)Encoding.Unicode.GetBytes(runesCsvValue, 0, runesCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(runesCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += runesCsvLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ArenaHistory : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ArenaHistory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ArenaHistory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ArenaHistory : IPacket
{
	public class Season
	{
		public string seasonKey;
		public int score;
		public int wins;
		public int losses;
		public int rank;
		public bool isCurrent;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort seasonKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.seasonKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, seasonKeyLen);
			count += seasonKeyLen;
			this.score = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.wins = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.losses = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.isCurrent = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string seasonKeyValue = this.seasonKey ?? string.Empty;
			ushort seasonKeyLen = (ushort)Encoding.Unicode.GetBytes(seasonKeyValue, 0, seasonKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(seasonKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += seasonKeyLen;
			Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.wins), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.losses), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.isCurrent), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Season> seasons = new List<Season>();

	public ushort Protocol { get { return (ushort)PacketID.S_ArenaHistory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.seasons.Clear();
		ushort seasonLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < seasonLen; i++)
		{
			Season season = new Season();
			season.Read(segment, ref count);
			seasons.Add(season);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ArenaHistory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.seasons.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Season season in this.seasons)
			season.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ArenaMatch : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ArenaMatch; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ArenaMatch), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ArenaMatchResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int opponentAccountId;
	public string opponentNickname;
	public long opponentCombatPower;
	public int opponentScore;
	public int opponentProfileFrameId;
	public int opponentRepresentativeCharId;
	public int opponentRepresentativeCostumeId;
	public int newDiamond;
	public int usedEntries;
	public int nextEntryCost;
	public string myRunesCsv;
	public string opponentRunesCsv;

	public ushort Protocol { get { return (ushort)PacketID.S_ArenaMatchResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.opponentAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort opponentNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.opponentNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, opponentNicknameLen);
		count += opponentNicknameLen;
		this.opponentCombatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.opponentScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.opponentProfileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.opponentRepresentativeCharId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.opponentRepresentativeCostumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.usedEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nextEntryCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort myRunesCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.myRunesCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, myRunesCsvLen);
		count += myRunesCsvLen;
		ushort opponentRunesCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.opponentRunesCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, opponentRunesCsvLen);
		count += opponentRunesCsvLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ArenaMatchResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.opponentAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string opponentNicknameValue = this.opponentNickname ?? string.Empty;
		ushort opponentNicknameLen = (ushort)Encoding.Unicode.GetBytes(opponentNicknameValue, 0, opponentNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(opponentNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += opponentNicknameLen;
		Array.Copy(BitConverter.GetBytes(this.opponentCombatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.opponentScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.opponentProfileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.opponentRepresentativeCharId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.opponentRepresentativeCostumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.usedEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nextEntryCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string myRunesCsvValue = this.myRunesCsv ?? string.Empty;
		ushort myRunesCsvLen = (ushort)Encoding.Unicode.GetBytes(myRunesCsvValue, 0, myRunesCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(myRunesCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += myRunesCsvLen;
		string opponentRunesCsvValue = this.opponentRunesCsv ?? string.Empty;
		ushort opponentRunesCsvLen = (ushort)Encoding.Unicode.GetBytes(opponentRunesCsvValue, 0, opponentRunesCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(opponentRunesCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += opponentRunesCsvLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ArenaReport : IPacket
{
	public int targetAccountId;
	public bool win;
	public int hpPercentTenth;

	public ushort Protocol { get { return (ushort)PacketID.C_ArenaReport; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.targetAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.win = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.hpPercentTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ArenaReport), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.targetAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.win), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.hpPercentTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ArenaReportResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public bool win;
	public int scoreDelta;
	public int newScore;
	public int newCoin;
	public int wins;
	public int losses;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_ArenaReportResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.win = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.scoreDelta = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newCoin = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.wins = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.losses = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ArenaReportResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.win), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.scoreDelta), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newCoin), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.wins), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.losses), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ArenaLog : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ArenaLog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ArenaLog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ArenaLog : IPacket
{
	public int myAccountId;
	public class Entry
	{
		public int attackerAccountId;
		public string attackerNickname;
		public int defenderAccountId;
		public string defenderNickname;
		public bool attackerWon;
		public int scoreDelta;
		public long createdUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.attackerAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort attackerNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.attackerNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, attackerNicknameLen);
			count += attackerNicknameLen;
			this.defenderAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort defenderNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.defenderNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, defenderNicknameLen);
			count += defenderNicknameLen;
			this.attackerWon = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.scoreDelta = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.attackerAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string attackerNicknameValue = this.attackerNickname ?? string.Empty;
			ushort attackerNicknameLen = (ushort)Encoding.Unicode.GetBytes(attackerNicknameValue, 0, attackerNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(attackerNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += attackerNicknameLen;
			Array.Copy(BitConverter.GetBytes(this.defenderAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string defenderNicknameValue = this.defenderNickname ?? string.Empty;
			ushort defenderNicknameLen = (ushort)Encoding.Unicode.GetBytes(defenderNicknameValue, 0, defenderNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(defenderNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += defenderNicknameLen;
			Array.Copy(BitConverter.GetBytes(this.attackerWon), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.scoreDelta), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();

	public ushort Protocol { get { return (ushort)PacketID.S_ArenaLog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.myAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ArenaLog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.myAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TrainingInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_TrainingInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TrainingInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_TrainingInfo : IPacket
{
	public int unlockedSlots;
	public int maxSlots;
	public int nextUnlockCost;
	public int durationSeconds;
	public int trainingTicket;
	public int instantTicketCost;
	public class Slot
	{
		public int slotIndex;
		public int charDataId;
		public long startUnix;
		public long endUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Slot> slots = new List<Slot>();
	public class Result
	{
		public int charDataId;
		public int effect;
		public int valueTenth;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.effect = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.valueTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.effect), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.valueTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Result> results = new List<Result>();
	public class Rate
	{
		public int effect;
		public int weight;
		public int totalWeight;
		public int minTenth;
		public int maxTenth;
		public int stepTenth;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.effect = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.weight = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.totalWeight = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.minTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.stepTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.effect), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.weight), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.totalWeight), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.minTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.stepTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Rate> rates = new List<Rate>();

	public ushort Protocol { get { return (ushort)PacketID.S_TrainingInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.unlockedSlots = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxSlots = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nextUnlockCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.durationSeconds = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.trainingTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.instantTicketCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slots.Clear();
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < slotLen; i++)
		{
			Slot slot = new Slot();
			slot.Read(segment, ref count);
			slots.Add(slot);
		}
		this.results.Clear();
		ushort resultLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < resultLen; i++)
		{
			Result result = new Result();
			result.Read(segment, ref count);
			results.Add(result);
		}
		this.rates.Clear();
		ushort rateLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < rateLen; i++)
		{
			Rate rate = new Rate();
			rate.Read(segment, ref count);
			rates.Add(rate);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_TrainingInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.unlockedSlots), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxSlots), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nextUnlockCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.durationSeconds), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.trainingTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.instantTicketCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.slots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Slot slot in this.slots)
			slot.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.results.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Result result in this.results)
			result.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.rates.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Rate rate in this.rates)
			rate.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TrainingAction : IPacket
{
	public int action;
	public int slotIndex;
	public int charDataId;

	public ushort Protocol { get { return (ushort)PacketID.C_TrainingAction; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TrainingAction), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_TrainingActionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int action;
	public int slotIndex;
	public int charDataId;
	public int newDiamond;
	public int newTrainingTicket;
	public int unlockedSlots;
	public long startUnix;
	public long endUnix;
	public int effect;
	public int valueTenth;

	public ushort Protocol { get { return (ushort)PacketID.S_TrainingActionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newTrainingTicket = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.unlockedSlots = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.effect = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.valueTenth = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_TrainingActionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newTrainingTicket), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.unlockedSlots), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.effect), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.valueTenth), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DevTool : IPacket
{
	public int command;
	public long value;

	public ushort Protocol { get { return (ushort)PacketID.C_DevTool; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.command = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.value = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DevTool), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.command), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DevTool : IPacket
{
	public int ok;
	public long serverNowUnix;
	public long offsetSeconds;
	public string message;

	public ushort Protocol { get { return (ushort)PacketID.S_DevTool; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.ok = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.serverNowUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.offsetSeconds = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		ushort messageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.message = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, messageLen);
		count += messageLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DevTool), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.ok), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.serverNowUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.offsetSeconds), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		string messageValue = this.message ?? string.Empty;
		ushort messageLen = (ushort)Encoding.Unicode.GetBytes(messageValue, 0, messageValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(messageLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += messageLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_OfflineReward : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_OfflineReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_OfflineReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_OfflineReward : IPacket
{
	public long settlementId;
	public long offlineSeconds;
	public int rewardMinutes;
	public bool capped;
	public int gold;
	public int stamina;
	public int exp;
	public int investExpPermille;
	public int diamond;
	public bool alreadySettled;
	public int newGold;
	public int newStamina;
	public int newDiamond;
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_OfflineReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.settlementId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.offlineSeconds = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.rewardMinutes = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.capped = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.gold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.investExpPermille = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.diamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.alreadySettled = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_OfflineReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.settlementId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.offlineSeconds), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.rewardMinutes), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.capped), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.gold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.investExpPermille), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.diamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.alreadySettled), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DiamondDetail : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DiamondDetail; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DiamondDetail), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DiamondDetail : IPacket
{
	public int paid;
	public int free;

	public ushort Protocol { get { return (ushort)PacketID.S_DiamondDetail; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.paid = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.free = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DiamondDetail), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.paid), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.free), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RegisterPushToken : IPacket
{
	public string installationId;
	public int platform;
	public string token;
	public int apnsEnvironment;

	public ushort Protocol { get { return (ushort)PacketID.C_RegisterPushToken; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort installationIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.installationId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, installationIdLen);
		count += installationIdLen;
		this.platform = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort tokenLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.token = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, tokenLen);
		count += tokenLen;
		this.apnsEnvironment = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RegisterPushToken), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string installationIdValue = this.installationId ?? string.Empty;
		ushort installationIdLen = (ushort)Encoding.Unicode.GetBytes(installationIdValue, 0, installationIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(installationIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += installationIdLen;
		Array.Copy(BitConverter.GetBytes(this.platform), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string tokenValue = this.token ?? string.Empty;
		ushort tokenLen = (ushort)Encoding.Unicode.GetBytes(tokenValue, 0, tokenValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(tokenLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += tokenLen;
		Array.Copy(BitConverter.GetBytes(this.apnsEnvironment), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UnregisterPushToken : IPacket
{
	public string installationId;

	public ushort Protocol { get { return (ushort)PacketID.C_UnregisterPushToken; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort installationIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.installationId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, installationIdLen);
		count += installationIdLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UnregisterPushToken), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string installationIdValue = this.installationId ?? string.Empty;
		ushort installationIdLen = (ushort)Encoding.Unicode.GetBytes(installationIdValue, 0, installationIdValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(installationIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += installationIdLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_PushTokenResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public bool registered;

	public ushort Protocol { get { return (ushort)PacketID.S_PushTokenResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.registered = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_PushTokenResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.registered), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CommissionAttempt : IPacket
{
	public int commissionType;

	public ushort Protocol { get { return (ushort)PacketID.C_CommissionAttempt; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.commissionType = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CommissionAttempt), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.commissionType), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_StoryProgress : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_StoryProgress; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_StoryProgress), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_StoryProgress : IPacket
{
	public class Story
	{
		public string storyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort storyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.storyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, storyKeyLen);
			count += storyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string storyKeyValue = this.storyKey ?? string.Empty;
			ushort storyKeyLen = (ushort)Encoding.Unicode.GetBytes(storyKeyValue, 0, storyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(storyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += storyKeyLen;
			return success;
		}	
	}
	public List<Story> storys = new List<Story>();
	public class Chapter
	{
		public string storyKey;
		public bool completed;
		public bool rewardClaimed;
		public bool unlocked;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort storyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.storyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, storyKeyLen);
			count += storyKeyLen;
			this.completed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.rewardClaimed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.unlocked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			string storyKeyValue = this.storyKey ?? string.Empty;
			ushort storyKeyLen = (ushort)Encoding.Unicode.GetBytes(storyKeyValue, 0, storyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(storyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += storyKeyLen;
			Array.Copy(BitConverter.GetBytes(this.completed), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.rewardClaimed), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.unlocked), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Chapter> chapters = new List<Chapter>();

	public ushort Protocol { get { return (ushort)PacketID.S_StoryProgress; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.storys.Clear();
		ushort storyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < storyLen; i++)
		{
			Story story = new Story();
			story.Read(segment, ref count);
			storys.Add(story);
		}
		this.chapters.Clear();
		ushort chapterLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < chapterLen; i++)
		{
			Chapter chapter = new Chapter();
			chapter.Read(segment, ref count);
			chapters.Add(chapter);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_StoryProgress), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.storys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Story story in this.storys)
			story.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.chapters.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Chapter chapter in this.chapters)
			chapter.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CompleteStory : IPacket
{
	public string storyKey;

	public ushort Protocol { get { return (ushort)PacketID.C_CompleteStory; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort storyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.storyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, storyKeyLen);
		count += storyKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CompleteStory), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string storyKeyValue = this.storyKey ?? string.Empty;
		ushort storyKeyLen = (ushort)Encoding.Unicode.GetBytes(storyKeyValue, 0, storyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(storyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += storyKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_CompleteStoryResult : IPacket
{
	public bool isSuccess;
	public string storyKey;

	public ushort Protocol { get { return (ushort)PacketID.S_CompleteStoryResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort storyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.storyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, storyKeyLen);
		count += storyKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_CompleteStoryResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		string storyKeyValue = this.storyKey ?? string.Empty;
		ushort storyKeyLen = (ushort)Encoding.Unicode.GetBytes(storyKeyValue, 0, storyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(storyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += storyKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimStoryReward : IPacket
{
	public string storyKey;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimStoryReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort storyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.storyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, storyKeyLen);
		count += storyKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimStoryReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string storyKeyValue = this.storyKey ?? string.Empty;
		ushort storyKeyLen = (ushort)Encoding.Unicode.GetBytes(storyKeyValue, 0, storyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(storyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += storyKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimStoryRewardResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public string storyKey;
	public int diamond;
	public int newDiamond;

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimStoryRewardResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort storyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.storyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, storyKeyLen);
		count += storyKeyLen;
		this.diamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimStoryRewardResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string storyKeyValue = this.storyKey ?? string.Empty;
		ushort storyKeyLen = (ushort)Encoding.Unicode.GetBytes(storyKeyValue, 0, storyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(storyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += storyKeyLen;
		Array.Copy(BitConverter.GetBytes(this.diamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Investment : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_Investment; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Investment), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Investment : IPacket
{
	public int starTotal;
	public int newGold;
	public class Slot
	{
		public int slotIndex;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Slot> slots = new List<Slot>();

	public ushort Protocol { get { return (ushort)PacketID.S_Investment; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.starTotal = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slots.Clear();
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < slotLen; i++)
		{
			Slot slot = new Slot();
			slot.Read(segment, ref count);
			slots.Add(slot);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Investment), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.starTotal), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.slots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Slot slot in this.slots)
			slot.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_UpgradeInvestment : IPacket
{
	public int slotIndex;
	public int expectedLevel;

	public ushort Protocol { get { return (ushort)PacketID.C_UpgradeInvestment; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.expectedLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_UpgradeInvestment), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.expectedLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UpgradeInvestmentResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int slotIndex;
	public int newLevel;
	public int spentGold;
	public int newGold;
	public int starTotal;
	public int spentDiamond;
	public int newDiamond;

	public ushort Protocol { get { return (ushort)PacketID.S_UpgradeInvestmentResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.spentGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.starTotal = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.spentDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UpgradeInvestmentResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.spentGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.starTotal), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.spentDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_MainQuest : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_MainQuest; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_MainQuest), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_MainQuestAction : IPacket
{
	public int objective;

	public ushort Protocol { get { return (ushort)PacketID.C_MainQuestAction; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.objective = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_MainQuestAction), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.objective), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MainQuest : IPacket
{
	public int level;
	public int objective;
	public int param;
	public int progress;
	public int threshold;
	public bool isCompleted;
	public class Reward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Reward> rewards = new List<Reward>();

	public ushort Protocol { get { return (ushort)PacketID.S_MainQuest; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.objective = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.param = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.progress = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.threshold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isCompleted = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.rewards.Clear();
		ushort rewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < rewardLen; i++)
		{
			Reward reward = new Reward();
			reward.Read(segment, ref count);
			rewards.Add(reward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MainQuest), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.objective), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.param), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.progress), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.threshold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isCompleted), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.rewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Reward reward in this.rewards)
			reward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MainQuestNotify : IPacket
{
	public int level;

	public ushort Protocol { get { return (ushort)PacketID.S_MainQuestNotify; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MainQuestNotify), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimMainQuest : IPacket
{
	public int level;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimMainQuest; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimMainQuest), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimMainQuestResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int claimedLevel;
	public int newGold;
	public int newDiamond;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimMainQuestResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimMainQuestResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DispatchInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DispatchInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DispatchInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DispatchInfo : IPacket
{
	public int refreshCharges;
	public int maxRefreshCharges;
	public int membersPerSlot;
	public class Slot
	{
		public int slotIndex;
		public int conditionId;
		public string membersCsv;
		public long startUnix;
		public long endUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.conditionId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort membersCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.membersCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, membersCsvLen);
			count += membersCsvLen;
			this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.conditionId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string membersCsvValue = this.membersCsv ?? string.Empty;
			ushort membersCsvLen = (ushort)Encoding.Unicode.GetBytes(membersCsvValue, 0, membersCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(membersCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += membersCsvLen;
			Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Slot> slots = new List<Slot>();
	public class Condition
	{
		public int conditionId;
		public int kind;
		public int param;
		public int threshold;
		public int tier;
		public int durationSeconds;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.conditionId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.param = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.threshold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.durationSeconds = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.conditionId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.param), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.threshold), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.durationSeconds), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Condition> conditions = new List<Condition>();
	public class TierGrant
	{
		public int tier;
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<TierGrant> tierGrants = new List<TierGrant>();

	public ushort Protocol { get { return (ushort)PacketID.S_DispatchInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.refreshCharges = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxRefreshCharges = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.membersPerSlot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slots.Clear();
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < slotLen; i++)
		{
			Slot slot = new Slot();
			slot.Read(segment, ref count);
			slots.Add(slot);
		}
		this.conditions.Clear();
		ushort conditionLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < conditionLen; i++)
		{
			Condition condition = new Condition();
			condition.Read(segment, ref count);
			conditions.Add(condition);
		}
		this.tierGrants.Clear();
		ushort tierGrantLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < tierGrantLen; i++)
		{
			TierGrant tierGrant = new TierGrant();
			tierGrant.Read(segment, ref count);
			tierGrants.Add(tierGrant);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DispatchInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.refreshCharges), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxRefreshCharges), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.membersPerSlot), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.slots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Slot slot in this.slots)
			slot.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.conditions.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Condition condition in this.conditions)
			condition.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.tierGrants.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (TierGrant tierGrant in this.tierGrants)
			tierGrant.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DispatchAction : IPacket
{
	public int action;
	public int slotIndex;
	public string membersCsv;

	public ushort Protocol { get { return (ushort)PacketID.C_DispatchAction; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort membersCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.membersCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, membersCsvLen);
		count += membersCsvLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DispatchAction), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string membersCsvValue = this.membersCsv ?? string.Empty;
		ushort membersCsvLen = (ushort)Encoding.Unicode.GetBytes(membersCsvValue, 0, membersCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(membersCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += membersCsvLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DispatchActionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int action;
	public int slotIndex;
	public int conditionId;
	public string membersCsv;
	public long startUnix;
	public long endUnix;
	public int refreshCharges;
	public int newGold;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_DispatchActionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.action = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.conditionId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort membersCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.membersCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, membersCsvLen);
		count += membersCsvLen;
		this.startUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.refreshCharges = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DispatchActionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.action), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.conditionId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string membersCsvValue = this.membersCsv ?? string.Empty;
		ushort membersCsvLen = (ushort)Encoding.Unicode.GetBytes(membersCsvValue, 0, membersCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(membersCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += membersCsvLen;
		Array.Copy(BitConverter.GetBytes(this.startUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.refreshCharges), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DispatchNotify : IPacket
{
	public class Slot
	{
		public int slotIndex;
		public long endUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.endUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.endUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Slot> slots = new List<Slot>();

	public ushort Protocol { get { return (ushort)PacketID.S_DispatchNotify; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.slots.Clear();
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < slotLen; i++)
		{
			Slot slot = new Slot();
			slot.Read(segment, ref count);
			slots.Add(slot);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DispatchNotify), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.slots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Slot slot in this.slots)
			slot.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SocialSettings : IPacket
{
	public bool write;
	public int whisperPolicy;

	public ushort Protocol { get { return (ushort)PacketID.C_SocialSettings; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.write = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.whisperPolicy = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SocialSettings), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.write), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.whisperPolicy), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SocialSettings : IPacket
{
	public int whisperPolicy;

	public ushort Protocol { get { return (ushort)PacketID.S_SocialSettings; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.whisperPolicy = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SocialSettings), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.whisperPolicy), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Test : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_Test; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Test), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DoomInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DoomInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DoomInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DoomInfo : IPacket
{
	public int bestScore;
	public int claimedScore;
	public bool canClaim;

	public ushort Protocol { get { return (ushort)PacketID.S_DoomInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.bestScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClaim = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DoomInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.bestScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClaim), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DoomResult : IPacket
{
	public int score;

	public ushort Protocol { get { return (ushort)PacketID.C_DoomResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.score = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DoomResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DoomResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int bestScore;
	public bool isBest;
	public bool canClaim;

	public ushort Protocol { get { return (ushort)PacketID.S_DoomResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bestScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isBest = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.canClaim = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DoomResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bestScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isBest), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.canClaim), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DoomClaim : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DoomClaim; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DoomClaim), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DoomClaimResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int claimedScore;
	public int bestScore;
	public int newGold;
	public int newDiamond;
	public class Granted
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Granted> granteds = new List<Granted>();

	public ushort Protocol { get { return (ushort)PacketID.S_DoomClaimResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bestScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.granteds.Clear();
		ushort grantedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedLen; i++)
		{
			Granted granted = new Granted();
			granted.Read(segment, ref count);
			granteds.Add(granted);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DoomClaimResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bestScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.granteds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Granted granted in this.granteds)
			granted.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_BattleFinished : IPacket
{
	public int content;

	public ushort Protocol { get { return (ushort)PacketID.C_BattleFinished; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.content = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_BattleFinished), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.content), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_PassInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_PassInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_PassInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_PassInfo : IPacket
{
	public long staminaSpent;
	public int level;
	public int freeClaimedCount;
	public int passClaimedCount;
	public bool hasPass;
	public bool canClaim;

	public ushort Protocol { get { return (ushort)PacketID.S_PassInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.staminaSpent = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.freeClaimedCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.passClaimedCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hasPass = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.canClaim = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_PassInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.staminaSpent), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.freeClaimedCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.passClaimedCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hasPass), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.canClaim), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_PassClaim : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_PassClaim; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_PassClaim), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_PassClaimResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public long staminaSpent;
	public int level;
	public int freeClaimedCount;
	public int passClaimedCount;
	public bool hasPass;
	public bool canClaim;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class Granted
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Granted> granteds = new List<Granted>();
	public class EquipRoll
	{
		public long instanceId;
		public int equipDataId;
		public int starLevel;
		public int level;
		public int mainPoints;
		public int opt1Points;
		public int opt2Points;
		public int opt3Points;
		public int opt4Points;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.instanceId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.equipDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.mainPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt1Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt2Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt3Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.opt4Points = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.instanceId), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.equipDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.mainPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt1Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt2Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt3Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.opt4Points), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<EquipRoll> equipRolls = new List<EquipRoll>();

	public ushort Protocol { get { return (ushort)PacketID.S_PassClaimResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaSpent = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.freeClaimedCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.passClaimedCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hasPass = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.canClaim = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.granteds.Clear();
		ushort grantedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedLen; i++)
		{
			Granted granted = new Granted();
			granted.Read(segment, ref count);
			granteds.Add(granted);
		}
		this.equipRolls.Clear();
		ushort equipRollLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equipRollLen; i++)
		{
			EquipRoll equipRoll = new EquipRoll();
			equipRoll.Read(segment, ref count);
			equipRolls.Add(equipRoll);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_PassClaimResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaSpent), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.freeClaimedCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.passClaimedCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hasPass), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.canClaim), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.granteds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Granted granted in this.granteds)
			granted.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.equipRolls.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (EquipRoll equipRoll in this.equipRolls)
			equipRoll.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EventRouletteInfo : IPacket
{
	public string eventKey;

	public ushort Protocol { get { return (ushort)PacketID.C_EventRouletteInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EventRouletteInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EventRouletteInfo : IPacket
{
	public string eventKey;
	public int tokens;
	public class Slot
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Slot> slots = new List<Slot>();

	public ushort Protocol { get { return (ushort)PacketID.S_EventRouletteInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
		this.tokens = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slots.Clear();
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < slotLen; i++)
		{
			Slot slot = new Slot();
			slot.Read(segment, ref count);
			slots.Add(slot);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EventRouletteInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;
		Array.Copy(BitConverter.GetBytes(this.tokens), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.slots.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Slot slot in this.slots)
			slot.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EventRouletteSpin : IPacket
{
	public string eventKey;

	public ushort Protocol { get { return (ushort)PacketID.C_EventRouletteSpin; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EventRouletteSpin), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EventRouletteSpinResult : IPacket
{
	public string eventKey;
	public bool isSuccess;
	public int failReason;
	public int slotIndex;
	public int tokens;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class Granted
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Granted> granteds = new List<Granted>();

	public ushort Protocol { get { return (ushort)PacketID.S_EventRouletteSpinResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.slotIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.tokens = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.granteds.Clear();
		ushort grantedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedLen; i++)
		{
			Granted granted = new Granted();
			granted.Read(segment, ref count);
			granteds.Add(granted);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EventRouletteSpinResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.slotIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.tokens), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.granteds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Granted granted in this.granteds)
			granted.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReportCharacterPower : IPacket
{
	public int charDataId;
	public long combatPower;

	public ushort Protocol { get { return (ushort)PacketID.C_ReportCharacterPower; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReportCharacterPower), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ExpeditionHome : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ExpeditionHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ExpeditionHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ExpeditionHome : IPacket
{
	public bool matched;
	public long nextMatchUnix;
	public long dayResetUnix;
	public int myAccountId;
	public bool enteredToday;
	public class Member
	{
		public int slot;
		public int accountId;
		public string nickname;
		public int profileFrameId;
		public int charDataId;
		public int costumeId;
		public int level;
		public int starLevel;
		public bool limitBroken;
		public long combatPower;
		public bool mercenary;
		public bool enteredToday;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.profileFrameId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.costumeId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.starLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.limitBroken = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.combatPower = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.mercenary = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.enteredToday = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.profileFrameId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.costumeId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.starLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.limitBroken), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.combatPower), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.mercenary), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.enteredToday), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Member> members = new List<Member>();
	public class Location
	{
		public int location;
		public int level;
		public long hp;
		public long maxHp;
		public int busyAccountId;
		public string busyNickname;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.location = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.hp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.maxHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.busyAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort busyNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.busyNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, busyNicknameLen);
			count += busyNicknameLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.location), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.hp), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.maxHp), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.busyAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string busyNicknameValue = this.busyNickname ?? string.Empty;
			ushort busyNicknameLen = (ushort)Encoding.Unicode.GetBytes(busyNicknameValue, 0, busyNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(busyNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += busyNicknameLen;
			return success;
		}	
	}
	public List<Location> locations = new List<Location>();

	public ushort Protocol { get { return (ushort)PacketID.S_ExpeditionHome; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.matched = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.nextMatchUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.dayResetUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.myAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.enteredToday = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.members.Clear();
		ushort memberLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < memberLen; i++)
		{
			Member member = new Member();
			member.Read(segment, ref count);
			members.Add(member);
		}
		this.locations.Clear();
		ushort locationLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < locationLen; i++)
		{
			Location location = new Location();
			location.Read(segment, ref count);
			locations.Add(location);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ExpeditionHome), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.matched), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.nextMatchUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.dayResetUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.myAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.enteredToday), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.members.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Member member in this.members)
			member.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.locations.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Location location in this.locations)
			location.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EnterExpedition : IPacket
{
	public int location;

	public ushort Protocol { get { return (ushort)PacketID.C_EnterExpedition; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.location = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EnterExpedition), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.location), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EnterExpeditionResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int location;
	public int level;
	public long hp;
	public long maxHp;
	public string busyNickname;

	public ushort Protocol { get { return (ushort)PacketID.S_EnterExpeditionResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.location = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.maxHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		ushort busyNicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.busyNickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, busyNicknameLen);
		count += busyNicknameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EnterExpeditionResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.location), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.maxHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		string busyNicknameValue = this.busyNickname ?? string.Empty;
		ushort busyNicknameLen = (ushort)Encoding.Unicode.GetBytes(busyNicknameValue, 0, busyNicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(busyNicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += busyNicknameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ExpeditionMemberDeck : IPacket
{
	public int slot;

	public ushort Protocol { get { return (ushort)PacketID.C_ExpeditionMemberDeck; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.slot = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ExpeditionMemberDeck), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.slot), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReportExpeditionDamage : IPacket
{
	public int location;
	public long damage;

	public ushort Protocol { get { return (ushort)PacketID.C_ReportExpeditionDamage; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.location = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.damage = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReportExpeditionDamage), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.location), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.damage), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ExpeditionDamageResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int location;
	public int level;
	public long hp;
	public long maxHp;
	public bool levelUp;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_ExpeditionDamageResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.location = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.maxHp = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.levelUp = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ExpeditionDamageResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.location), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.maxHp), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.levelUp), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ExpeditionLog : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ExpeditionLog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ExpeditionLog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ExpeditionLog : IPacket
{
	public int myAccountId;
	public class Entry
	{
		public int accountId;
		public string nickname;
		public int location;
		public int level;
		public long damage;
		public bool levelUp;
		public long createdUnix;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.accountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nicknameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickname = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nicknameLen);
			count += nicknameLen;
			this.location = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.damage = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
			this.levelUp = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.createdUnix = BitConverter.ToInt64(segment.Array, segment.Offset + count);
			count += sizeof(long);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.accountId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string nicknameValue = this.nickname ?? string.Empty;
			ushort nicknameLen = (ushort)Encoding.Unicode.GetBytes(nicknameValue, 0, nicknameValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nicknameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nicknameLen;
			Array.Copy(BitConverter.GetBytes(this.location), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.damage), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			Array.Copy(BitConverter.GetBytes(this.levelUp), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.createdUnix), 0, segment.Array, segment.Offset + count, sizeof(long));
			count += sizeof(long);
			return success;
		}	
	}
	public List<Entry> entrys = new List<Entry>();

	public ushort Protocol { get { return (ushort)PacketID.S_ExpeditionLog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.myAccountId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.entrys.Clear();
		ushort entryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entryLen; i++)
		{
			Entry entry = new Entry();
			entry.Read(segment, ref count);
			entrys.Add(entry);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ExpeditionLog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.myAccountId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.entrys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entry entry in this.entrys)
			entry.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DoomEntryInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DoomEntryInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DoomEntryInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DoomEntryInfo : IPacket
{
	public int remaining;
	public int dailyEntries;
	public int usedEntries;
	public int nextEntryCost;

	public ushort Protocol { get { return (ushort)PacketID.S_DoomEntryInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.remaining = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.usedEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nextEntryCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DoomEntryInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.remaining), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.usedEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nextEntryCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DoomEnter : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_DoomEnter; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DoomEnter), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DoomEnterResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int remaining;
	public int dailyEntries;
	public int usedEntries;
	public int nextEntryCost;
	public int newDiamond;

	public ushort Protocol { get { return (ushort)PacketID.S_DoomEnterResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.remaining = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.usedEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.nextEntryCost = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DoomEnterResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.remaining), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.usedEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.nextEntryCost), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_DoomFinish : IPacket
{
	public int score;
	public int phase;

	public ushort Protocol { get { return (ushort)PacketID.C_DoomFinish; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.score = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.phase = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_DoomFinish), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.phase), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_DoomFinishResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int bestScore;
	public bool isBest;
	public bool canClaim;
	public int gainedExp;
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_DoomFinishResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bestScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isBest = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.canClaim = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_DoomFinishResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bestScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isBest), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.canClaim), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TowerInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_TowerInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TowerInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_TowerInfo : IPacket
{
	public int floor;
	public int maxFloor;
	public bool canClimb;
	public int bonusKind;
	public int bonusTarget;
	public int bonusValue;

	public ushort Protocol { get { return (ushort)PacketID.S_TowerInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.floor = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxFloor = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClimb = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.bonusKind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bonusTarget = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bonusValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_TowerInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.floor), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxFloor), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClimb), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.bonusKind), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bonusTarget), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bonusValue), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TowerEnter : IPacket
{
	public int floor;

	public ushort Protocol { get { return (ushort)PacketID.C_TowerEnter; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.floor = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TowerEnter), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.floor), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_TowerEnterResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_TowerEnterResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_TowerEnterResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TowerClear : IPacket
{
	public int floor;

	public ushort Protocol { get { return (ushort)PacketID.C_TowerClear; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.floor = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TowerClear), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.floor), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_TowerClearResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int clearedFloor;
	public int floor;
	public bool canClimb;
	public int bonusKind;
	public int bonusTarget;
	public int bonusValue;
	public int newDiamond;
	public class Granted
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Granted> granteds = new List<Granted>();

	public ushort Protocol { get { return (ushort)PacketID.S_TowerClearResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.clearedFloor = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.floor = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClimb = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.bonusKind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bonusTarget = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bonusValue = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.granteds.Clear();
		ushort grantedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedLen; i++)
		{
			Granted granted = new Granted();
			granted.Read(segment, ref count);
			granteds.Add(granted);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_TowerClearResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.clearedFloor), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.floor), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClimb), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.bonusKind), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bonusTarget), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bonusValue), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.granteds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Granted granted in this.granteds)
			granted.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AttendanceEventInfo : IPacket
{
	public string eventKey;

	public ushort Protocol { get { return (ushort)PacketID.C_AttendanceEventInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AttendanceEventInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AttendanceEventInfo : IPacket
{
	public string eventKey;
	public bool locked;
	public int claimedDays;
	public bool canClaimToday;
	public int maxDays;
	public class DayReward
	{
		public int day;
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.day = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.day), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<DayReward> dayRewards = new List<DayReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_AttendanceEventInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
		this.locked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.claimedDays = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClaimToday = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.maxDays = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dayRewards.Clear();
		ushort dayRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < dayRewardLen; i++)
		{
			DayReward dayReward = new DayReward();
			dayReward.Read(segment, ref count);
			dayRewards.Add(dayReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AttendanceEventInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;
		Array.Copy(BitConverter.GetBytes(this.locked), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.claimedDays), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClaimToday), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.maxDays), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.dayRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (DayReward dayReward in this.dayRewards)
			dayReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimAttendanceEvent : IPacket
{
	public string eventKey;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimAttendanceEvent; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimAttendanceEvent), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimAttendanceEventResult : IPacket
{
	public string eventKey;
	public bool isSuccess;
	public int failReason;
	public int claimedDays;
	public bool canClaimToday;
	public int newGold;
	public int newDiamond;
	public int newStamina;
	public class GrantedReward
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<GrantedReward> grantedRewards = new List<GrantedReward>();

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimAttendanceEventResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort eventKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.eventKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, eventKeyLen);
		count += eventKeyLen;
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedDays = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.canClaimToday = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.grantedRewards.Clear();
		ushort grantedRewardLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedRewardLen; i++)
		{
			GrantedReward grantedReward = new GrantedReward();
			grantedReward.Read(segment, ref count);
			grantedRewards.Add(grantedReward);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimAttendanceEventResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		string eventKeyValue = this.eventKey ?? string.Empty;
		ushort eventKeyLen = (ushort)Encoding.Unicode.GetBytes(eventKeyValue, 0, eventKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(eventKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += eventKeyLen;
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedDays), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.canClaimToday), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.grantedRewards.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (GrantedReward grantedReward in this.grantedRewards)
			grantedReward.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FrontlineInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_FrontlineInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FrontlineInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FrontlineInfo : IPacket
{
	public int maxStage;
	public int staminaPerClear;
	public int dailyStaminaLimit;
	public int dailyStaminaUsed;
	public class Frontline
	{
		public int frontlineId;
		public int stage;
		public string membersCsv;
		public bool locked;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort membersCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.membersCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, membersCsvLen);
			count += membersCsvLen;
			this.locked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string membersCsvValue = this.membersCsv ?? string.Empty;
			ushort membersCsvLen = (ushort)Encoding.Unicode.GetBytes(membersCsvValue, 0, membersCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(membersCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += membersCsvLen;
			Array.Copy(BitConverter.GetBytes(this.locked), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			return success;
		}	
	}
	public List<Frontline> frontlines = new List<Frontline>();

	public ushort Protocol { get { return (ushort)PacketID.S_FrontlineInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.maxStage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.staminaPerClear = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyStaminaLimit = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyStaminaUsed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.frontlines.Clear();
		ushort frontlineLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < frontlineLen; i++)
		{
			Frontline frontline = new Frontline();
			frontline.Read(segment, ref count);
			frontlines.Add(frontline);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FrontlineInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.maxStage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.staminaPerClear), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyStaminaLimit), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyStaminaUsed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.frontlines.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Frontline frontline in this.frontlines)
			frontline.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FrontlineSetMembers : IPacket
{
	public int frontlineId;
	public string membersCsv;

	public ushort Protocol { get { return (ushort)PacketID.C_FrontlineSetMembers; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort membersCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.membersCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, membersCsvLen);
		count += membersCsvLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FrontlineSetMembers), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string membersCsvValue = this.membersCsv ?? string.Empty;
		ushort membersCsvLen = (ushort)Encoding.Unicode.GetBytes(membersCsvValue, 0, membersCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(membersCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += membersCsvLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FrontlineSetMembersResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int frontlineId;
	public string membersCsv;
	public bool locked;

	public ushort Protocol { get { return (ushort)PacketID.S_FrontlineSetMembersResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort membersCsvLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.membersCsv = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, membersCsvLen);
		count += membersCsvLen;
		this.locked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FrontlineSetMembersResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		string membersCsvValue = this.membersCsv ?? string.Empty;
		ushort membersCsvLen = (ushort)Encoding.Unicode.GetBytes(membersCsvValue, 0, membersCsvValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(membersCsvLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += membersCsvLen;
		Array.Copy(BitConverter.GetBytes(this.locked), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FrontlineReset : IPacket
{
	public int frontlineId;

	public ushort Protocol { get { return (ushort)PacketID.C_FrontlineReset; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FrontlineReset), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FrontlineResetResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int frontlineId;
	public int stage;

	public ushort Protocol { get { return (ushort)PacketID.S_FrontlineResetResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FrontlineResetResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FrontlineEnter : IPacket
{
	public int frontlineId;
	public int stage;

	public ushort Protocol { get { return (ushort)PacketID.C_FrontlineEnter; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FrontlineEnter), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FrontlineEnterResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int newStamina;
	public int dailyStaminaUsed;

	public ushort Protocol { get { return (ushort)PacketID.S_FrontlineEnterResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyStaminaUsed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FrontlineEnterResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyStaminaUsed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_FrontlineFinish : IPacket
{
	public int frontlineId;
	public int stage;
	public bool cleared;

	public ushort Protocol { get { return (ushort)PacketID.C_FrontlineFinish; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.cleared = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_FrontlineFinish), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.cleared), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_FrontlineFinishResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int frontlineId;
	public bool cleared;
	public int stage;
	public bool locked;
	public int newStamina;
	public int dailyStaminaUsed;
	public int gainedExp;
	public class CharExp
	{
		public int charDataId;
		public int gainedExp;
		public int newLevel;
		public int newExp;
		public int maxExp;
		public int gainedStatPoints;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.charDataId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.newExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.gainedStatPoints = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.charDataId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.newExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.gainedStatPoints), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<CharExp> charExps = new List<CharExp>();

	public ushort Protocol { get { return (ushort)PacketID.S_FrontlineFinishResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.frontlineId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.cleared = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.locked = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.newStamina = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyStaminaUsed = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.charExps.Clear();
		ushort charExpLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < charExpLen; i++)
		{
			CharExp charExp = new CharExp();
			charExp.Read(segment, ref count);
			charExps.Add(charExp);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_FrontlineFinishResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.frontlineId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.cleared), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.locked), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.newStamina), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyStaminaUsed), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.charExps.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (CharExp charExp in this.charExps)
			charExp.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_HeroesInfo : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_HeroesInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_HeroesInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_HeroesInfo : IPacket
{
	public int stage;
	public int maxStage;
	public int remainEntries;
	public int dailyEntries;

	public ushort Protocol { get { return (ushort)PacketID.S_HeroesInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxStage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.remainEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.dailyEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_HeroesInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxStage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.remainEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.dailyEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_HeroesEnter : IPacket
{
	public int stage;

	public ushort Protocol { get { return (ushort)PacketID.C_HeroesEnter; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_HeroesEnter), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_HeroesEnterResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int remainEntries;

	public ushort Protocol { get { return (ushort)PacketID.S_HeroesEnterResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.remainEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_HeroesEnterResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.remainEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_HeroesClear : IPacket
{
	public int stage;

	public ushort Protocol { get { return (ushort)PacketID.C_HeroesClear; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_HeroesClear), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_HeroesClearResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int clearedStage;
	public int stage;
	public int remainEntries;
	public int newGold;
	public int newDiamond;
	public class Granted
	{
		public int kind;
		public int value;
		public int qty;
		public string currencyKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.kind = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.value = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort currencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.currencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyKeyLen);
			count += currencyKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.kind), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.value), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			string currencyKeyValue = this.currencyKey ?? string.Empty;
			ushort currencyKeyLen = (ushort)Encoding.Unicode.GetBytes(currencyKeyValue, 0, currencyKeyValue.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(currencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += currencyKeyLen;
			return success;
		}	
	}
	public List<Granted> granteds = new List<Granted>();

	public ushort Protocol { get { return (ushort)PacketID.S_HeroesClearResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.clearedStage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.stage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.remainEntries = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newDiamond = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.granteds.Clear();
		ushort grantedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < grantedLen; i++)
		{
			Granted granted = new Granted();
			granted.Read(segment, ref count);
			granteds.Add(granted);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_HeroesClearResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.clearedStage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.stage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.remainEntries), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newDiamond), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.granteds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Granted granted in this.granteds)
			granted.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

