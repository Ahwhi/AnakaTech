using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 이벤트 돌림판 팝업. 서버가 준 칸 목록으로 원판 위에 보상 칸을 둘러 세우고, 돌리면 서버가 뽑은 칸에서 멈춘다.
/// 어느 칸이 나올지는 서버가 정하며(균등 확률), 이 팝업은 그 결과를 연출로 보여 줄 뿐이다.
///
/// 배치: 0번 칸이 맨 위(12시)에 오고 시계 방향으로 이어진다. 바늘(포인터)은 원판 밖 맨 위에 고정해 두면 된다.
/// 원판(wheel)만 돌고 바늘은 돌지 않는다. 칸 수가 바뀌어도 각도는 자동으로 나뉜다.
/// </summary>
public sealed class EventRoulettePopup : UIView {
    [Header("Wheel")]
    [Tooltip("회전하는 원판. 보상 칸들이 이 아래에 생성된다. 바늘은 이 밖(돌지 않는 곳)에 둔다.")]
    [SerializeField] private RectTransform wheel;
    [Tooltip("칸 하나에 쓸 보상 슬롯 프리팹.")]
    [SerializeField] private RewardSlotView slotPrefab;
    [Tooltip("원판 중심에서 칸 중심까지의 거리.")]
    [SerializeField] private float slotRadius = 300f;
    [Tooltip("켜면 칸이 바깥을 향하도록 각도만큼 기운다. 끄면 모든 칸이 똑바로 선다.")]
    [SerializeField] private bool tiltSlots = true;

    [Header("Spin")]
    [SerializeField] private Button spinButton;
    [Tooltip("멈추기 전까지 도는 바퀴 수.")]
    [SerializeField, Min(1)] private int fullTurns = 6;
    [SerializeField, Min(0.5f)] private float spinDuration = 4.5f;
    [SerializeField] private Ease spinEase = Ease.OutQuart;
    [Tooltip("멈추는 위치를 칸 안에서 흔드는 정도(칸 너비 대비 0~0.45). 0 이면 항상 칸 정중앙에서 멈춘다.")]
    [SerializeField, Range(0f, 0.45f)] private float landJitter = 0.3f;

    [Header("Close")]
    [Tooltip("닫기 버튼. 도는 중에는 눌러도 닫히지 않는다(배경 탭/뒤로가기와 같은 기준).")]
    [SerializeField] private Button closeButton;

    [Header("Tokens")]
    [Tooltip("전용 재화 보유량 표시. {0} 자리에 개수가 들어간다.")]
    [SerializeField] private TMP_Text tokenText;
    [SerializeField] private string tokenFormat = "보유 다트: {0}";

    private readonly List<RewardSlotView> _slots = new();
    private readonly CompositeDisposable _subs = new();
    private CancellationToken _ct;

    private string _eventKey;
    private IEventRouletteService _roulette;
    private IUIService _ui;
    private ISoundService _sound;
    private IEventBadgeService _badges;
    private MainHudContext _hud;
    private int _slotCount;
    private int _tokens;
    private bool _spinning;
    private Tween _spinTween;

    protected override void Awake() {
        base.Awake();
        _ct = this.GetCancellationTokenOnDestroy();
        if (spinButton != null) spinButton.onClick.AddListener(OnSpin);
        if (closeButton != null) closeButton.onClick.AddListener(() => { _sound?.PlaySfx(SfxKey.ButtonClick); Dismiss(); });
    }

    // 도는 중에는 닫히지 않는다(결과를 못 보고 닫히면 무엇을 받았는지 모른다).
    protected override bool CanDismiss => !_spinning;

    /// <summary>열기 전에 부른다. 칸 목록과 보유량을 받아 원판을 그린다.</summary>
    public void Open(string eventKey, IEventRouletteService roulette, IUIService ui, ISoundService sound,
                     IEventBadgeService badges, MainHudContext hud) {
        _eventKey = eventKey;
        _roulette = roulette;
        _ui = ui;
        _sound = sound;
        _badges = badges;
        _hud = hud;

        _subs.Clear();
        _roulette.TokensOf(_eventKey).Subscribe(OnTokens).AddTo(_subs);
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        SetSpinInteractable(false);
        try {
            var info = await _roulette.RequestInfoAsync(_eventKey, _ct);
            BuildSlots(info.Slots);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ui?.ShowToast("돌림판 정보를 불러오지 못했습니다.");
        } finally {
            RefreshSpinButton();
        }
    }

    private void OnTokens(int tokens) {
        _tokens = tokens;
        if (tokenText != null) tokenText.text = string.Format(tokenFormat, tokens.ToString("N0"));
        RefreshSpinButton();
    }

    private float SlotAngle => _slotCount > 0 ? 360f / _slotCount : 0f;

