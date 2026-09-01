using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 의뢰 씬 HUD 총괄. 상단 게이지/하단 캐릭터 패널/딜량 패널을 초기화하고, 우측 상단 햄버거 버튼으로
/// 일시정지 팝업(계속/나가기)을 띄운다.
///
/// 의뢰 결과 처리(보상 수령 -> 결과 팝업 -> 다음 단계/나가기)는 CommissionSceneManager 가 담당한다.
/// 여기서는 "화면"만 책임진다(역할 분리).
/// </summary>
public sealed class CommissionHudController : MonoBehaviour {
    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [SerializeField] private CommissionHudPanel commissionPanel;
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [SerializeField] private DamageMeterPanel damageMeterPanel;
    [Tooltip("두 손가락 핀치 줌. 줌 버튼 대신 화면을 직접 벌려 확대/축소한다.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    [Header("Menu")]
    [Tooltip("우측 상단 햄버거 버튼. 누르면 전투가 멈추고 계속/나가기 팝업이 뜬다.")]
    [SerializeField] private Button menuButton;

    private CommissionHudContext _ctx;
    private bool _menuOpen;

    /// <summary>일시정지 메뉴에서 나가기를 골랐을 때 발생. 씬 매니저가 메인 씬 복귀를 처리한다.</summary>
    public event Action ExitRequested;

    /// <summary>일시정지 메뉴에서 재도전을 골랐을 때 발생. 씬 매니저가 현재 단계를 처음부터 다시 시작한다.</summary>
    public event Action RetryRequested;

    [Inject]
    public void Construct(CommissionHudContext ctx) => _ctx = ctx;

    private void Start() {
        // 주입이 안 된 채로 패널을 초기화하면 패널 안쪽에서 NullReference 가 나 원인을 찾기 어렵다.
        // 여기서 먼저 끊고, 무엇을 확인해야 하는지 알려 준다.
        if (_ctx == null) {
            Debug.LogError("[CommissionHudController] 의존성 주입이 되지 않았습니다. " +
                           "CommissionLifetimeScope 의 Configure 에 RegisterComponentInHierarchy<CommissionHudController>() 가 있는지, " +
                           "이 오브젝트가 씬 로드 시점에 활성 상태인지 확인하세요(비활성 오브젝트는 검색되지 않습니다).");
            return;
        }

        if (commissionPanel != null) commissionPanel.Initialize(_ctx);
        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (damageMeterPanel != null) damageMeterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);

        if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
    }

    /// <summary>다음 단계로 이어서 시작했을 때 제목 등 의뢰 종속 표시를 다시 그린다.</summary>
    public void RefreshForNewCommission() {
        if (commissionPanel != null) commissionPanel.RefreshTitle();
    }

    private void OpenMenu() {
        if (_menuOpen) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenMenuAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenMenuAsync(CancellationToken ct) {
        _menuOpen = true;
        _ctx.Director.SetPaused(true);
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<CommissionPausePopup>(_ctx.Popups.CommissionPausePopup, ct);
            var result = await popup.ShowAsync();

            if (result == CommissionPausePopup.Result.Exit) {
                ExitRequested?.Invoke();
                return;   // 씬을 떠나므로 일시정지를 풀 필요가 없다(오브젝트가 곧 파괴된다).
            }
            if (result == CommissionPausePopup.Result.Retry) {
                // 재시작이 전장을 통째로 다시 세우므로 먼저 일시정지를 푼다.
                // 멈춘 채로 두면 새로 생성된 유닛이 정지 상태로 남아 전투가 시작되지 않는다.
                _ctx.Director.SetPaused(false);
                RetryRequested?.Invoke();
                return;
            }
            _ctx.Director.SetPaused(false);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Director.SetPaused(false);   // 팝업을 못 띄웠는데 전투가 멈춘 채로 남지 않게 한다.
        } finally {
            _menuOpen = false;
        }
    }
}
