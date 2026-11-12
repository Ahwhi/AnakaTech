using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 무한 타워 씬 HUD 총괄. 상단 표시(TowerHud)와 하단 캐릭터 패널을 초기화하고, 햄버거 버튼으로
/// 일시정지 팝업(계속/나가기)을 띄운다. 판의 결말 처리는 TowerSceneManager 가 맡는다.
/// </summary>
public sealed class TowerHudController : MonoBehaviour {
    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [SerializeField] private TowerHud towerHud;
    [Tooltip("하단 캐릭터 패널. 순번 전체를 나열하고, 전장 밖 소녀는 대기/전투불능으로 표시한다(슬롯의 Waiting Group).")]
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [Tooltip("두 손가락 핀치 줌.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    [Header("Menu")]
    [Tooltip("햄버거 버튼. 누르면 전투가 멈추고 계속/나가기 팝업이 뜬다.")]
    [SerializeField] private Button menuButton;

    private TowerHudContext _ctx;
    private bool _menuOpen;

    /// <summary>일시정지 메뉴에서 나가기를 골랐을 때.</summary>
    public event Action ExitRequested;

    [Inject]
    public void Construct(TowerHudContext ctx) => _ctx = ctx;

    private void Start() {
        if (_ctx == null) {
            Debug.LogError("[TowerHudController] 의존성 주입이 되지 않았습니다. " +
                           "TowerLifetimeScope 의 Configure 에 RegisterComponentInHierarchy<TowerHudController>() 가 있는지, " +
                           "이 오브젝트가 씬 로드 시점에 활성 상태인지 확인하세요.");
            return;
        }

        if (towerHud != null) towerHud.Initialize(_ctx.Director);
        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);

        if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
    }

    /// <summary>판이 새로 시작됐을 때(첫 판/재도전) 층과 보너스 표시를 다시 맞춘다.</summary>
    public void RefreshForNewRun() {
        if (towerHud != null) towerHud.Refresh();
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
