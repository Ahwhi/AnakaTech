using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 메인 HUD 의 재사용 메뉴 버튼. 아무 버튼 오브젝트에 붙이고 "열 팝업 프리팹(AssetReference)"만 지정하면,
/// 클릭 시 그 팝업을 띄운다. 팝업이 IHudPopup 이면 MainHudContext 로 자동 초기화한다(상점/도감/덱/설정 등 공통).
/// 자가 주입(HudInjectedBehaviour)이라 컨트롤러가 이 버튼을 참조/관리할 필요가 없고, 씬에서 비활성으로 두어도
/// 활성화되는 순간 동작한다. 새 기능 추가 = 버튼 오브젝트 + 팝업 프리팹만 있으면 되고 코드 수정이 없다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class HudMenuButton : HudInjectedBehaviour {
    [SerializeField] private Button button;
    [Tooltip("이 버튼이 열 팝업 프리팹. IHudPopup 을 구현하면 MainHudContext 로 자동 초기화된다.")]
    [SerializeField] private AssetReferenceGameObject targetPopup;
    [Tooltip("어느 Canvas 레이어에 띄울지. 보통 Popup.")]
    [SerializeField] private UIPopupLayer layer = UIPopupLayer.Popup;

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
    }

    private void Open() {
        if (Ctx == null || targetPopup == null || !targetPopup.RuntimeKeyIsValid()) return;
        Ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenAsync(CancellationToken ct) {
        var view = await Ctx.Ui.ShowPopupAsync<UIView>(targetPopup, ct, layer);
        if (view is IHudPopup hudPopup) hudPopup.Initialize(Ctx);
        await view.ShowAsync();
    }
}
