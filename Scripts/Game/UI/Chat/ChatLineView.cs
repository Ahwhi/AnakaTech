using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 메인 화면의 채팅 한 줄. 가장 최근 대화 하나를 "닉네임: 내용" 으로 짧게 보여 주고, 누르면 채팅 패널을 연다.
///
/// 귓속말은 이 줄에 흘리지 않는다. 메인 화면은 늘 떠 있는 자리라 사적인 대화가 계속 노출되고,
/// 옆 사람에게 보이거나 화면을 찍었을 때 그대로 남는다.
/// 대신 안 읽은 귓속말이 있으면 이 줄의 배지(whisperBadge)를 켜서, 다른 채팅에 묻혀 온 줄 모르는 일이 없게 한다.
/// 내용은 보여 주지 않고 "왔다"는 사실만 알린다(ChatService.UnreadWhisperCount).
///
/// 길드 대화는 그대로 흐른다. 여러 사람이 함께 보는 말이라 이 자리에 있을 이유가 있으며,
/// 어느 채널인지 헷갈리지 않도록 채널마다 색을 달리한다.
///
/// 한 줄이라 프로필(테두리/초상)은 그리지 않는다. 좁은 자리에 아이콘까지 넣으면 정작 내용이 잘리기 때문이며,
/// 누가 말했는지를 제대로 보여 주는 것은 채팅 패널의 몫이다.
///
/// 알림(모집 성공/수련 성공)은 유저의 말과 색을 달리한다. 같은 줄에 섞여 흐르므로 색이 유일한 구분이다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class ChatLineView : HudInjectedBehaviour {
    [SerializeField] private Button button;
    [Tooltip("표시할 한 줄. 닉네임과 내용을 한 문자열로 넣는다(자리가 좁아 항목을 나누지 않는다).")]
    [SerializeField] private TMP_Text lineText;
    [Tooltip("아직 대화가 없을 때 보여 줄 안내 문구.")]
    [SerializeField] private string emptyMessage = "채팅을 눌러 대화에 참여하세요.";
    [Tooltip("유저 대화 글자색.")]
    [SerializeField] private Color userColor = Color.white;
    [Tooltip("시스템 알림 글자색. 유저 대화와 확실히 구분되는 색으로 둔다.")]
    [SerializeField] private Color noticeColor = new Color(1f, 0.85f, 0.35f);
    [Tooltip("길드 대화 글자색. 채팅창의 길드 채널과 같은 색으로 맞춘다.")]
    [SerializeField] private Color guildColor = new Color(0.45f, 0.9f, 0.55f);
    [Tooltip("이 줄을 눌렀을 때 열 채팅 패널 프리팹(ChatPopup).")]
    [SerializeField] private AssetReferenceGameObject chatPopup;
    [Tooltip("안 읽은 귓속말이 있을 때 켜는 배지. Source=Manual 로 둔다. 비우면 표시 안 함.")]
    [SerializeField] private BadgeView whisperBadge;

    private CompositeDisposable _subs;

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
    }

    protected override void OnInjected() {
        _subs?.Dispose();
        _subs = new CompositeDisposable();

        // 대화는 서비스가 들고 있는 값이라, 채팅 패널을 한 번도 열지 않았어도 채워진다.
        // 구독 직전의 최근 줄이 귓속말이면 아무것도 흘러오지 않으므로 첫 줄은 버퍼에서 직접 찾는다.
        Apply(LatestShowable());

        // 걸러진 줄(귓속말)이 오면 갱신하지 않고 직전 줄을 그대로 둔다. 빈 줄로 되돌리면
        // 귓속말이 오갈 때마다 멀쩡한 대화가 사라져 오히려 눈에 띈다.
        Ctx.Chat.Latest.Where(CanShow).Subscribe(Apply).AddTo(_subs);

        // 안 읽은 귓속말 배지. 채팅창에서 그 대화를 보면 서비스가 수를 줄이므로 여기서는 구독만 한다.
        if (whisperBadge != null)
            Ctx.Chat.UnreadWhisperCount.Subscribe(n => whisperBadge.SetOn(n > 0)).AddTo(_subs);
    }

    // 아직 대화가 없는 상태(null)도 통과시킨다. 안내 문구를 보여 주어야 하는 값이다.
    private static bool CanShow(ChatMessageDto message) =>
        message == null || message.Channel != ChatChannel.Whisper;

    /// <summary>
    /// 이 줄에 흐를 수 있는 가장 최근 대화. 채널이 늘어도 그대로 동작하도록 귓속말만 빼고 버퍼를 훑는다.
    /// 버퍼는 오래된 것 -> 최신 순이라 채널마다 마지막 줄끼리만 견주면 된다.
    /// </summary>
    private ChatMessageDto LatestShowable() {
        ChatMessageDto newest = null;

        foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel))) {
            if (channel == ChatChannel.Whisper) continue;

            var list = Ctx.Chat.Messages(channel);
            if (list.Count == 0) continue;

            var last = list[list.Count - 1];
            if (newest == null || last.MessageId > newest.MessageId) newest = last;
        }
        return newest;
    }

    private void Apply(ChatMessageDto message) {
        if (lineText == null) return;

        if (message == null) {
            lineText.text = emptyMessage;
            lineText.color = userColor;
            return;
        }

        if (message.IsNotice) {
            string notice = ChatNoticeFormat.Build(message, Ctx);
            // 해석하지 못한 알림(구버전 클라가 모르는 종류)은 빈 줄이 되므로 안내 문구를 유지한다.
            if (string.IsNullOrEmpty(notice)) return;
            lineText.text = notice;
            lineText.color = noticeColor;
            return;
        }

        lineText.text = $"{message.Nickname}: {message.Text}";
        lineText.color = ColorOf(message.Channel);
    }

    private Color ColorOf(ChatChannel channel) => channel switch {
        ChatChannel.Guild => guildColor,
        _ => userColor,
    };

    private void Open() {
        if (Ctx == null || chatPopup == null || !chatPopup.RuntimeKeyIsValid()) return;
        Ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 여는 판단(이미 떠 있으면 그 창을 쓴다)은 ChatPopup 이 갖는다. 여기서는 부탁만 한다.
    private async UniTaskVoid OpenAsync(CancellationToken ct) {
        await ChatPopup.OpenAsync(Ctx, chatPopup, ct: ct);
    }

    private void OnDestroy() => _subs?.Dispose();
}
