using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 로컬 세이브(GameData.json) 전체. 파일은 기기당 하나이고, 한 기기에서 여러 계정을 쓸 수 있으므로
/// 저장 항목을 두 갈래로 나눈다.
///   - 기기 공용: 볼륨/표시 토글/패널 접힘/카메라 줌처럼 "이 기기에서의 화면 취향". 계정이 바뀌어도 유지되는 게 맞다.
///   - 계정별  : 그 계정에 속한 값(캐릭터 선택, 이벤트 열람 등). AccountLocalData 에 모아 계정 키로 분리한다.
/// 계정별 항목을 추가할 때는 AccountLocalData 에 필드만 늘리면 되고 분리 로직은 손대지 않는다.
/// </summary>
[Serializable]
public class GameData {
    // ── 기기 공용(계정 무관) ────────────────────────────
    // 사운드. 최종 출력은 마스터 x 개별 볼륨이라, 개별 값을 건드리지 않고 전체만 줄일 수 있다.
    // 값의 적용/저장 창구는 ISoundService 이며 여기에는 저장값만 둔다.
    // 첫 실행(세이브 파일이 없을 때)의 기본 볼륨. 새 세이브 생성과 초기화(SetDefaultData)가 같은 값을 쓰도록
    // 숫자는 여기 한 곳에만 둔다. 값을 바꾸면 이미 저장된 세이브는 그대로이고 새로 시작하는 기기에만 적용된다.
    public const float DefaultMasterVolume = 1f;
    public const float DefaultBgmVolume = 0.25f;
    public const float DefaultSfxVolume = 0.20f;

    public float MasterVolume = DefaultMasterVolume;
    public float BgmVolume = DefaultBgmVolume;
    public float SfxVolume = DefaultSfxVolume;

    // 타이틀 화면 배경음악 on/off. 설정 팝업의 BGM 볼륨과는 별개의 스위치라 볼륨 값을 건드리지 않는다
    // (타이틀에서 꺼도 게임 안 BGM 볼륨은 그대로다). 값의 창구는 TitleBgmToggle 이며 여기에는 저장값만 둔다.
    // (구버전 세이브에는 이 항목이 없으므로 역직렬화 시 초기값 true 가 그대로 유지된다.)
    public bool TitleBgmOn = true;

    // 화면(그래픽). 저장값을 실제 엔진 설정으로 옮기는 규칙은 DisplaySettings 한 곳에만 있다.
    // 목표 프레임(FPS). 0 이면 기기 기본값(DisplaySettings 가 플랫폼별로 정한다)을 쓴다.
    public int FrameRateLimit = 0;
    // 수직 동기화. 켜면 목표 프레임 대신 화면 주사율에 맞춰 그린다.
    public bool VSync = false;
    // 렌더 해상도 배율(1 = 기기 해상도). 낮출수록 가볍지만 화면이 흐려진다. 모바일에서만 의미가 있다.
    public float RenderScale = 1f;
    // 무입력이 이만큼(초) 이어지면 화면 잠금(잠수) 모드로 들어간다. 0 이면 자동 잠금을 쓰지 않는다.
    // 잠금 중에는 화면을 켠 채 절전 모드(PowerSaveMode)로 부하만 줄이므로, 유저가 만질 항목은 이 대기 시간 하나로 충분하다.
    public int AutoLockSeconds = 300;
    // UI 좌우 여백(가로 인셋). SafeArea 안쪽에서 좌우를 이만큼 더 좁혀 화면 가장자리의 UI 를 안쪽으로 당긴다.
    // 기기가 알려 주는 SafeArea 만으로는 아슬아슬한 단말이 있어, 유저가 직접 더 좁힐 수 있게 둔다.
    // 0 이면 SafeArea 그대로다(구버전 세이브에는 이 항목이 없으므로 역직렬화 시 초기값 0 이 유지된다).
    public float UiSideInset = 0f;

    // 전투 중 캐릭터가 스킬 사용 시 머리 위 말풍선(스킬명+!) 표시 여부. SettingPopup 에서 토글.
    public bool ShowSkillBubble = true;

    // 화면을 터치/클릭한 자리에 뜨는 피드백 이펙트 표시 여부. SettingPopup 에서 토글.
    // (구버전 세이브에는 이 항목이 없으므로 역직렬화 시 초기값 true 가 그대로 유지된다.)
    public bool ShowTouchVfx = true;

    // 전투 중 피해량 숫자 팝업 표시 여부.
    public bool ShowDamageNumber = true;

    // 대표 캐릭터가 적을 때렸을 때 카메라 흔들림 사용 여부.
    public bool UseCameraShake = true;

