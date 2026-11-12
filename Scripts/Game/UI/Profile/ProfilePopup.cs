using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 프로필 팝업. 대표 캐릭터(+장착 프로필 테두리)/닉네임/UID 와 계정 진행 지표(총 전투력/캐릭터 수집/총 별 등급/계정 생성일/누적 충전 금액)를
/// 표시하고, 닉네임 변경/프로필 테두리 변경 팝업과 덱 팝업으로 진입한다. UID 는 버튼으로 클립보드에 복사할 수 있다.
/// 닉네임/테두리/진행 지표는 PlayerStateStore 를 구독해 변경 즉시 반영된다(하위 팝업이 Store 를 갱신).
/// 계정 생성일/누적 충전 금액은 서버 권위 값이라 열릴 때 1회 조회한다.
/// </summary>
public sealed class ProfilePopup : UIView, IHudPopup {
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("대표 캐릭터 일러스트 위에 겹쳐 그릴 장착 테두리(선택). 미장착이면 스스로 숨김. " +
             "illustrationView 와 같은 영역을 덮도록 배치하고 Image 의 Raycast Target 은 끈다.")]
    [FormerlySerializedAs("nameplateView")]
    [SerializeField] private ProfileFrameView profileFrameView;
    [SerializeField] private Button changeNicknameButton;
    [FormerlySerializedAs("changeNameplateButton")]
    [SerializeField] private Button changeProfileFrameButton;

    [Header("Account Stats")]
    [Tooltip("덱 팝업을 여는 버튼(선택).")]
    [SerializeField] private Button openDeckButton;
    [Tooltip("덱 총 전투력. 정보 패널과 동일 계산.")]
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("캐릭터 수집 현황(예: '12 / 50 (24%)').")]
    [SerializeField] private TMP_Text collectionText;
    [Tooltip("총 별 등급(예: '80 / 750 (10%)'). 보유 캐릭터 별레벨 합 / 전체 캐릭터 최대 별레벨 합.")]
    [SerializeField] private TMP_Text starText;
    [Tooltip("계정 생성일(서버 조회, 예: '2026.07.16').")]
    [SerializeField] private TMP_Text createdAtText;
    [Tooltip("누적 충전 금액(서버 조회, 예: '12,000원').")]
    [SerializeField] private TMP_Text cumulativeChargeText;
    [Tooltip("업적 총점(서버 조회). 업적 팝업의 총합과 같은 값이다.")]
    [SerializeField] private TMP_Text achievementScoreText;
    [Tooltip("선택. 유저 랭킹(전투력) 순위(예: '3위'). 순위 구간별 색은 RankColorCatalog 가 정한다.")]
    [SerializeField] private RankText combatPowerRankText;
    [Tooltip("선택. 업적 랭킹 순위(예: '5위'). 순위 구간별 색은 RankColorCatalog 가 정한다.")]
    [SerializeField] private RankText achievementRankText;

    [Header("Guild")]
    [Tooltip("소속 길드 이름. 길드가 없으면 guildNoneLabel 을 대신 켠다.")]
    [SerializeField] private TMP_Text guildNameText;
    [Tooltip("선택. 길드 마크 아이콘.")]
    [SerializeField] private Image guildMarkImage;
    [Tooltip("선택. 길드 내 역할(길드장/부길드장/운영진/길드원).")]
    [SerializeField] private TMP_Text guildRoleText;
    [Tooltip("선택. 길드 미가입 시 켜는 안내(예: '길드 없음').")]
    [SerializeField] private GameObject guildNoneLabel;
    [Tooltip("길드 마크 스프라이트 카탈로그(guildMarkImage 를 쓸 때만 필요).")]
    [SerializeField] private GuildMarkCatalog guildMarkCatalog;

    [Header("UID")]
    [Tooltip("계정 공개 식별자(UID) 표시. 문의/제보 때 사용자가 그대로 전달하는 값이다.")]
    [SerializeField] private TMP_Text uidText;
    [Tooltip("UID 를 시스템 클립보드에 복사하는 버튼(선택).")]
    [SerializeField] private Button copyUidButton;

    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CompositeDisposable _subs;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (changeNicknameButton != null) changeNicknameButton.onClick.AddListener(() => OpenNicknameChange());
        if (changeProfileFrameButton != null) changeProfileFrameButton.onClick.AddListener(() => OpenProfileFrame());
        if (openDeckButton != null) openDeckButton.onClick.AddListener(() => OpenDeck());
        if (copyUidButton != null) copyUidButton.onClick.AddListener(() => CopyUid());
    }

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;

        var repId = _hud.Store.Deck.RepresentativeCharId;
        var appearance = _hud.Appearance.Mine(repId);
        if (appearance.Character != null && illustrationView != null) illustrationView.Bind(appearance);

        _subs?.Dispose();
        _subs = new CompositeDisposable();
        _hud.Store.Nickname.Subscribe(n => { if (nicknameText != null) nicknameText.text = n; }).AddTo(_subs);
        if (profileFrameView != null) profileFrameView.BindReactive(_hud.ProfileFrames, _hud.Store.EquippedProfileFrameId);

        RefreshUid();

        // 진행 지표는 덱/캐릭터 변경(덱 팝업에서의 성장 등)에 맞춰 즉시 갱신한다.
        _hud.Store.DeckChanged += RefreshStats;
        _hud.Store.CharactersChanged += OnCharactersChanged;
        RefreshStats();

        LoadAccountInfoAsync().Forget();
        LoadGuildAsync().Forget();
    }

    /// <summary>
    /// 소속 길드 표시. 길드 정보는 프로필 응답이 아니라 길드 쪽에서 오므로 따로 조회한다
    /// (프로필 패킷에 길드 필드를 끼워 넣으면 길드가 바뀔 때마다 두 응답이 어긋날 여지가 생긴다).
    /// </summary>
    private async UniTaskVoid LoadGuildAsync() {
        bool hasGuild = false;
        try {
            var home = await _hud.Guild.RequestHomeAsync(this.GetCancellationTokenOnDestroy());
            hasGuild = home.HasGuild;
            if (hasGuild) {
                if (guildNameText != null) guildNameText.text = home.Summary.Name;
                if (guildRoleText != null) guildRoleText.text = GuildRules.RoleName(home.MyRole);
                if (guildMarkImage != null) {
                    guildMarkImage.sprite = guildMarkCatalog != null ? guildMarkCatalog.Icon(home.Summary.MarkId) : null;
                    guildMarkImage.enabled = guildMarkImage.sprite != null;
                }
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }

        // 조회에 실패해도 "길드 없음"으로 보이게 두어, 빈 칸이 남지 않게 한다.
        if (guildNameText != null) guildNameText.gameObject.SetActive(hasGuild);
        if (guildRoleText != null) guildRoleText.gameObject.SetActive(hasGuild);
        if (guildMarkImage != null && !hasGuild) guildMarkImage.enabled = false;
        if (guildNoneLabel != null) guildNoneLabel.SetActive(!hasGuild);
    }

    private void OnCharactersChanged(CharacterChange _) => RefreshStats();

    // UID 는 서버가 "공개 식별자"로 설계한 GUID(PlayerGuid)다. 서버 내부 PK(UserId)는 순차 정수라
    // 가입자 수 추정/열거 우려로 노출하지 않는다(LoginCompletePanel 과 같은 규약).
    private string Uid => _hud != null && _hud.Login != null && _hud.Login.IsLoggedIn ? _hud.Login.PlayerGuid : "";

    private void RefreshUid() {
        string uid = Uid;
        if (uidText != null) uidText.text = uid;
        // 복사할 값이 없으면 버튼을 눌러도 할 일이 없으므로 아예 막는다.
        if (copyUidButton != null) copyUidButton.interactable = !string.IsNullOrEmpty(uid);
    }

    private void CopyUid() {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        _hud.Ui.ShowToast(Clipboard.Copy(Uid) ? "UID를 복사했습니다." : "복사할 UID가 없습니다.");
    }

    private void RefreshStats() {
        if (combatPowerText != null)
            combatPowerText.text = AccountStats.TotalDeckCombatPower(_hud).ToString("N0");
        if (collectionText != null)
            collectionText.text = Fraction(AccountStats.OwnedCharacterCount(_hud), AccountStats.TotalCharacterCount(_hud));
        if (starText != null)
            starText.text = Fraction(AccountStats.TotalStarLevel(_hud), AccountStats.MaxTotalStarLevel(_hud));
    }

    // "현재 / 최대 (백분율%)" 형태. 반올림이 아닌 내림으로 미완료가 100%로 보이지 않게 한다.
    private static string Fraction(int current, int max) {
        int percent = max > 0 ? Mathf.Clamp(Mathf.FloorToInt(current * 100f / max), 0, 100) : 0;
        return $"{current} / {max} ({percent}%)";
    }

    private async UniTaskVoid LoadAccountInfoAsync() {
        if (createdAtText != null) createdAtText.text = "-";
        if (cumulativeChargeText != null) cumulativeChargeText.text = "-";
        if (achievementScoreText != null) achievementScoreText.text = "-";
        if (combatPowerRankText != null) combatPowerRankText.SetPlaceholder();
        if (achievementRankText != null) achievementRankText.SetPlaceholder();

        var ct = this.GetCancellationTokenOnDestroy();
        try {
            var (profile, charge) = await UniTask.WhenAll(
                _hud.Profile.RequestProfileAsync(ct),
                _hud.ChargeReward.RequestStatusAsync(ct));

            if (createdAtText != null)
                createdAtText.text = profile.CreatedAt == default ? "-" : profile.CreatedAt.ToString("yyyy.MM.dd");
            if (cumulativeChargeText != null)
                cumulativeChargeText.text = $"{charge.CumulativeWon:N0}원";
            // 업적 총점과 순위는 프로필 응답에 함께 실려 온다(업적/랭킹 팝업을 열지 않아도 보이도록).
            // 순위는 마지막 집계 시점 기준이라 방금 오른 전투력이 바로 반영되지는 않는다.
            if (achievementScoreText != null)
                achievementScoreText.text = AchievementRules.ScoreText(profile.AchievementScore);
            if (combatPowerRankText != null) combatPowerRankText.SetRank(profile.CombatPowerRank);
            if (achievementRankText != null) achievementRankText.SetRank(profile.AchievementRank);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void OpenNicknameChange() {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenHudPopupAsync<NicknameChangePopup>(_hud.Popups.NicknameChangePopup).Forget();
    }

    private void OpenProfileFrame() {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenHudPopupAsync<ProfileFramePopup>(_hud.Popups.ProfileFramePopup).Forget();
    }

    // 이 버튼은 "편성하러 간다" 는 뜻이므로 덱 팝업을 연 뒤 편성 패널까지 이어 연다.
    // 덱 팝업을 다른 곳에서 열 때는 평소대로 목록만 보이므로 그쪽 동선은 그대로다.
    private void OpenDeck() {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenHudPopupAsync<DeckPopup>(_hud.Popups.DeckPopup, popup => popup.OpenComposition()).Forget();
    }

    /// <param name="afterShow">팝업이 다 열린 뒤에 할 일(선택). 여는 쪽마다 다른 후속 동작을 여기서 넘긴다.</param>
    private async UniTaskVoid OpenHudPopupAsync<T>(UnityEngine.AddressableAssets.AssetReference reference,
                                                   Action<T> afterShow = null) where T : UIView, IHudPopup {
        if (reference == null || !reference.RuntimeKeyIsValid()) { _hud.Ui.ShowToast("준비 중입니다."); return; }
        try {
            var popup = await _hud.Ui.ShowPopupAsync<T>(reference, this.GetCancellationTokenOnDestroy());
            popup.Initialize(_hud);
            await popup.ShowAsync();
            afterShow?.Invoke(popup);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    protected override void OnClosed() {
        if (_hud != null) {
            _hud.Store.DeckChanged -= RefreshStats;
            _hud.Store.CharactersChanged -= OnCharactersChanged;
        }
        _subs?.Dispose();
        _subs = null;
    }
}
