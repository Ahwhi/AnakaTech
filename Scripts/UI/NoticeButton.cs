using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 공지/이벤트 팝업을 여는 재사용 버튼. 아무 버튼 오브젝트에 붙이고 모드만 고르면 동작한다.
///   - NoticePopup : 공지 + 이벤트 전체 목록 팝업(공지사항 버튼용)
///   - EventPopup  : 이벤트 전용 팝업(상태 배지 포함, 이벤트 버튼용)
///
/// 주입을 받으려면 이 컴포넌트를 쓰는 씬의 LifetimeScope 인스펙터의
/// "Auto Inject GameObjects" 에 버튼 오브젝트를 등록해야 한다(같은 타입이 여러 개여도 모두 주입됨).
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class NoticeButton : MonoBehaviour {
    private enum Mode {
        NoticePopup,
        EventPopup
    }

    [SerializeField] private Button button;
    [Tooltip("NoticePopup=공지+이벤트 전체, EventPopup=이벤트 전용(배지)")]
    [SerializeField] private Mode mode = Mode.NoticePopup;

    private INoticePresenter _presenter;
    private ISoundService _sound;

    [Inject]
    public void Construct(INoticePresenter presenter, ISoundService sound) {
        _presenter = presenter;
        _sound = sound;
    }

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
    }

    private void Open() {
        _sound?.PlaySfx(SfxKey.ButtonClick);
        var ct = this.GetCancellationTokenOnDestroy();
        if (mode == Mode.EventPopup)
            _presenter.OpenEventsAsync(ct).Forget();
        else
            _presenter.OpenNoticesAsync(ct).Forget();
    }
}
