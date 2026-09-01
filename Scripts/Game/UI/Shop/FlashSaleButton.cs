using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 메인 HUD 의 한정 판매 버튼. 지금 이 계정에 열려 있는 한정 판매가 있을 때만 나타나고,
/// 배지에 그 개수를 숫자로 보여 준다(빨간 점/NEW 가 아니라 "몇 개가 돌고 있는지"가 정보이기 때문).
///
/// HudMenuButton 과 달리 자기 표시 여부를 스스로 정한다. 한정 판매는 평소에 없는 것이 정상이라
/// 항상 자리를 차지하고 있으면 "눌러도 아무것도 없는 버튼"이 되기 때문이다.
///
/// 버튼을 누르면 안내 팝업만 연다. 결제는 상점에서만 이뤄지며(팝업이 상점으로 보낸다),
/// 그래야 결제 경로가 한 곳으로 유지된다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class FlashSaleButton : HudInjectedBehaviour {
    [SerializeField] private Button button;
    [Tooltip("열려 있는 한정 판매가 없을 때 끌 대상. 반드시 이 오브젝트의 자식을 지정한다(자기 자신을 넣으면 안 된다). " +
             "비우면 CanvasGroup 으로 투명하게 숨긴다. 레이아웃 그룹 안에 있어 자리까지 접혀야 한다면 자식을 지정할 것.")]
    [SerializeField] private GameObject visualRoot;
    [Tooltip("열려 있는 개수를 숫자로 보여 줄 배지(BadgeView, Source=Manual). 선택.")]
    [SerializeField] private BadgeView countBadge;
    [Tooltip("이 버튼이 열 한정 판매 안내 팝업 프리팹(FlashSalePopup).")]
    [SerializeField] private AssetReferenceGameObject flashSalePopup;

    private CompositeDisposable _subs;
    private CanvasGroup _group;

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
        // 목록을 받기 전에는 숨겨 둔다(잠깐 떴다 사라지는 깜빡임 방지).
        SetVisible(false);
    }

    // HudInjectedBehaviour 가 의존성을 넣어 준 뒤 호출된다.
    protected override void OnInjected() {
        _subs?.Dispose();
        _subs = new CompositeDisposable();

        // "열려 있는 수"가 아니라 "지금 살 수 있는 수"를 센다. 이미 산 상품까지 세면 다 사고도 배지가 남는다.
        Ctx.FlashSale.PurchasableCount
           .Subscribe(ApplyCount)
           .AddTo(_subs);
    }

    private void ApplyCount(int count) {
        SetVisible(count > 0);
        if (countBadge != null) countBadge.SetCount(count);
    }

    /// <summary>
    /// 버튼을 보이거나 숨긴다.
    ///
    /// 자기 자신을 SetActive(false) 로 끄면 안 된다. 꺼진 오브젝트는 OnEnable 이 다시 오지 않아
    /// 목록 구독을 걸 기회 자체가 사라지고, 한정 판매가 열려도 영영 나타나지 않는다.
    /// 그래서 숨길 자식(visualRoot)을 지정했으면 그 자식만 끄고, 지정하지 않았으면
    /// CanvasGroup 으로 투명하게 만들어 자신은 살려 둔다.
    /// </summary>
    private void SetVisible(bool visible) {
        if (visualRoot != null && visualRoot != gameObject) {
            visualRoot.SetActive(visible);
            return;
        }

        if (_group == null) {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }
        _group.alpha = visible ? 1f : 0f;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }

    private void Open() {
        if (Ctx == null || flashSalePopup == null || !flashSalePopup.RuntimeKeyIsValid()) return;
        Ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenAsync(CancellationToken ct) {
        var popup = await Ctx.Ui.ShowPopupAsync<FlashSalePopup>(flashSalePopup, ct);
        popup.Initialize(Ctx);
        await popup.ShowAsync();
    }

    private void OnDestroy() => _subs?.Dispose();
}
