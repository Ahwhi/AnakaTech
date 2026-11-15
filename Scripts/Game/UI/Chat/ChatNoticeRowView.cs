using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 채팅 패널의 공지 한 줄(모집 성공 / 수련 성공 등). 프로필 없이 안내문만 보여 준다.
///
/// 색과 배경은 이 프리팹이 스스로 들고 있는다. 유저 대화와 구분되는 것이 공지의 전부라
/// 코드가 색을 지정하는 것보다 프리팹에서 눈으로 맞추는 편이 낫다.
///
/// 문구는 ChatNoticeFormat 이 만든다. 서버는 무슨 일이 있었는지(종류 + 인자)만 보내고,
/// 캐릭터 이름 같은 표시용 문자열은 클라 데이터가 갖고 있기 때문이다.
/// </summary>
public sealed class ChatNoticeRowView : ChatRowView {
    [Tooltip("공지 문구. 색/서체는 이 프리팹이 그대로 쓴다(유저 대화와 구분되게 잡아 둘 것).")]
    [SerializeField] private TMP_Text noticeText;
    [Tooltip("한계돌파 알림 글자색. 그 밖의 공지는 프리팹에 잡아 둔 색을 그대로 쓴다.")]
    [SerializeField] private Color limitBreakColor = new Color(0.3f, 1f, 0.3f);

    // 프리팹에 잡아 둔 기본 공지색. 같은 줄을 다시 묶어도 한계돌파 색이 남지 않도록 처음 값을 기억해 둔다.
    private Color? _baseColor;

    protected override void Apply(BattleHudContext hud, ChatMessageDto message, Action<int> onProfile,
                                  Action<int, string> onWhisperReply, Action<ChatMessageDto> onReport) {
        if (noticeText == null) return;

        // 해석하지 못한 공지(구버전 클라가 모르는 새 종류)는 빈 문자열이 되므로 줄이 비어 보인다.
        // 목록이 그런 줄은 아예 만들지 않으므로 여기까지 오면 항상 내용이 있다.
        noticeText.text = ChatNoticeFormat.Build(message, hud);

        _baseColor ??= noticeText.color;
        noticeText.color = ChatNoticeFormat.IsLimitBreak(message) ? limitBreakColor : _baseColor.Value;
    }
}
