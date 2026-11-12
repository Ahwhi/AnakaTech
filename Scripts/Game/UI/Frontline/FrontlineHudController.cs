using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 전선 씬 HUD 총괄. 상단 표시(FrontlineHud)와 하단 캐릭터 패널을 초기화하고, 햄버거 버튼으로
/// 일시정지 팝업(계속/나가기)을 띄운다. 판과 판 사이의 흐름은 FrontlineSceneManager 가 맡는다.
/// </summary>
public sealed class FrontlineHudController : MonoBehaviour {
    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [SerializeField] private FrontlineHud frontlineHud;
    [Tooltip("하단 캐릭터 패널(스쿼드 5명).")]
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [Tooltip("딜량 그래프 패널(다른 전투 씬과 같은 공용 패널). 판이 새로 시작될 때마다 0 부터 다시 센다.")]
    [SerializeField] private DamageMeterPanel damageMeterPanel;
    [Tooltip("두 손가락 핀치 줌.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    [Header("Menu")]
    [Tooltip("햄버거 버튼. 누르면 전투가 멈추고 계속/나가기 팝업이 뜬다.")]
    [SerializeField] private Button menuButton;

    private FrontlineHudContext _ctx;
    private bool _menuOpen;

    /// <summary>일시정지 메뉴에서 나가기를 골랐을 때.</summary>
    public event Action ExitRequested;

    [Inject]
    public void Construct(FrontlineHudContext ctx) => _ctx = ctx;

    private void Start() {
        if (_ctx == null) {
            Debug.LogError("[FrontlineHudController] 의존성 주입이 되지 않았습니다. " +
                           "FrontlineLifetimeScope 의 Configure 에 RegisterComponentInHierarchy<FrontlineHudController>() 가 있는지, " +
                           "이 오브젝트가 씬 로드 시점에 활성 상태인지 확인하세요.");
            return;
        }

        if (frontlineHud != null) frontlineHud.Initialize(_ctx);
        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (damageMeterPanel != null) damageMeterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);

        if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
    }

    /// <summary>판이 새로 시작됐을 때 전선 이름/단계 표시를 다시 맞춘다.</summary>
    public void RefreshForNewRun() {
        if (frontlineHud != null) frontlineHud.Refresh();
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
            // 도중 재시작은 허용하지 않는다(계속/나가기만).
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