    // 전투 캐릭터 패널 슬롯(BattleCharacterSlot)의 체력/스킬 게이지 표시 여부.
    // 구버전 세이브에는 이 항목이 없으므로 기존 유저도 초기값 true(켜짐)로 시작한다.
    public bool ShowSlotGauges = true;

    // UI 패널 접힘 상태(로컬). 패널마다 고유 키로 기록하므로 접을 수 있는 패널이 늘어도 이 클래스는 고칠 게 없다.
    public List<PanelFoldState> panelFolds = new List<PanelFoldState>();

    // 전투 카메라 줌(오쏘 사이즈). 0 이하면 "저장값 없음"으로 보고 BattleSettings.CameraDefaultZoom 으로 시작한다.
    // 메인/의뢰 씬이 같은 값을 공유한다(같은 전장 카메라 조작이라 씬마다 따로 기억할 이유가 없다).
    public float BattleCameraZoom;

    // ── 계정별 ────────────────────────────────────────
    // 계정(PlayerGuid)마다 한 칸. 현재 계정의 칸은 UserDataManager.Account 로 얻는다.
    public List<AccountLocalData> accounts = new List<AccountLocalData>();

    // 구버전 세이브 이관용(신규 코드에서 사용 금지). 예전 이름(accountBadges)으로 저장된 계정 칸을
    // 로드 시 accounts 로 옮기고 비운다. 모든 세이브가 이관된 뒤에는 이 필드를 지워도 된다.
    public List<AccountLocalData> accountBadges = new List<AccountLocalData>();

    /// <summary>계정 키(PlayerGuid)에 해당하는 로컬 데이터를 찾고, 없으면 만들어 반환한다.</summary>
    public AccountLocalData AccountFor(string accountKey) {
        accountKey ??= "";
        accounts ??= new List<AccountLocalData>();
        foreach (var a in accounts) if (a.accountKey == accountKey) return a;

        var created = new AccountLocalData { accountKey = accountKey };
        accounts.Add(created);
        return created;
    }

    /// <summary>저장된 패널 접힘 상태. 기록이 없으면(첫 실행/새 패널) fallback 을 그대로 돌려준다.</summary>
    public bool PanelExpanded(string key, bool fallback) {
        if (string.IsNullOrEmpty(key) || panelFolds == null) return fallback;
        foreach (var p in panelFolds) if (p.key == key) return p.expanded;
        return fallback;
    }

    /// <summary>패널 접힘 상태를 기록한다(항목이 없으면 새로 만든다). 디스크 쓰기는 RequestSave 로 오토세이버에 맡긴다.</summary>
    public void SetPanelExpanded(string key, bool expanded) {
        if (string.IsNullOrEmpty(key)) return;
        panelFolds ??= new List<PanelFoldState>();
        foreach (var p in panelFolds) {
            if (p.key != key) continue;
            p.expanded = expanded;
            return;
        }
        panelFolds.Add(new PanelFoldState { key = key, expanded = expanded });
    }
}

/// <summary>접을 수 있는 UI 패널 하나의 상태. 키는 각 패널이 인스펙터에서 정한다(FoldablePanel.saveKey).</summary>
[Serializable]
public class PanelFoldState {
    public string key = "";
    public bool expanded = true;
}

/// <summary>
/// 계정 하나의 로컬 데이터. 계정 UUID(PlayerGuid)로 구분하므로 같은 기기에서 여러 계정을 써도 섞이지 않는다.
/// "그 계정의 것"인 값만 여기 둔다(기기 화면 취향은 GameData 본체).
/// </summary>
[Serializable]
public class AccountLocalData {
    public string accountKey = "";

    // New(Unread=NEW 뱃지)용 "봤음" 상태(로컬). 서버 저장 대신 여기에 기록한다.
    // 마지막으로 확인한 가챠 로테이션 XR 캐릭 id(0=미확인). 현재 로테이션과 다르면 가챠 New.
    public int seenRotationCharId = 0;
    // 마지막으로 확인한 상점 오퍼 id 목록. 새 오퍼가 생기면 상점 버튼/카테고리 New.
    public List<string> seenShopOfferIds = new List<string>();
    // 마지막으로 확인한 보유 캐릭터 id 목록. 새로 획득한 캐릭이 생기면 덱 버튼 New.
    public List<int> seenCharIds = new List<int>();
    // 마지막으로 확인한 투자 개방 슬롯 수(0=미확인). 별레벨이 올라 이보다 많이 열리면 투자 버튼 New.
    public int seenInvestmentSlots = 0;

    // 읽은 이벤트 id 목록(이벤트 미열람 Unread=NEW 배지 판단용).
    public List<long> readEventIds = new List<long>();

