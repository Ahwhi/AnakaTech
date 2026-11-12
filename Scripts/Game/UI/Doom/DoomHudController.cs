using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 멸망전 씬 HUD 총괄. 상단 표시(DoomHud)와 하단 캐릭터 패널을 초기화하고, 우측 상단 햄버거 버튼으로
/// 일시정지 팝업(계속/나가기)을 띄운다.
///
/// 딜량 패널은 붙이지 않는다. 보유 소녀 전원이 나서는 컨텐츠라 목록이 화면을 덮기 때문이다.
/// 판의 결말 처리(점수 보고 -> 결과 안내 -> 복귀)는 DoomSceneManager 가 맡는다. 여기는 화면만 책임진다.
/// </summary>
public sealed class DoomHudController : MonoBehaviour {
    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [Tooltip("페이즈/점수/증강 목록 표시.")]
    [SerializeField] private DoomHud doomHud;
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [Tooltip("두 손가락 핀치 줌. 줌 버튼 대신 화면을 직접 벌려 확대/축소한다.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    [Header("Menu")]
    [Tooltip("우측 상단 햄버거 버튼. 누르면 전투가 멈추고 계속/나가기 팝업이 뜬다.")]
    [SerializeField] private Button menuButton;

    private DoomHudContext _ctx;
    private bool _menuOpen;

    /// <summary>일시정지 메뉴에서 나가기를 골랐을 때 발생. 씬 매니저가 점수 보고와 메인 복귀를 처리한다.</summary>
    public event Action ExitRequested;

    [Inject]
    public void Construct(DoomHudContext ctx) => _ctx = ctx;

    private void Start() {
        if (_ctx == null) {
            Debug.LogError("[DoomHudController] 의존성 주입이 되지 않았습니다. " +
                           "DoomLifetimeScope 의 Configure 에 RegisterComponentInHierarchy<DoomHudController>() 가 있는지, " +
                           "이 오브젝트가 씬 로드 시점에 활성 상태인지 확인하세요(비활성 오브젝트는 검색되지 않습니다).");
            return;
        }

        if (doomHud != null) doomHud.Initialize(_ctx.Director);
        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);

        if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
    }

    /// <summary>
    /// 역대 최고 점수를 HUD 에 넣는다. 계정 정보라 전투가 알 수 없고 씬 매니저가 서버에서 받아 넘겨 준다.
    /// HUD 가 없는 씬(테스트 등)에서도 안전하도록 널을 허용한다.
    /// </summary>
    public void SetBestScore(int bestScore) {
        if (doomHud != null) doomHud.SetBestScore(bestScore);
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
            // 도중 재시작은 허용하지 않는다(계속/나가기만). 참여 횟수가 걸린 컨텐츠다.
            var result = await popup.ShowAsync(allowRetry: false);

            if (result == CommissionPausePopup.Result.Exit) {
                ExitRequested?.Invoke();
                return;   // 씬을 떠나므로 일시정지를 풀 필요가 없다(오브젝트가 곧 파괴된다).
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
