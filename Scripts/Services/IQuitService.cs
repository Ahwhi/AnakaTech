using Cysharp.Threading.Tasks;

/// <summary>
/// 게임 종료 계약. 종료 경로가 하나로 모이도록 "확인을 받고 끄는" 절차 전체를 이 서비스가 갖는다.
/// 안드로이드 뒤로가기(BackKeyRouter)와 화면의 종료 버튼(QuitButton)이 같은 창구를 쓰므로,
/// 확인 문구나 종료 방식이 바뀌어도 호출부는 손대지 않는다.
/// </summary>
public interface IQuitService {
    /// <summary>종료 확인 팝업을 띄우고, 확인을 받으면 앱을 끝낸다. 이미 확인 중이면 무시한다.</summary>
    UniTask RequestQuitAsync();
}
