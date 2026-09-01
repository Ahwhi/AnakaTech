using System;
using UnityEngine;

/// <summary>
/// 채팅 목록에 놓이는 한 줄의 공통 규약. 유저 대화(ChatUserRowView)와 공지(ChatNoticeRowView)가 상속한다.
///
/// 왜 나누는가: 두 줄은 생김새가 아예 다르다. 유저 대화는 프로필 + 닉네임 + 말풍선이고 공지는 안내문 한 줄이다.
/// 한 프리팹에서 묶음을 켜고 끄면 쓰지 않는 오브젝트가 항상 딸려 다니고, 한쪽 배치를 만질 때마다
/// 다른 쪽이 밀리지 않는지 확인해야 한다. 프리팹을 나누면 각자 자기 배치만 신경 쓰면 된다.
///
/// 목록(ChatPopup)은 이 베이스만 알고 있어서, 줄 종류가 늘어도 생성/정리 코드는 그대로다.
/// </summary>
public abstract class ChatRowView : MonoBehaviour {
    /// <summary>이 줄이 그린 대화.</summary>
    public ChatMessageDto Message { get; private set; }

    /// <param name="onProfile">발언자 프로필 열기(계정 id). 공지 줄은 쓰지 않는다.</param>
    /// <param name="onWhisperReply">귓속말 줄의 답장(상대 계정 id + 닉네임). 귓속말이 아닌 줄은 쓰지 않는다.</param>
    /// <param name="onReport">이 발언 신고하기. 공지 줄과 내 발언에는 쓰지 않는다.</param>
    public void Bind(BattleHudContext hud, ChatMessageDto message, Action<int> onProfile,
                     Action<int, string> onWhisperReply = null, Action<ChatMessageDto> onReport = null) {
        Message = message;
        if (message == null) return;
        Apply(hud, message, onProfile, onWhisperReply, onReport);
    }

    /// <summary>줄 종류별 표시. 베이스가 null 검사를 끝낸 뒤에만 호출된다.</summary>
    protected abstract void Apply(BattleHudContext hud, ChatMessageDto message, Action<int> onProfile,
                                  Action<int, string> onWhisperReply, Action<ChatMessageDto> onReport);
}
