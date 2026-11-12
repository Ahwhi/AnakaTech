using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 이벤트 페이지 안의 돌림판 입구. 전용 재화(다트 등) 보유량을 보여 주고, 버튼으로 돌림판 팝업을 연다.
/// 미션 목록(EventPage_Missions)과 같은 페이지 프리팹에 함께 두며, 서로를 알지 않는다
/// (미션 수령으로 재화가 늘면 돌림판 서비스의 보유량이 바뀌고, 이 패널은 그 값을 구독할 뿐이다).
///
/// 이벤트 페이지는 루트 컨테이너로 주입되므로 HUD(보상 아이콘/스토어)는 현재 씬의 MainLifetimeScope 에서 직접 해석한다.
/// </summary>
public sealed class EventRoulettePanel : MonoBehaviour {
    [Tooltip("돌림판이 쓰는 이벤트 키(서버 EventRouletteCatalog 와 같은 값).")]
    [SerializeField] private string eventKey = EventMissionKeys.DailyRoulette;

    [Tooltip("전용 재화 보유량 표시. {0} 자리에 개수가 들어간다.")]
    [SerializeField] private TMP_Text tokenText;
    [SerializeField] private string tokenFormat = "보유 다트: {0}";

    [Tooltip("돌림판 팝업(UIPopupCatalog.EventRoulettePopup)을 여는 버튼.")]
    [SerializeField] private Button openButton;
    [Tooltip("돌릴 수 있는 재화가 있을 때 켤 표시(배지 등). 선택.")]
    [SerializeField] private GameObject readyMark;

    private IUIService _ui;
    private IEventRouletteService _roulette;
    private ISoundService _sound;
    private IEventBadgeService _badges;
    private MainHudContext _hud;
    private bool _opening;

    [Inject]
    public void Construct(IUIService ui, IEventRouletteService roulette, ISoundService sound, IEventBadgeService badges) {
        _ui = ui;
        _roulette = roulette;
        _sound = sound;
        _badges = badges;
    }

    private void Start() {
        if (_roulette == null) return;

        var scope = FindAnyObjectByType<MainLifetimeScope>();
        if (scope != null && scope.Container != null) {
            try { _hud = scope.Container.Resolve<MainHudContext>(); } catch { _hud = null; }
        }

        _roulette.TokensOf(eventKey).Subscribe(OnTokens).AddTo(this);
        if (openButton != null) openButton.onClick.AddListener(Open);
        _roulette.RefreshAsync(eventKey, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnTokens(int tokens) {
        if (tokenText != null) tokenText.text = string.Format(tokenFormat, tokens.ToString("N0"));
        if (readyMark != null) readyMark.SetActive(tokens > 0);
    }

    private void Open() {
        if (_opening) return;
        var reference = _hud != null && _hud.Popups != null ? _hud.Popups.EventRoulettePopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        _sound?.PlaySfx(SfxKey.ButtonClick);
        OpenAsync(reference, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenAsync(AssetReference reference, CancellationToken ct) {
        _opening = true;
        try {
            var popup = await _ui.ShowPopupAsync<EventRoulettePopup>(reference, ct);
            popup.Open(eventKey, _roulette, _ui, _sound, _badges, _hud);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("돌림판을 열지 못했습니다.");
        } finally {
            _opening = false;
        }
    }
}
