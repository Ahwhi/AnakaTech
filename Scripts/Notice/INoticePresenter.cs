using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 공지/이벤트 화면을 여는 전역 진입점. 앱 어디서든(로비 버튼 등) 주입받아 호출한다.
/// 데이터 조회와 팝업 표시를 조합해 한 번의 호출로 화면을 완성한다.
/// 공지와 이벤트는 표현이 달라(이벤트는 상태 배지 포함) 메서드를 분리한다.
/// </summary>
public interface INoticePresenter {
    /// <summary>공지 + 이벤트 전체를 목록 팝업으로 연다.</summary>
    UniTask OpenNoticesAsync(CancellationToken ct = default);

    /// <summary>이벤트만 전용 팝업으로 연다(미열람/충족 상태 배지 포함).</summary>
    UniTask OpenEventsAsync(CancellationToken ct = default);
}
