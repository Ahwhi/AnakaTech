using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 로그아웃 버튼(재사용). 아무 버튼 오브젝트에 붙이면 클릭 시 로그아웃한다(소켓 끊기 + 타이틀 복귀).
/// 타이틀/로비 등 어느 씬에서나 쓸 수 있다.
///
/// 주입을 받으려면 이 컴포넌트를 쓰는 씬의 LifetimeScope 에 등록해야 한다
/// (단일이면 RegisterComponentInHierarchy, 여러 개면 Auto Inject GameObjects).
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class LogoutButton : MonoBehaviour {
    [SerializeField] private Button button;

    private ILogoutService _logout;
    private ISoundService _sound;

    [Inject]
    public void Construct(ILogoutService logout, ISoundService sound) {
        _logout = logout;
        _sound = sound;
    }

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        _sound?.PlaySfx(SfxKey.ButtonClick);
        _logout.LogoutAsync().Forget();
    }
}
