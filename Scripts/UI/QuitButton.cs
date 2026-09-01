using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 게임 종료 버튼(재사용). 아무 버튼 오브젝트에 붙이면 누를 때 종료 확인 팝업이 뜨고, 확인 시 앱이 종료된다.
/// 타이틀 화면, 메인 화면의 슬라이드 메뉴 등 어느 씬에서나 쓸 수 있다.
///
/// 종료 절차 자체는 IQuitService 가 갖는다(안드로이드 뒤로가기와 같은 창구). 이 컴포넌트는 "눌렀다"를 전달할 뿐이다.
/// 루트 스코프에서 스스로 주입받으므로(RootInjectedBehaviour) 씬 스코프에 등록할 필요가 없다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class QuitButton : RootInjectedBehaviour {
    [Tooltip("비우면 이 컴포넌트가 붙은 오브젝트의 Button 을 사용한다.")]
    [SerializeField] private Button button;

    private IQuitService _quit;
    private ISoundService _sound;

    [Inject]
    public void Construct(IQuitService quit, ISoundService sound) {
        _quit = quit;
        _sound = sound;
    }

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        _sound?.PlaySfx(SfxKey.ButtonClick);
        _quit?.RequestQuitAsync().Forget();
    }

    private void OnDestroy() {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }
}