    // 마지막으로 확인한 쟁탈전 방어 기록 id(0=미확인). 서버가 알려준 최근 방어 기록 id 가 이 값보다 크면
    // "누가 나를 쳤다"는 알림을 켠다. 쟁탈전 화면 안에서만 쓰는 표시라 서버에 열람 상태를 두지 않는다.
    public long seenArenaDefenseLogId = 0;

    // 전투 카메라가 비추는 캐릭터 id. 0 은 "대표 추종"(기본값)이라 대표를 바꿔도 따로 지정할 필요가 없다.
    // 하단 전투 캐릭터 패널의 슬롯을 눌러 바꾸며, 지정한 캐릭터가 덱에서 빠지면 0(대표)으로 되돌아간다.
    public int battleFocusCharId;

    // 기력 부스트 모드를 켜 둔 상태인지. 서버가 관측하지 않는 클라 조작이라 여기 남겨 두고,
    // 메인 씬에 들어올 때 되살린다(씬을 다녀오거나 재접속했다고 유저가 고른 모드가 풀리지 않게).
    // 자원이 떨어져 저절로 꺼지거나 유저가 직접 끄면 false 로 기록된다.
    public bool boostModeOn;

    // 무한 타워 편성 선택(TowerSession 이 읽고 쓴다). 출처는 TowerDeckSource 값이고, 커스텀 편성은 출처가
    // 커스텀일 때만 남는다(메인 스쿼드/프리셋으로 바꾸면 비운다). 서버 판정과 무관한 화면 선택이라 로컬에 둔다.
    public int towerDeckSource;
    public int towerPresetSlot = -1;
    public List<int> towerCustomMembers = new List<int>();

    // 영웅전에 내보낼 소녀(HeroesSession 이 읽고 쓴다). 0 이면 고른 적이 없어 대표 소녀가 나선다.
    // 서버 판정과 무관한 화면 선택이라 로컬에 둔다(무한 타워 편성 선택과 같은 이유).
    public int heroesCharId;

    // 귓속말 상대별로 마지막까지 읽은 줄의 순번. 서버가 재접속 때 최근 귓속말을 다시 보내 주므로,
    // 읽음 표시를 메모리에만 두면 재접속할 때마다 다 읽은 대화가 안 읽음으로 되살아난다.
    // 열람 상태는 이 기기의 화면 표시일 뿐이라 서버에 두지 않는다(쟁탈전 방어 기록 확인과 같은 이유).
    public List<WhisperReadMark> whisperReadMarks = new List<WhisperReadMark>();

    /// <summary>그 상대와의 귓속말을 어디까지 읽었는지(기록이 없으면 0).</summary>
    public long WhisperReadMarkOf(int partnerAccountId) {
        if (whisperReadMarks == null) return 0;
        foreach (var m in whisperReadMarks) if (m.accountId == partnerAccountId) return m.messageId;
        return 0;
    }

    /// <summary>읽은 지점을 기록한다(뒤로 물리지 않는다). 디스크 쓰기는 RequestSave 로 오토세이버에 맡긴다.</summary>
    public void SetWhisperReadMark(int partnerAccountId, long messageId) {
        whisperReadMarks ??= new List<WhisperReadMark>();
        foreach (var m in whisperReadMarks) {
            if (m.accountId != partnerAccountId) continue;
            if (messageId > m.messageId) m.messageId = messageId;
            return;
        }
        whisperReadMarks.Add(new WhisperReadMark { accountId = partnerAccountId, messageId = messageId });
    }

    /// <summary>그 상대와의 대화를 지웠을 때 읽음 기록도 함께 지운다(목록이 끝없이 늘지 않게).</summary>
    public void RemoveWhisperReadMark(int partnerAccountId) =>
        whisperReadMarks?.RemoveAll(m => m.accountId == partnerAccountId);
}

/// <summary>귓속말 상대 하나의 읽은 지점(상대 계정 id + 마지막으로 읽은 줄의 순번).</summary>
[Serializable]
public class WhisperReadMark {
    public int accountId;
    public long messageId;
}

public static class UserDataManager {
    public static GameData Data;
    public static bool SaveFlag;
    private static string SavePath => Path.Combine(Application.persistentDataPath, "GameData.json");

    /// <summary>현재 로그인한 계정 키(PlayerGuid). 로그인 성공 시 LoginManager 가 알려 준다.</summary>
    public static string CurrentAccountKey { get; private set; } = "";

    /// <summary>
    /// 현재 계정의 로컬 데이터. 계정별 값은 전부 이 창구로만 읽고 쓴다.
    /// 세이브 로드 전이거나 아직 로그인하지 않았으면 null 이다(어느 계정 것인지 알 수 없는 값을 남기지 않는다).
    /// </summary>
    public static AccountLocalData Account =>
        Data != null && !string.IsNullOrEmpty(CurrentAccountKey) ? Data.AccountFor(CurrentAccountKey) : null;

