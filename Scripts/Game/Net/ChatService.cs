using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 채팅 통신 구현. 수신(서버 푸시)을 상시 받아 채널별 버퍼에 쌓고, 발언은 요청-응답으로 보낸다.
///
/// 버퍼를 서비스가 들고 있는 이유: 채팅창은 필요할 때만 열리는 화면이라 거기에 대화를 쌓아 두면
/// 창을 닫는 순간 그 사이의 대화가 사라진다. 메인 화면의 "최근 한 줄"도 같은 값을 봐야 하므로
/// 화면보다 오래 사는 이 자리에 둔다.
///
/// 차단/길드 필터링은 서버가 이미 하고 온다. 클라가 다시 거를 것은 없다
/// (차단 목록을 클라가 들고 있지 않아도 되고, 두 곳의 판정이 어긋날 일도 없다).
/// </summary>
public sealed class ChatService : IChatService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    // 채널별 버퍼(오래된 것 -> 최신). 채널 수가 둘뿐이라 배열 대신 사전으로 두어 채널이 늘어도 그대로 동작한다.
    private readonly Dictionary<ChatChannel, List<ChatMessageDto>> _buffers = new();
    private readonly Subject<ChatMessageDto> _received = new();
    private readonly ReactiveProperty<ChatMessageDto> _latest = new(null);

    // 이미 받은 줄의 순번. 재조회(History)와 푸시가 겹칠 때 같은 줄이 두 번 쌓이는 것을 막는다.
    private readonly HashSet<long> _seenIds = new();

    // 귓속말 상대별로 마지막까지 읽은 줄의 순번은 계정 로컬 데이터(AccountLocalData.whisperReadMarks)에 둔다.
    // 서버가 재접속 때 최근 귓속말을 다시 보내 주므로, 메모리에만 두면 재접속마다 읽은 대화가 안 읽음으로 되살아난다.

    // 안 읽은 귓속말 줄 수(모든 상대 합). 메인 화면 채팅 줄의 배지가 구독한다.
    // 귓속말이 들어오거나 / 읽음 표시가 바뀌거나 / 대화를 지우면 다시 센다.
    private readonly ReactiveProperty<int> _unreadWhispers = new(0);

    // 내 계정 id. 최근 대화 응답으로 알게 되며, 그 전에 받은 대화는 알게 된 시점에 다시 표시한다.
    private int _myAccountId;

    public IObservable<ChatMessageDto> Received => _received;
    public IReadOnlyReactiveProperty<ChatMessageDto> Latest => _latest;
    public int MyAccountId => _myAccountId;
    public IReadOnlyReactiveProperty<int> UnreadWhisperCount => _unreadWhispers;

    public ChatService(INetworkService net) {
        _net = net;
        _net.PacketReceived += OnPacketReceived;
    }

    public IReadOnlyList<ChatMessageDto> Messages(ChatChannel channel) =>
        _buffers.TryGetValue(channel, out var list) ? list : Array.Empty<ChatMessageDto>();

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_ChatHistory>(cts.Token);
            _net.Send(new C_ChatHistory().Write());
            var res = await wait;

            SetMyAccountId(res.myAccountId);

            foreach (var m in res.messages)
                Accept(ToDto(m.messageId, m.channel, m.type, m.noticeKind, m.accountId, m.nickname,
                             m.profileFrameId, m.representativeCharId, m.representativeCostumeId,
                             m.text, m.refId, m.refValue, m.createdUnix,
                             m.targetAccountId, m.targetNickname), notify: false);
        } catch (OperationCanceledException) {
            // 화면 전환/타임아웃. 이후 들어오는 대화는 푸시로 계속 쌓이므로 조용히 넘어간다.
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    public async UniTask<ChatSendResult> SendAsync(ChatChannel channel, string text, int targetAccountId = 0,
                                                   CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ChatSendResult>(cts.Token);
        _net.Send(new C_ChatSend {
            channel = (int)channel, text = text ?? "", targetAccountId = targetAccountId,
        }.Write());
        var res = await wait;

        return new ChatSendResult { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<ChatReportResult> ReportAsync(long messageId, ChatReportReason reason, string reasonText,
                                                       CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ReportChatResult>(cts.Token);
        _net.Send(new C_ReportChat {
            messageId = messageId, reason = (int)reason, reasonText = reasonText ?? "",
        }.Write());
        var res = await wait;

        return new ChatReportResult { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<bool> DeleteWhisperAsync(int partnerAccountId, CancellationToken ct = default) {
        if (partnerAccountId == 0) return false;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DeleteWhisperResult>(cts.Token);
        _net.Send(new C_DeleteWhisper { partnerAccountId = partnerAccountId }.Write());
        var res = await wait;

        // 서버가 지웠으면 화면에서도 즉시 사라져야 한다. 다시 받아 오지 않고 버퍼에서 바로 뺀다.
        if (res.isSuccess) ForgetWhisperWith(partnerAccountId);
        return res.isSuccess;
    }

    // 그 상대와의 귓속말을 버퍼에서 뺀다. 서버에서 지운 뒤 화면을 맞추기 위한 것이다.
    private void ForgetWhisperWith(int partnerAccountId) {
        if (!_buffers.TryGetValue(ChatChannel.Whisper, out var list)) return;

        for (int i = list.Count - 1; i >= 0; i--) {
            if (PartnerOf(list[i]) != partnerAccountId) continue;
            _seenIds.Remove(list[i].MessageId);
            list.RemoveAt(i);
        }
        var account = UserDataManager.Account;
        if (account != null) {
            account.RemoveWhisperReadMark(partnerAccountId);
            UserDataManager.RequestSave();
        }
        RecountUnreadWhispers();
    }

    // ── 귓속말 대화 상대 ─────────────────────────────────
    /// <summary>
    /// 버퍼에 있는 귓속말에서 대화 상대를 뽑아 최근 대화 순으로 돌려준다.
    ///
    /// 대화방을 따로 만들어 두지 않는 이유: 귓속말은 보관하지 않는 대화라(재접속하면 사라진다)
    /// 방을 관리하면 버퍼가 비워진 뒤에도 빈 방만 남는다. 버퍼가 곧 대화 목록이면 그럴 일이 없다.
    /// </summary>
    public IReadOnlyList<WhisperPartnerDto> WhisperPartners() {
        var byId = new Dictionary<int, WhisperPartnerDto>();

        foreach (var m in Messages(ChatChannel.Whisper)) {
            int partnerId = PartnerOf(m);
            if (partnerId == 0) continue;

            if (!byId.TryGetValue(partnerId, out var partner)) {
                partner = new WhisperPartnerDto { AccountId = partnerId };
                byId[partnerId] = partner;
            }
            // 닉네임은 마지막에 본 것으로 맞춘다(상대가 도중에 바꿨어도 최신 이름이 남는다).
            partner.Nickname = m.WhisperPartnerNickname;
            if (m.MessageId > partner.LastMessageId) partner.LastMessageId = m.MessageId;

            // 내가 보낸 줄은 읽고 말고가 없다. 받은 줄만 마지막으로 읽은 지점과 견준다.
            if (!m.IsMine && m.MessageId > ReadMarkOf(partnerId)) partner.Unread++;
        }

        var list = new List<WhisperPartnerDto>(byId.Values);
        list.Sort((a, b) => b.LastMessageId.CompareTo(a.LastMessageId));
        return list;
    }

    public void MarkWhisperRead(int accountId) {
        if (accountId == 0) return;

        long last = 0;
        foreach (var m in Messages(ChatChannel.Whisper))
            if (PartnerOf(m) == accountId && m.MessageId > last) last = m.MessageId;

        var account = UserDataManager.Account;
        if (last > 0 && account != null && last > account.WhisperReadMarkOf(accountId)) {
            account.SetWhisperReadMark(accountId, last);
            UserDataManager.RequestSave();
        }
        RecountUnreadWhispers();
    }

    // 받은 귓속말 중 그 상대의 읽음 표시보다 뒤에 있는 줄을 센다(WhisperPartners 의 Unread 합과 같은 기준).
    private void RecountUnreadWhispers() {
        int unread = 0;
        foreach (var m in Messages(ChatChannel.Whisper))
            if (!m.IsMine && m.MessageId > ReadMarkOf(PartnerOf(m))) unread++;
        _unreadWhispers.Value = unread;
    }

    // 이 줄의 대화 상대(내가 보냈으면 받는 사람, 받았으면 보낸 사람).
    private static int PartnerOf(ChatMessageDto m) =>
        m == null || !m.IsWhisper ? 0 : (m.IsMine ? m.TargetAccountId : m.AccountId);

    private static long ReadMarkOf(int accountId) =>
        UserDataManager.Account != null ? UserDataManager.Account.WhisperReadMarkOf(accountId) : 0;

    // 소셜 설정은 조회와 저장이 같은 쌍을 쓴다(write 로만 갈린다).
    public UniTask<WhisperPolicy> RequestWhisperPolicyAsync(CancellationToken ct = default) =>
        SocialSettingsAsync(write: false, WhisperPolicy.All, ct);

    public UniTask<WhisperPolicy> SetWhisperPolicyAsync(WhisperPolicy policy, CancellationToken ct = default) =>
        SocialSettingsAsync(write: true, policy, ct);

    private async UniTask<WhisperPolicy> SocialSettingsAsync(bool write, WhisperPolicy policy, CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_SocialSettings>(cts.Token);
        _net.Send(new C_SocialSettings { write = write, whisperPolicy = (int)policy }.Write());
        var res = await wait;
        return (WhisperPolicy)res.whisperPolicy;
    }

    // 서버가 밀어 주는 한 줄(요청 없음). 내가 친 말도 이 경로로 되돌아오므로 표시가 한 곳에서만 일어난다.
    private void OnPacketReceived(IPacket packet) {
        if (packet is not S_ChatMessage m) return;

        Accept(ToDto(m.messageId, m.channel, m.type, m.noticeKind, m.accountId, m.nickname,
                     m.profileFrameId, m.representativeCharId, m.representativeCostumeId,
                     m.text, m.refId, m.refValue, m.createdUnix,
                     m.targetAccountId, m.targetNickname), notify: true);
    }

    // 내 계정 id 를 알게 되면 이미 받아 둔 대화의 표시도 함께 맞춘다.
    // 최근 대화를 받기 전에 도착한 발언은 그때까지 "남의 말"로 표시되어 있기 때문이다.
    private void SetMyAccountId(int accountId) {
        if (accountId <= 0 || _myAccountId == accountId) return;
        _myAccountId = accountId;

        foreach (var list in _buffers.Values)
            foreach (var m in list) m.IsMine = m.AccountId == accountId;
        RecountUnreadWhispers();   // 내 말로 바뀐 줄은 안 읽음에서 빠진다
    }

    private void Accept(ChatMessageDto message, bool notify) {
        if (message == null || !_seenIds.Add(message.MessageId)) return;

        message.IsMine = _myAccountId != 0 && message.AccountId == _myAccountId;

        if (!_buffers.TryGetValue(message.Channel, out var list)) {
            list = new List<ChatMessageDto>(ChatRules.BufferSize);
            _buffers[message.Channel] = list;
        }
        list.Add(message);

        // 오래된 줄을 버린다. 버린 줄의 순번은 다시 들어올 일이 없으므로 중복 판정 집합에서도 뺀다
        // (집합이 접속 내내 무한히 커지지 않게 한다).
        while (list.Count > ChatRules.BufferSize) {
            _seenIds.Remove(list[0].MessageId);
            list.RemoveAt(0);
        }

        // 채널을 가리지 않고 가장 최근 줄을 들고 있는다. 어떤 채널을 보여 줄지는 화면의 판단이라
        // 여기서 미리 거르지 않는다(메인 화면 한 줄은 귓속말을 빼고 보여 준다 - ChatLineView).
        //
        // "들어온 순서"가 아니라 "순번(MessageId)"으로 견준다. 최근 대화 응답은 채널별 묶음으로 오기 때문이다
        // (서버 Room.ChatHistory: 전체 -> 길드 -> 귓속말). 도착 순서대로 덮으면 마지막 묶음의 끝 줄이
        // 최신으로 남아, 전체 채널에 더 새로운 말이 있어도 길드 대화가 최근으로 보인다.
        // 순번은 채팅 로그 한 표의 auto-increment 라 채널이 달라도 그대로 견줄 수 있다(시간순 = 순번순).
        if (_latest.Value == null || message.MessageId > _latest.Value.MessageId)
            _latest.Value = message;

        // 알림보다 먼저 센다. 채팅창이 이 줄을 보고 있으면 알림을 받자마자 읽음으로 바꿔 다시 0 이 된다.
        if (message.Channel == ChatChannel.Whisper) RecountUnreadWhispers();

        if (notify) _received.OnNext(message);
    }

    private static ChatMessageDto ToDto(long messageId, int channel, int type, int noticeKind, int accountId,
                                        string nickname, int frameId, int repCharId, int repCostumeId,
                                        string text, int refId, int refValue, long createdUnix,
                                        int targetAccountId = 0, string targetNickname = "") => new() {
        MessageId = messageId,
        Channel = (ChatChannel)channel,
        Type = (ChatMessageType)type,
        NoticeKind = (ChatNoticeKind)noticeKind,
        AccountId = accountId,
        Nickname = nickname ?? "",
        ProfileFrameId = frameId,
        RepresentativeCharId = repCharId,
        RepresentativeCostumeId = repCostumeId,
        Text = text ?? "",
        RefId = refId,
        RefValue = refValue,
        CreatedUnix = createdUnix,
        TargetAccountId = targetAccountId,
        TargetNickname = targetNickname ?? "",
    };

    public void Dispose() {
        _net.PacketReceived -= OnPacketReceived;
        _received.Dispose();
        _latest.Dispose();
        _unreadWhispers.Dispose();
    }
}
