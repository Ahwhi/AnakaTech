using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 영웅전 씬 HUD 총괄. 상단 표시(HeroesHud)를 초기화하고, 터치 조이스틱(PC 는 방향키)을 무대의 이동 입력으로 연결하며,
/// 햄버거 버튼으로 일시정지 팝업(계속/나가기)을 띄운다. 판의 결말 처리는 HeroesSceneManager 가 맡는다.
/// </summary>
public sealed class HeroesHudController : MonoBehaviour {
    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [SerializeField] private HeroesHud heroesHud;
    [Tooltip("하단 캐릭터 패널(선택). 조작 중인 소녀의 체력/스킬 게이지를 보여 준다.")]
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [Tooltip("딜량 그래프(선택).")]
    [SerializeField] private DamageMeterPanel damageMeterPanel;
    [Tooltip("두 손가락 핀치 줌(선택). 조이스틱 터치 영역 위의 손가락은 UI 로 보고 무시하므로, 조이스틱 밖 화면에서 동작한다.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    [Header("Control")]
    [Tooltip("터치 조이스틱. 비워도 PC 방향키/WASD 로는 움직일 수 있다.")]
    [SerializeField] private VirtualJoystick joystick;

    [Header("Menu")]
    [Tooltip("햄버거 버튼. 누르면 전투가 멈추고 계속/나가기 팝업이 뜬다.")]
    [SerializeField] private Button menuButton;

    private HeroesHudContext _ctx;
    private bool _menuOpen;

    /// <summary>일시정지 메뉴에서 나가기를 골랐을 때.</summary>
    public event Action ExitRequested;

    [Inject]
    public void Construct(HeroesHudContext ctx) => _ctx = ctx;

    private void Start() {
        if (_ctx == null) {
            Debug.LogError("[HeroesHudController] 의존성 주입이 되지 않았습니다. " +
                           "HeroesLifetimeScope 의 Configure 에 RegisterComponentInHierarchy<HeroesHudController>() 가 있는지, " +
                           "이 오브젝트가 씬 로드 시점에 활성 상태인지 확인하세요.");
            return;
        }

        if (heroesHud != null) heroesHud.Initialize(_ctx.Director);
        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (damageMeterPanel != null) damageMeterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);
        _ctx.Director.SetMoveInput(new DirectionalMoveInput(joystick));

        if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
    }

    /// <summary>판이 새로 시작됐을 때(첫 판/다음 단계/재도전) 단계 표시를 다시 맞춘다.</summary>
    public void RefreshForNewRun() {
        if (heroesHud != null) heroesHud.Refresh();
    }

    private void OpenMenu() {
        if (_menuOpen) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenMenuAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 일시정지 팝업은 의뢰 씬의 것을 그대로 쓴다(계속/나가기).
    private async UniTaskVoid OpenMenuAsync(CancellationToken ct) {
        _menuOpen = true;
        _ctx.Director.SetPaused(true);
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<CommissionPausePopup>(_ctx.Popups.CommissionPausePopup, ct);
            // 도중 재시작은 허용하지 않는다(계속/나가기만). 도전 횟수가 걸린 컨텐츠다.
            var result = await popup.ShowAsync(allowRetry: false);

            if (result == CommissionPausePopup.Result.Exit) {
                ExitRequested?.Invoke();
                return;   // 씬을 떠나므로 일시정지를 풀 필요가 없다.
            }
            _ctx.Director.SetPaused(false);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Director.SetPaused(false);
        } finally {
            _menuOpen = false;
        }
    }
}
