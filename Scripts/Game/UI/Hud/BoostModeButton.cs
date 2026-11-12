using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 기력 부스트(터보) 모드 토글 버튼(메인 HUD). 누르면 팝업 없이 기본 모드 <-> 부스트 모드가 바로 바뀐다
/// (자동 전투 모드 버튼과 같은 조작감). 버튼 아래에는 보유 부스트권을 "N / 일일충전량" 으로 보여준다.
///
/// 켜짐 표시는 normalOnlyRoot / boostOnlyRoot 를 갈아 끼우는 방식이라 연출은 전적으로 에셋 쪽에서 정한다
/// (버튼이 트윈을 돌리지 않는다).
///
/// 자가 주입(HudInjectedBehaviour)이라 컨트롤러가 이 버튼을 참조/관리할 필요가 없다.
/// 모드의 단일 소스는 BattleDirector.BoostMode 이며, 이 버튼은 그 값을 구독해 표시만 맞춘다
/// (자원이 떨어져 루프가 모드를 내려도 버튼이 저절로 따라온다).
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class BoostModeButton : HudInjectedBehaviour {
    [SerializeField] private Button button;
    [Tooltip("선택. 모드 이름 표시(기본/부스트).")]
    [SerializeField] private TMP_Text label;
    [Tooltip("선택. 보유 부스트권 표시(N / 일일충전량). 버튼 아래에 배치한다.")]
    [SerializeField] private TMP_Text ticketText;
    [Tooltip("선택. 기본 모드에서만 켤 오브젝트(아이콘/배경 등).")]
    [SerializeField] private GameObject normalOnlyRoot;
    [Tooltip("선택. 부스트 모드에서만 켤 오브젝트.")]
    [SerializeField] private GameObject boostOnlyRoot;

    private CompositeDisposable _subs;
    private bool _wired;

    protected override void OnEnable() {
        base.OnEnable();   // 아직 주입 전이면 씬 스코프에서 Ctx 를 채운다.

        if (button == null) button = GetComponent<Button>();
        if (!_wired && button != null) {
            button.onClick.AddListener(OnClicked);
            _wired = true;
        }

        if (Ctx == null || Ctx.Battle == null) return;
        _subs?.Dispose();
        _subs = new CompositeDisposable();
        Ctx.Battle.BoostMode.Subscribe(Apply).AddTo(_subs);
        // 켤 수 있는지는 기력과 부스트권을 함께 봐야 정해진다(BoostRules.CanBoost). 둘 중 어느 쪽이 바뀌든
        // 같은 갱신을 태워야, 스냅샷이 순차로 도착할 때 나중에 온 값이 최종 판정을 내린다.
        Ctx.Store.StaminaBoostTicket.Subscribe(_ => RefreshResources()).AddTo(_subs);
        Ctx.Store.Stamina.Subscribe(_ => RefreshResources()).AddTo(_subs);

        Ctx.Battle.BoostExhausted -= OnBoostExhausted;
        Ctx.Battle.BoostExhausted += OnBoostExhausted;
    }

    private void OnDisable() {
        if (Ctx != null && Ctx.Battle != null) Ctx.Battle.BoostExhausted -= OnBoostExhausted;
        _subs?.Dispose();
        _subs = null;
    }

    private void OnClicked() {
        if (Ctx == null || Ctx.Battle == null) return;
        Ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        // 끄는 것은 언제나 가능하다(자원과 무관).
        if (Ctx.Battle.BoostMode.Value) { Ctx.Battle.SetBoostMode(false); return; }

        // 켜는 것은 자원이 있어야 한다. 버튼이 비활성이라 보통 여기까지 오지 않지만,
        // 누르는 순간 자원이 바뀌었을 수 있으므로 이유를 알려 주고 막는다.
        if (!BoostRules.CanBoost(Ctx.Store.Stamina.Value, Ctx.Store.StaminaBoostTicket.Value)) {
            Ctx.Ui.ShowToast(Ctx.Store.StaminaBoostTicket.Value < BoostRules.TicketPerBoostClear
                ? "기력 부스트권이 부족합니다."
                : $"기력이 {BoostRules.StaminaPerBoostClear} 이상이어야 합니다.");
            return;
        }

        Ctx.Battle.SetBoostMode(true);

        // 부스트 모드는 클라 안에서만 바뀌는 값이라 서버가 관측할 수 없다. 예전에는 부스트로 클리어한 순간을
        // 사용으로 봤지만, 그러면 버튼을 누르고도 클리어할 때까지 퀘스트가 아무 반응이 없다.
        // 켜는 데 성공한 이 지점에서 알린다(끄는 것은 알리지 않는다. 기능을 만난 시점이 아니다).
        Ctx.MainQuest?.ReportAction(MainQuestObjective.StaminaBoost);
    }

    private void OnBoostExhausted(BoostStopReason reason) {
        if (Ctx == null) return;
        Ctx.Ui.ShowToast(reason == BoostStopReason.TicketDepleted
            ? "기력 부스트권이 떨어져 기본 모드로 돌아갑니다."
            : $"기력이 {BoostRules.StaminaPerBoostClear} 아래로 내려가 기본 모드로 돌아갑니다.");
    }

    private void Apply(bool boost) {
        if (label != null) label.text = boost ? "부스트" : "기본";
        if (normalOnlyRoot != null) normalOnlyRoot.SetActive(!boost);
        if (boostOnlyRoot != null) boostOnlyRoot.SetActive(boost);

        RefreshResources();
    }

    // 부스트 자원(기력/부스트권)에 딸린 표시를 한 번에 맞춘다. 보유 표시와 버튼 활성이 같은 자원을 보므로
    // 둘을 갈라 두면 한쪽만 갱신되는 상태가 생긴다(스냅샷이 순차로 도착할 때 실제로 그랬다).
    private void RefreshResources() {
        if (Ctx == null) return;

        if (ticketText != null)
            ticketText.text = $"{Ctx.Store.StaminaBoostTicket.Value} / {Ctx.Store.StaminaBoostTicketDaily}";

        // 켤 수 없는 상태(자원 부족)에서는 버튼을 흐리게 둔다. 이미 켜져 있으면 끌 수 있어야 하므로 항상 활성이다.
        if (button != null && Ctx.Battle != null)
            button.interactable = Ctx.Battle.BoostMode.Value
                                  || BoostRules.CanBoost(Ctx.Store.Stamina.Value, Ctx.Store.StaminaBoostTicket.Value);
    }
}
