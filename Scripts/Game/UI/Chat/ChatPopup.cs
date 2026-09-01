using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 채팅 패널. 채널(전체/길드) 탭으로 나눠 대화를 보여 주고 발언을 보낸다.
///
/// 대화는 이 화면이 아니라 서비스(ChatService)가 들고 있다. 채팅창은 필요할 때만 열리는 화면이라
/// 여기에 쌓아 두면 창을 닫는 순간 그 사이의 대화가 사라지기 때문이다. 이 화면은 버퍼를 그리기만 한다.
///
/// 차단한 상대의 말은 애초에 서버가 보내지 않는다(과거 기록도 마찬가지). 클라가 따로 거를 것이 없고,
/// 차단 목록을 클라가 들고 있지 않아도 되며, 두 곳의 판정이 어긋날 일도 없다.
///
/// 시스템 알림(모집/수련)은 전체 채널에 섞여 흐르고, 줄 프리팹이 색과 형태를 달리 그린다.
/// </summary>
public sealed class ChatPopup : UIView, IHudPopup {
    [Serializable]
    private struct ChannelTab {
        [Tooltip("이 탭이 보여 줄 채널.")]
        public ChatChannel channel;
        public Button button;
        [Tooltip("선택 표시(현재 탭). 선택.")]
        public GameObject selectedIndicator;
    }

    [Header("Channels")]
    [Tooltip("채널 탭들. 배열 순서가 곧 탭 순서이며, 첫 칸이 기본 선택이다.")]
    [SerializeField] private ChannelTab[] tabs;