    // i 번 칸을 맨 위에서 시계 방향으로 i * 칸각도 만큼 돌린 자리에 세운다.
    private void BuildSlots(IReadOnlyList<RewardItem> slots) {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        _slotCount = slots != null ? slots.Count : 0;
        if (wheel == null || slotPrefab == null || _slotCount == 0) return;

        wheel.localRotation = Quaternion.identity;
        for (int i = 0; i < _slotCount; i++) {
            float angle = i * SlotAngle;
            float rad = angle * Mathf.Deg2Rad;
            var view = Instantiate(slotPrefab, wheel);
            var rt = (RectTransform)view.transform;
            rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * slotRadius;
            rt.localRotation = tiltSlots ? Quaternion.Euler(0f, 0f, -angle) : Quaternion.identity;
            view.Bind(RewardVisualResolver.Resolve(_hud, slots[i]));
            _slots.Add(view);
        }
    }

    private void OnSpin() {
        if (_spinning) return;
        if (_tokens < 1) {
            _ui?.ShowToast($"{EventTokens.Name(_eventKey)}가 부족합니다.");
            return;
        }
        _sound?.PlaySfx(SfxKey.ButtonClick);
        SpinAsync().Forget();
    }

    private async UniTaskVoid SpinAsync() {
        _spinning = true;
        RefreshSpinButton();
        try {
            var res = await _roulette.SpinAsync(_eventKey, _ct);
            if (!res.IsSuccess || res.SlotIndex < 0 || res.SlotIndex >= _slotCount) {
                _ui?.ShowToast(FailMessage(res.FailReason));
                return;
            }

            await RotateToAsync(res.SlotIndex);

            ApplyToStore(res);
            // 이벤트 배지는 남은 다트까지 보고 켜지므로, 마지막 다트를 쓰면 여기서 꺼진다.
            _badges?.RefreshClaimStatesAsync(_ct).Forget();
            _sound?.PlaySfx(SfxKey.Claim);
            await ShowResultAsync(res.Granted);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ui?.ShowToast("돌림판을 돌리지 못했습니다.");
        } finally {
            _spinning = false;
            RefreshSpinButton();
        }
    }

    // 원판을 시계 방향으로 여러 바퀴 돌려 index 번 칸이 맨 위(바늘)에 오게 멈춘다.
    // i 번 칸은 시계 방향 i * 칸각도 자리에 있으므로, 원판의 z 회전이 그 각도(반시계)가 되면 맨 위로 온다.
    private UniTask RotateToAsync(int index) {
        float step = SlotAngle;
        float target = index * step + UnityEngine.Random.Range(-landJitter, landJitter) * step;
        float current = wheel.localEulerAngles.z;
        // 시계 방향(각도 감소)으로만 돌린다. 지금 각도에서 목표까지 남은 시계 방향 거리 + 바퀴 수.
        float clockwise = Mathf.Repeat(current - target, 360f) + 360f * fullTurns;

        var tcs = new UniTaskCompletionSource();
        _spinTween?.Kill();
        _spinTween = wheel.DOLocalRotate(new Vector3(0f, 0f, current - clockwise), spinDuration, RotateMode.FastBeyond360)
            .SetEase(spinEase)
            .SetUpdate(true)
            .OnComplete(() => tcs.TrySetResult())
            .OnKill(() => tcs.TrySetResult());
        return tcs.Task;
    }

    // 받은 보상을 HUD 스토어에 반영한다(패스/우편 수령과 같은 경로). 골드/다이아/기력은 서버 확정 잔액을 쓴다.
    private void ApplyToStore(EventRouletteSpinResultDto res) {
        if (_hud == null || _hud.Store == null) return;
        _hud.Store.ApplyGrantedRewards(res.Granted);
        _hud.Store.AddGrantedCurrencies(res.Granted);
        _hud.Store.Gold.Value = res.NewGold;
        _hud.Store.Diamond.Value = res.NewDiamond;
        _hud.Store.Stamina.Value = res.NewStamina;
    }

    private async UniTask ShowResultAsync(IReadOnlyList<RewardItem> granted) {
        if (granted == null || granted.Count == 0) return;

        var reference = _hud != null && _hud.Popups != null ? _hud.Popups.RewardInfoPopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            _ui?.ShowToast("보상을 받았습니다.");
            return;
        }
        var popup = await _ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
        popup.Show(_hud, "돌림판 보상", "돌림판에서 보상을 획득했습니다.",
                   new List<RewardInfoPopup.Row> { new("획득", granted, true) });
    }

    private void RefreshSpinButton() {
        SetSpinInteractable(!_spinning && _slotCount > 0);
        if (closeButton != null) closeButton.interactable = !_spinning;
    }

    private void SetSpinInteractable(bool on) {
        if (spinButton != null) spinButton.interactable = on;
    }

    private string FailMessage(int fail) => fail switch {
        1 => "이벤트가 종료되었습니다.",
        2 => $"{EventTokens.Name(_eventKey)}가 부족합니다.",
        _ => "돌림판을 돌리지 못했습니다."
    };

    protected override void OnClosed() {
        _spinTween?.Kill();
        _subs.Clear();
    }

    private void OnDestroy() => _subs.Dispose();
}