    /// <summary>로그인한 계정을 알린다. 이후 Account 는 이 계정의 칸을 가리킨다(계정 전환 시 재호출).</summary>
    public static void SetAccount(string accountKey) => CurrentAccountKey = accountKey ?? "";

    /// <summary>신규 세이브의 기본값. 숫자는 GameData 의 Default* 상수 한 곳에서만 관리한다.</summary>
    public static void SetDefaultData() {
        Data.MasterVolume = GameData.DefaultMasterVolume;
        Data.BgmVolume = GameData.DefaultBgmVolume;
        Data.SfxVolume = GameData.DefaultSfxVolume;
    }

    public static bool Load() {
        try {
            if (!File.Exists(SavePath)) {
                Data = new GameData();
                SetDefaultData();
                Save();
                return true;
            }

            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<GameData>(json);

            if (Data == null) {
                Data = new GameData();
                SetDefaultData();
                Save();
                return true;
            }

            MigrateData();
        } catch (Exception e) {
            Debug.LogException(e);
            Data = new GameData();
            SetDefaultData();
            return true;
        }
        return false;
    }

    private static void MigrateData() {
        Data.MasterVolume = Mathf.Clamp01(Data.MasterVolume);
        Data.BgmVolume = Mathf.Clamp01(Data.BgmVolume);
        Data.SfxVolume = Mathf.Clamp01(Data.SfxVolume);

        // 화면 설정: 알 수 없는 값이 들어오면 "기기 기본"으로 되돌린다(엔진에 그대로 넘기지 않는다).
        if (Data.FrameRateLimit < 0) Data.FrameRateLimit = 0;
        if (Data.AutoLockSeconds < 0) Data.AutoLockSeconds = 0;
        Data.RenderScale = Data.RenderScale > 0f
            ? Mathf.Clamp(Data.RenderScale, DisplaySettings.MinRenderScale, 1f)
            : 1f;
        // NaN 이 들어오면 비교가 전부 false 라 Clamp 로는 걸러지지 않는다. 레이아웃에 NaN 이 새어 나가면
        // 그 아래 UI 가 통째로 사라지므로 먼저 정상값인지 확인하고 아니면 0(SafeArea 그대로)으로 되돌린다.
        // 음수는 정상값이다(UI 를 Safe Area 바깥으로 넓히는 쪽).
        Data.UiSideInset = float.IsNaN(Data.UiSideInset)
            ? 0f
            : Mathf.Clamp(Data.UiSideInset, DisplaySettings.MinUiSideInset, DisplaySettings.MaxUiSideInset);

        Data.panelFolds ??= new List<PanelFoldState>();
        Data.accounts ??= new List<AccountLocalData>();
        // 음수/NaN 같은 망가진 값이 들어오면 "저장값 없음"으로 되돌린다(카메라 쪽 클램프 전에 한 번 거른다).
        if (!(Data.BattleCameraZoom > 0f)) Data.BattleCameraZoom = 0f;

        MigrateLegacyAccounts();

        foreach (var a in Data.accounts) {
            if (a == null) continue;
            a.seenShopOfferIds ??= new List<string>();
            a.seenCharIds ??= new List<int>();
            a.readEventIds ??= new List<long>();
            a.whisperReadMarks ??= new List<WhisperReadMark>();
            // 캐릭터 id 는 양수라 음수는 망가진 값이다. 대표 추종(0)으로 되돌린다.
            if (a.battleFocusCharId < 0) a.battleFocusCharId = 0;
        }
    }

    // 구버전 이름(accountBadges)으로 저장된 계정 칸을 현재 목록으로 옮긴다. 같은 계정 칸이 이미 있으면 새 쪽을 남긴다.
    // (구버전의 계정 구분 없는 "읽은 이벤트 id"는 어느 계정 것인지 알 수 없어 이관하지 않는다. NEW 배지가 한 번 다시 뜬다.)
    private static void MigrateLegacyAccounts() {
        var legacy = Data.accountBadges;
        if (legacy == null || legacy.Count == 0) return;

        foreach (var old in legacy) {
            if (old == null || string.IsNullOrEmpty(old.accountKey)) continue;

            bool exists = false;
            foreach (var a in Data.accounts) {
                if (a == null || a.accountKey != old.accountKey) continue;
                exists = true;
                break;
            }
            if (!exists) Data.accounts.Add(old);
        }
        legacy.Clear();
    }

    public static void RequestSave() => SaveFlag = true;

    public static void Save() {
        if (Data == null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
        File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
        SaveFlag = false;
    }
}