    [Header("List")]
    [Tooltip("대화 줄이 생성될 부모(Vertical Layout). 스크롤뷰 안에 둔다.")]
    [SerializeField] private Transform content;
    [Tooltip("남이 친 대화 줄 프리팹(ChatUserRowView).")]
    [SerializeField] private ChatUserRowView userRowPrefab;
    [Tooltip("선택. 내가 친 대화 줄 프리팹(ChatUserRowView). 비워 두면 위 프리팹을 그대로 쓴다.")]
    [SerializeField] private ChatUserRowView myUserRowPrefab;
    [Tooltip("공지 줄 프리팹(ChatNoticeRowView). 모집/수련 성공 알림이 이 줄로 그려진다.")]
    [SerializeField] private ChatNoticeRowView noticeRowPrefab;
    [Tooltip("목록 스크롤뷰. 새 대화가 오면 맨 아래로 따라간다.")]
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("대화가 하나도 없을 때 켜는 안내 묶음. 선택.")]
    [SerializeField] private GameObject emptyGroup;

    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [Tooltip("길드 채널인데 길드가 없을 때 켜는 안내 묶음(입력창 대신 노출). 선택.")]
    [SerializeField] private GameObject noGuildGroup;

    [Header("Whisper")]
    [Tooltip("귓속말 탭에서만 켜는 묶음(대화 상대 칩 목록). 다른 탭에서는 통째로 꺼진다. 선택.")]
    [SerializeField] private GameObject whisperGroup;
    [Tooltip("대화 상대 칩이 생성될 부모(가로 Layout). 상대가 늘면 스크롤되도록 스크롤뷰 안에 두는 것이 좋다.")]
    [SerializeField] private Transform whisperPartnerBar;
    [Tooltip("칩 줄 전체를 감출 오브젝트(배경/스크롤 포함). 비워 두면 위 칩 부모를 끄고 켠다. " +
             "칩 부모가 스크롤 안쪽 Content 라면 그 바깥 오브젝트를 여기에 넣어야 배경까지 함께 사라진다.")]
    [SerializeField] private GameObject whisperPartnerBarRoot;
    [Tooltip("대화 상대 칩 프리팹(ChatWhisperPartnerView).")]
    [SerializeField] private ChatWhisperPartnerView whisperPartnerPrefab;
    [Tooltip("신고 팝업 프리팹(ChatReportPopup). 비워 두면 신고 버튼이 동작하지 않는다.")]
    [SerializeField] private UnityEngine.AddressableAssets.AssetReferenceGameObject reportPopup;
    [Tooltip("귓속말 탭인데 대화 상대가 하나도 없을 때 켜는 안내 묶음(입력창 대신 노출). 선택.")]
    [SerializeField] private GameObject noWhisperTargetGroup;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CancellationToken _ct;
    private bool _wired;
    private bool _sending;

    // 길드 채널 발언이 "길드 없음"으로 거부된 실패 코드(서버 ChatSend 와 같은 값).
    private const int GuildRequiredFail = 5;

    private ChatChannel _channel = ChatChannel.World;

    // 귓속말 대상. 프로필의 귓속말 버튼/줄의 답장 버튼이 정하며, 탭을 옮겨도 유지된다
    // (전체를 보다가 귓속말 탭으로 돌아왔을 때 상대를 다시 고르지 않아도 된다).
    private int _whisperTargetId;
    private string _whisperTargetName = "";

    // 팝업을 열기 전에 지정한 귓속말 대상(프로필에서 바로 열 때).
    private int _entryWhisperId;
    private string _entryWhisperName = "";
    private readonly List<ChatRowView> _rows = new();
    private readonly List<ChatWhisperPartnerView> _partnerChips = new();
    private CompositeDisposable _subs;

    // ─ 열기 ───────────────────────────────────────────
    /// <summary>지금 떠 있는 채팅창. 없으면 null.</summary>
    private static ChatPopup _current;

    /// <summary>여는 중(프리팹 로드 대기). 로드가 끝나기 전에 또 누르는 것을 막는다.</summary>
    private static bool _opening;

    /// <summary>
    /// 채팅창을 연다. 이미 떠 있으면 새로 만들지 않고 그 창을 쓴다.
    ///
    /// 채팅창은 프로필 팝업을 띄우고, 그 프로필의 귓속말 버튼이 다시 채팅창을 연다. 이 되돌이를 부르는 쪽에서
    /// 막게 하면 입구가 늘 때마다 같은 검사를 다시 적어야 하고, 한 곳이라도 빠지면 창이 겹쳐 쌓인다.
    /// 그래서 채팅창 자신이 유일한 입구를 갖는다.
    /// </summary>
    /// <param name="whisperAccountId">0 이 아니면 그 상대의 귓속말 탭으로 연다.</param>
    public static async UniTask<ChatPopup> OpenAsync(MainHudContext ctx, AssetReference reference,
                                                     int whisperAccountId = 0, string whisperNickname = null,
                                                     CancellationToken ct = default) {
        if (ctx == null) return null;

        // 이미 떠 있으면 그 창에 대상만 넘긴다. 새로 만들면 뒤에 남은 창이 그대로 쌓인다.
        if (_current != null) {
            if (whisperAccountId != 0) _current.OpenWhisperWith(whisperAccountId, whisperNickname);
            return _current;
        }
        if (_opening) return null;

        if (reference == null || !reference.RuntimeKeyIsValid()) { ctx.Ui.ShowToast("준비 중입니다."); return null; }

        _opening = true;
        try {
            var popup = await ctx.Ui.ShowPopupAsync<ChatPopup>(reference, ct);
            if (whisperAccountId != 0) popup.SetEntryWhisper(whisperAccountId, whisperNickname);
            popup.Initialize(ctx);
            await popup.ShowAsync();
            return popup;
        } finally {
            _opening = false;
        }
    }

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;
        _ct = this.GetCancellationTokenOnDestroy();
        _current = this;

        if (!_wired) {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (sendButton != null)
                sendButton.onClick.AddListener(Send);
            if (inputField != null) {
                inputField.characterLimit = ChatRules.MaxLength;   // 서버 규칙과 같은 값으로 입력 자체를 막는다.

                // 엔터(휴대폰 키보드의 확인/전송)로 바로 보낸다. 버튼을 따로 누르게 하면 한 줄 칠 때마다
                // 손이 화면을 오가야 해서 대화가 끊긴다.
                //
                // 줄바꿈이 가능한 입력 형태면 엔터가 개행으로 먹혀 이 이벤트 자체가 오지 않으므로,
                // 한 줄 입력으로 못 박는다(채팅은 어차피 한 줄이고, 서버도 줄바꿈을 공백으로 바꾼다).
                inputField.lineType = TMP_InputField.LineType.SingleLine;
                inputField.onSubmit.AddListener(_ => Send());
            }
            WireTabs();
            _wired = true;
        }

        _subs?.Dispose();
        _subs = new CompositeDisposable();
        // 창이 떠 있는 동안 들어오는 대화를 바로 붙인다(전체를 다시 그리지 않는다).
        _hud.Chat.Received.Subscribe(OnReceived).AddTo(_subs);

        // 프로필에서 귓속말로 열었으면 그 상대를 대상으로 잡고 귓속말 탭으로 바로 들어간다.
        bool entered = _entryWhisperId != 0;
        if (entered) {
            SelectPartner(_entryWhisperId, _entryWhisperName, rebuild: false);
            _entryWhisperId = 0;
            _entryWhisperName = "";
        }

        SelectChannel(entered ? ChatChannel.Whisper
                              : tabs != null && tabs.Length > 0 ? tabs[0].channel : ChatChannel.World);

        // 창을 열 때 최근 기록을 한 번 맞춘다(그 사이 서버에서 흘러간 대화를 채운다).
        RefreshAsync().Forget();
    }

    private void WireTabs() {
        if (tabs == null) return;
        for (int i = 0; i < tabs.Length; i++) {
            var tab = tabs[i];
            if (tab.button == null) continue;
            var channel = tab.channel;
            tab.button.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); SelectChannel(channel); });
        }
    }

    private async UniTaskVoid RefreshAsync() {
        await _hud.Chat.RefreshAsync(_ct);
        Rebuild();
    }

    // ── 채널 ─────────────────────────────────────────────
    private void SelectChannel(ChatChannel channel) {
        _channel = channel;

        if (tabs != null) {
            foreach (var tab in tabs)
                if (tab.selectedIndicator != null) tab.selectedIndicator.SetActive(tab.channel == channel);
        }

        // 길드 미가입 안내는 서버가 거부해 준 뒤에만 켠다. 클라가 소속을 따로 들고 있지 않아서,
        // 미리 판단하려면 채팅창을 열 때마다 길드 정보를 받아 와야 한다(그 값이 채팅과 무관하게 낡을 수도 있다).
        // 실제로 말해 보고 막히는 순간에 안내하는 편이 항상 정확하다.
        ApplyNoGuild(false);
        ApplyWhisperTarget();
        Rebuild();
    }

    /// <summary>
    /// Initialize 전에 호출하면 그 상대를 귓속말 대상으로 잡고 귓속말 탭으로 열린다(유저 프로필의 귓속말 버튼).
    /// </summary>
    public void SetEntryWhisper(int accountId, string nickname) {
        _entryWhisperId = accountId;
        _entryWhisperName = nickname ?? "";
    }


    /// <summary>
    /// 귓속말 탭의 대화 상대 칩과 입력 상태를 맞춘다.
    ///
    /// 칩이 곧 대화방이다. 여러 명과 동시에 귓말할 때 한 줄기로 섞이면 상대를 오갈 수 없으므로,
    /// 상대를 고르면 그 사람과의 대화만 목록에 남고 입력 대상도 그 사람이 된다.
    /// </summary>
    private void ApplyWhisperTarget() {
        bool whisper = _channel == ChatChannel.Whisper;
        if (whisperGroup != null) whisperGroup.SetActive(whisper);

        var partners = whisper ? WhisperPartnersWithTarget() : null;

        // 대상이 아직 없으면 가장 최근에 대화한 상대를 연다(창을 열자마자 이어서 말할 수 있게).
        if (whisper && _whisperTargetId == 0 && partners.Count > 0)
            SelectPartner(partners[0].AccountId, partners[0].Nickname, rebuild: false);

        RebuildPartnerChips(partners);

        // 상대 칩 줄은 귓속말 탭에서, 그것도 보여 줄 칩이 있을 때만 연다. 대화한 적이 없으면
        // 빈 띠만 덩그러니 남아 무엇을 하는 자리인지 알 수 없다.
        var barRoot = whisperPartnerBarRoot != null
            ? whisperPartnerBarRoot
            : (whisperPartnerBar != null ? whisperPartnerBar.gameObject : null);
        if (barRoot != null) barRoot.SetActive(whisper && partners != null && partners.Count > 0);

        // 입력칸과 전송 버튼은 상대가 있든 없든 항상 둔다. 채팅창에서 입력칸이 사라지면 그 자체로
        // 고장처럼 보인다. 보낼 곳이 없을 때는 Send 가 안내만 하고 보내지 않는다.
        bool hasTarget = _whisperTargetId != 0;
        if (noWhisperTargetGroup != null) noWhisperTargetGroup.SetActive(whisper && !hasTarget);
    }

    /// <summary>
    /// 대화 상대 목록. 아직 한 마디도 오가지 않은 대상(프로필의 귓속말 버튼으로 막 들어온 상대)은
    /// 서비스가 알 수 없으므로 여기서 맨 앞에 끼워 넣는다. 그러지 않으면 "누구에게 보내는 중인지"가
    /// 화면 어디에도 보이지 않는다. 첫 줄을 주고받으면 그 뒤로는 서비스 목록에 자연히 들어온다.
    /// </summary>
    private IReadOnlyList<WhisperPartnerDto> WhisperPartnersWithTarget() {
        var partners = _hud.Chat.WhisperPartners();
        if (_whisperTargetId == 0) return partners;

        foreach (var p in partners)
            if (p.AccountId == _whisperTargetId) return partners;

        var list = new List<WhisperPartnerDto>(partners.Count + 1) {
            new WhisperPartnerDto { AccountId = _whisperTargetId, Nickname = _whisperTargetName },
        };
        list.AddRange(partners);
        return list;
    }

    private void RebuildPartnerChips(IReadOnlyList<WhisperPartnerDto> partners) {
        foreach (var chip in _partnerChips) if (chip != null) Destroy(chip.gameObject);
        _partnerChips.Clear();

        if (partners == null || whisperPartnerBar == null || whisperPartnerPrefab == null) return;

        foreach (var partner in partners) {
            var chip = Instantiate(whisperPartnerPrefab, whisperPartnerBar);
            chip.Bind(partner, partner.AccountId == _whisperTargetId, OnPartnerSelected, OnPartnerClosed);
            _partnerChips.Add(chip);
        }
    }

    private void OnPartnerSelected(int accountId, string nickname) {
        if (accountId == _whisperTargetId) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        SelectPartner(accountId, nickname, rebuild: true);
        ApplyWhisperTarget();
        FocusInput();
    }

    /// <summary>
    /// 대화 상대 칩의 닫기(X). 그 상대와의 귓속말을 지우고 칩을 없앤다.
    ///
    /// 지우는 것은 내 쪽뿐이다(상대의 대화창에는 그대로 남는다). 되돌릴 수 없으므로 한 번 묻는다.
    /// </summary>
    private void OnPartnerClosed(int accountId, string nickname) {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        DeleteWhisperAsync(accountId, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid DeleteWhisperAsync(int accountId, CancellationToken ct) {
        try {
            bool ok = await _hud.Chat.DeleteWhisperAsync(accountId, ct);
            if (!ok) { _hud.Ui.ShowToast("대화를 지우지 못했습니다."); return; }

            // 지운 상대를 보고 있었다면 대상을 비운다. 남은 대화가 있으면 Rebuild 가 첫 상대를 고른다.
            if (_whisperTargetId == accountId) {
                _whisperTargetId = 0;
                _whisperTargetName = "";
            }
            Rebuild();
            ApplyWhisperTarget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[Chat] 귓속말 삭제 실패: {e.Message}");
        }
    }

    /// <summary>발언 신고. 팝업에서 사유를 고른다.</summary>
    private void OpenReport(ChatMessageDto message) {
        if (message == null || reportPopup == null || !reportPopup.RuntimeKeyIsValid()) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenReportAsync(message, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenReportAsync(ChatMessageDto message, CancellationToken ct) {
        try {
            var popup = await _hud.Ui.ShowPopupAsync<ChatReportPopup>(reportPopup, ct);
            await popup.ShowAsync(_hud, message);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[Chat] 신고 팝업 실패: {e.Message}");
        }
    }

    // 대상을 바꾸고 그 상대의 대화를 읽은 것으로 표시한다(칩의 안 읽음 표식이 꺼진다).
    private void SelectPartner(int accountId, string nickname, bool rebuild) {
        _whisperTargetId = accountId;
        _whisperTargetName = nickname ?? "";
        _hud.Chat.MarkWhisperRead(accountId);
        if (rebuild) Rebuild();
    }

    // 귓속말 탭은 고른 상대와의 대화만 보여 준다. 다른 채널은 그 채널 전체를 보여 준다.
    private bool BelongsToCurrentView(ChatMessageDto message) {
        if (message == null || message.Channel != _channel) return false;
        if (_channel != ChatChannel.Whisper) return true;

        int partnerId = message.IsMine ? message.TargetAccountId : message.AccountId;
        return partnerId == _whisperTargetId;
    }

    // ── 목록 ─────────────────────────────────────────────
    private void Rebuild() {
        ClearRows();

        foreach (var m in _hud.Chat.Messages(_channel))
            if (BelongsToCurrentView(m)) AddRow(m);

        if (emptyGroup != null) emptyGroup.SetActive(_rows.Count == 0);
        ScrollToBottom();
    }

    private void OnReceived(ChatMessageDto message) {
        if (message == null) return;

        // 귓속말은 지금 보고 있지 않은 상대의 것도 오므로, 목록에 붙이지 않더라도 칩(안 읽음 표식)은 갱신한다.
        if (message.Channel == ChatChannel.Whisper && _channel == ChatChannel.Whisper) ApplyWhisperTarget();
        if (!BelongsToCurrentView(message)) return;

        // 보고 있는 대화에 붙는 줄은 그 자리에서 읽은 것으로 본다.
        if (message.Channel == ChatChannel.Whisper) _hud.Chat.MarkWhisperRead(_whisperTargetId);

        AddRow(message);
        if (emptyGroup != null) emptyGroup.SetActive(false);
        ScrollToBottom();
    }

    private void AddRow(ChatMessageDto message) {
        if (content == null) return;

        // 줄 종류에 맞는 프리팹을 고른다. 유저 대화와 공지는 생김새가 아예 달라 프리팹을 나눠 두었고,
        // 유저 대화도 내 말과 남의 말은 좌우 배치가 달라 다시 나눈다. 내 말 프리팹을 비워 두면
        // 남의 말 프리팹을 그대로 써서 나누기 전과 똑같이 보인다.
        ChatRowView prefab = message.IsNotice
            ? noticeRowPrefab
            : (message.IsMine && myUserRowPrefab != null ? myUserRowPrefab : userRowPrefab);
        if (prefab == null) return;

        // 해석하지 못한 공지(구버전 클라가 모르는 새 종류)는 빈 줄이 되므로 아예 만들지 않는다.
        if (message.IsNotice && string.IsNullOrEmpty(ChatNoticeFormat.Build(message, _hud))) return;

        var row = Instantiate(prefab, content);
        row.Bind(_hud, message, OpenProfile, OnWhisperReply, OpenReport);
        _rows.Add(row);

        // 화면에 남기는 줄도 버퍼와 같은 상한을 둔다(오래 열어 두어도 칸이 무한히 늘지 않게).
        while (_rows.Count > ChatRules.BufferSize) {
            if (_rows[0] != null) Destroy(_rows[0].gameObject);
            _rows.RemoveAt(0);
        }
    }

    // 새 대화는 맨 아래에 붙으므로 시선을 따라 내려 준다. 레이아웃이 확정된 뒤에 옮겨야 위치가 정확하다.
    private void ScrollToBottom() {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // ── 발언 ─────────────────────────────────────────────
    private void Send() {
        if (_sending || inputField == null) return;
        string text = inputField.text;

        // 빈 채로 엔터를 친 경우. 보내지는 않되 초점은 돌려준다(키보드가 내려가면 다시 눌러야 한다).
        if (string.IsNullOrWhiteSpace(text)) { FocusInput(); return; }

        // 귓속말은 받을 사람이 정해져야 보낼 수 있다. 입력칸은 늘 열어 두므로 여기서 걸러 안내한다.
        if (_channel == ChatChannel.Whisper && _whisperTargetId == 0) {
            _hud.Ui.ShowToast("귓속말을 보낼 상대를 먼저 선택하세요.");
            FocusInput();
            return;
        }

        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        SendAsync(text).Forget();
    }

    /// <summary>
    /// 입력창에 초점을 돌려준다. 연달아 말할 수 있게 하려는 것이다.
    ///
    /// 한 프레임 기다리는 이유: 엔터로 보낸 직후에는 입력창이 스스로 초점을 놓는 중이라,
    /// 같은 프레임에 다시 잡으면 그 처리에 덮여 키보드가 내려간다.
    /// </summary>
    private void FocusInput() => FocusInputAsync().Forget();

    private async UniTaskVoid FocusInputAsync() {
        try {
            await UniTask.Yield(PlayerLoopTiming.Update, _ct);
        } catch (OperationCanceledException) {
            return;
        }

        // 길드 미가입 안내로 입력창이 숨겨진 상태면 초점을 잡지 않는다.
        if (inputField != null && inputField.gameObject.activeInHierarchy)
            inputField.ActivateInputField();
    }

    private async UniTaskVoid SendAsync(string text) {
        _sending = true;
        if (sendButton != null) sendButton.interactable = false;
        try {
            var res = await _hud.Chat.SendAsync(_channel, text, _whisperTargetId, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(ChatRules.FailMessage(res.FailReason));
                // 길드가 없어서 막힌 것이면 입력창을 안내로 바꾼다(계속 두드려도 같은 결과다).
                if (res.FailReason == GuildRequiredFail) ApplyNoGuild(true);
                return;
            }
            // 보낸 말은 서버 푸시로 되돌아와 목록에 붙는다(표시 경로가 하나뿐이라 내 말만 다르게 보일 일이 없다).
            if (inputField != null) inputField.text = "";
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("메시지를 보내지 못했습니다.");
        } finally {
            _sending = false;
            if (sendButton != null) sendButton.interactable = true;
            // 성공이든 실패든 초점을 돌려준다. 도배 간격에 걸려 실패한 경우에도 방금 친 말이 남아 있어
            // 잠시 뒤 그대로 다시 보낼 수 있어야 한다.
            FocusInput();
        }
    }

    // 귓속말 줄의 답장. 그 대화의 상대를 대상으로 잡고 귓속말 탭으로 옮긴다.
    private void OnWhisperReply(int accountId, string nickname) {
        if (accountId == 0) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenWhisperWith(accountId, nickname);
    }

    /// <summary>떠 있는 창을 그 상대의 귓속말 탭으로 옮긴다(프로필의 귓속말 버튼, 줄의 답장 버튼).</summary>
    public void OpenWhisperWith(int accountId, string nickname) {
        if (accountId == 0) return;
        SelectPartner(accountId, nickname, rebuild: false);
        SelectChannel(ChatChannel.Whisper);
        FocusInput();
    }

    // 길드 미가입 안내 표시. 켜면 입력창 대신 안내가 보인다.
    private void ApplyNoGuild(bool noGuild) {
        if (inputField != null) inputField.gameObject.SetActive(!noGuild);
        if (sendButton != null) sendButton.gameObject.SetActive(!noGuild);
        if (noGuildGroup != null) noGuildGroup.SetActive(noGuild);
    }

    // ── 프로필 ───────────────────────────────────────────
    private void OpenProfile(int accountId) {
        if (accountId <= 0) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenProfileAsync(accountId).Forget();
    }

    private async UniTaskVoid OpenProfileAsync(int accountId) {
        var reference = _hud.Popups.UserProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _hud.Ui.ShowPopupAsync<UserProfilePopup>(reference, _ct);
            // 프로필에서 차단했다면 그 상대의 말이 더는 오지 않는다(서버가 거른다).
            // 이미 화면에 남아 있는 줄은 목록을 다시 만들어 지운다.
            bool changed = await popup.ShowAsync(_hud, accountId, null);
            if (changed) {
                await _hud.Chat.RefreshAsync(_ct);
                Rebuild();
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void ClearRows() {
        foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
        _rows.Clear();
    }

    protected override void OnClosed() {
        _subs?.Dispose();
        _subs = null;
        ClearRows();
        if (_current == this) _current = null;
    }

    // 닫기를 거치지 않고 사라지는 경우(씬 전환 등)에도 자리를 비워 둬야 다음에 다시 열린다.
    private void OnDestroy() {
        if (_current == this) _current = null;
    }
}
