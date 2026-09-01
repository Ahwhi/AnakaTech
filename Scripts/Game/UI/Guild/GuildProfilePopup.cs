using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 길드 프로필 팝업(길드 id 하나만 있으면 어디서든 연다). 랭킹의 길드 마크, 유저 프로필의 소속 길드,
/// 대항전 전적 등 "길드를 가리키는 자리"라면 같은 팝업을 띄우면 된다.
///
/// 스스로 조회한다. 여는 쪽은 길드 id 만 넘기고, 길드 요약과 내 길드 상태(가입 여부/신청 여부)는
/// 이 팝업이 직접 받아 온다. 그래야 새 화면에서 열 때 그 화면이 길드 데이터를 미리 들고 있지 않아도 된다.
///
/// 가입 버튼은 내가 무소속일 때만 나온다. 문구와 동작(가입/신청/신청취소)은 길드 목록 카드와 같은
/// 규칙(GuildRules)에서 뽑으므로 두 화면이 어긋나지 않는다.
/// </summary>
public sealed class GuildProfilePopup : UIView {
    [Header("Profile")]
    [Tooltip("길드 마크. 마크가 없으면 숨긴다.")]
    [SerializeField] private Image markImage;
    [SerializeField] private TMP_Text guildNameText;
    [Tooltip("길드장 닉네임.")]
    [SerializeField] private TMP_Text masterText;
    [Tooltip("길드 생성일.")]
    [SerializeField] private TMP_Text createdText;
    [Tooltip("길드 전투력.")]
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("길드 레벨.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("선택. 길드 랭킹(길드 전투력) 순위(예: '3위'). 순위 구간별 색은 RankColorCatalog 가 정한다. " +
             "순위권 밖이거나 아직 집계 전이면 '순위 없음' 으로 표시된다.")]
    [SerializeField] private RankText rankText;
    [Tooltip("선택. 인원 표시(예: '24 / 30').")]
    [SerializeField] private TMP_Text memberCountText;
    [Tooltip("선택. 가입 방식 표시(즉시 가입 / 가입 신청).")]
    [SerializeField] private TMP_Text joinTypeText;
    [Tooltip("선택. 길드 소개.")]
    [SerializeField] private TMP_Text introText;

    [Header("Join")]
    [Tooltip("가입/신청/신청취소 버튼. 내가 이미 길드에 속해 있으면 통째로 숨긴다.")]
    [SerializeField] private Button joinButton;
    [Tooltip("가입 버튼의 문구. 비우면 버튼 하위의 TMP_Text 를 자동으로 찾는다.")]
    [SerializeField] private TMP_Text joinButtonText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private UniTaskCompletionSource _closed;
    private MainHudContext _ctx;
    private GuildMarkCatalog _marks;
    private GuildSummaryDto _guild;
    private bool _hasMyGuild;
    private bool _applied;
    private bool _busy;

    /// <summary>이 팝업에서 가입/신청 상태를 바꿨는지. 연 화면이 목록을 다시 받을지 판단하는 근거다.</summary>
    private bool _changed;

    protected override void Awake() {
        // 배경 탭으로 닫는 배선은 UIView 가 자기 tapToCloseArea 로 이미 한다(여기서 다시 걸지 않는다).
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinButton);
    }

    /// <summary>
    /// 길드를 조회해 표시하고, 닫힐 때까지 기다린다.
    /// </summary>
    /// <param name="marks">길드 마크 카탈로그. 여는 화면이 이미 들고 있는 것을 그대로 넘긴다.</param>
    /// <returns>이 팝업에서 가입/신청 상태를 바꿨으면 true.</returns>
    public async UniTask<bool> ShowAsync(MainHudContext ctx, int guildId, GuildMarkCatalog marks) {
        _ctx = ctx;
        _marks = marks;
        _changed = false;
        Clear();
        if (loadingGroup != null) loadingGroup.SetActive(true);

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();

        var ct = this.GetCancellationTokenOnDestroy();
        try {
            // 길드 요약과 내 길드 상태를 함께 받는다. 가입 버튼을 낼지가 내 상태에 달려 있어
            // 둘 중 하나만으로는 화면을 완성할 수 없다.
            var guildTask = ctx.Guild.RequestGuildAsync(guildId, ct);
            var homeTask = ctx.Guild.RequestHomeAsync(ct);
            var (guild, home) = await UniTask.WhenAll(guildTask, homeTask);

            _guild = guild;
            _hasMyGuild = home != null && home.GuildId != 0;
            _applied = home != null && home.AppliedGuildIds.Contains(guildId);
            Apply();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ctx.Ui.ShowToast("길드 정보를 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }

        await _closed.Task;
        return _changed;
    }

    private void Apply() {
        // 해체된 길드는 표시할 것이 없다(랭킹에 남아 있던 옛 줄을 눌렀을 때).
        if (_guild == null) {
            _ctx.Ui.ShowToast("이미 해체된 길드입니다.");
            CloseAsync().Forget();
            return;
        }

        if (guildNameText != null) guildNameText.text = _guild.Name;
        if (masterText != null) masterText.text = _guild.MasterNickname;
        if (createdText != null) createdText.text = DateText(_guild.CreatedUnix);
        if (combatPowerText != null) combatPowerText.text = _guild.CombatPower.ToString("N0");
        if (levelText != null) levelText.text = _guild.Level.ToString();
        if (rankText != null) rankText.SetRank(_guild.Rank);
        if (memberCountText != null) memberCountText.text = $"{_guild.MemberCount} / {GuildRules.MaxMembers}";
        if (joinTypeText != null) joinTypeText.text = GuildRules.JoinTypeName(_guild.JoinType);
        if (introText != null) introText.text = _guild.Intro;

        // 마크가 없거나 카탈로그에 미할당이면 빈 사각형이 남지 않도록 이미지를 끈다(랭킹 줄과 같은 규칙).
        if (markImage != null) {
            var sprite = _guild.MarkId > 0 && _marks != null ? _marks.Icon(_guild.MarkId) : null;
            markImage.gameObject.SetActive(sprite != null);
            markImage.sprite = sprite;
            markImage.enabled = sprite != null;
        }

        RefreshJoinButton();
    }

    // 이미 길드가 있으면 가입 버튼 자체를 감춘다(누를 수 없는 버튼을 남겨 두면 왜 안 되는지 알 수 없다).
    private void RefreshJoinButton() {
        if (joinButton == null) return;

        joinButton.gameObject.SetActive(!_hasMyGuild);
        if (_hasMyGuild) return;

        joinButton.interactable = GuildRules.CanPressJoin(_guild, _applied) && !_busy;

        var label = joinButtonText != null ? joinButtonText : joinButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = GuildRules.JoinActionLabel(_guild, _applied);
    }

    private void OnJoinButton() {
        if (_busy || _guild == null) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        JoinAsync().Forget();
    }

    private async UniTaskVoid JoinAsync() {
        _busy = true;
        RefreshJoinButton();

        try {
            var action = GuildRules.JoinActionOf(_guild, _applied);
            long power = (long)AccountStats.TotalDeckCombatPower(_ctx);
            var res = await _ctx.Guild.ActAsync(action, guildId: _guild.GuildId, combatPower: power,
                                                ct: this.GetCancellationTokenOnDestroy());
            if (!res.IsSuccess) { _ctx.Ui.ShowToast(GuildRules.FailMessage(res.FailReason)); return; }

            _changed = true;
            // 즉시 가입은 이미 길드원이 됐으므로 이 팝업에 더 있을 이유가 없다.
            if (action == GuildActionType.Join) { CloseAsync().Forget(); return; }

            // 신청/취소는 버튼 문구만 뒤집히므로 화면에서 바로 반영한다.
            _applied = action == GuildActionType.Apply;
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("요청을 처리하지 못했습니다.");
        } finally {
            _busy = false;
            RefreshJoinButton();
        }
    }

    private void Clear() {
        _guild = null;
        _hasMyGuild = false;
        _applied = false;
        _busy = false;
        if (joinButton != null) joinButton.gameObject.SetActive(false);
        if (markImage != null) markImage.gameObject.SetActive(false);
        // 조회가 끝나기 전까지는 앞서 본 길드의 순위가 남아 보이지 않게 자리만 채운다.
        if (rankText != null) rankText.SetPlaceholder();
    }

    private static string DateText(long unix) =>
        unix <= 0 ? "-" : DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy. MM. dd");

    protected override void OnClosed() {
        _closed?.TrySetResult();
    }
}
