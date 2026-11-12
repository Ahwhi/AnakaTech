using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 구매 복원 버튼(앱스토어 정책상 비소모성 상품이 있으면 필수). 아무 버튼에 붙이면 동작하며,
/// 패스 화면/상점/설정 어디에 두어도 같은 일을 한다.
///
/// 하는 일
///   1. 스토어에 지난 거래를 다시 묻는다. 아직 지급받지 못한 결제가 있으면 평소 미완료 결제 복구 흐름이 지급받는다.
///   2. 서버 기준 보유 상태(패스 등)와 배지를 다시 받아 화면을 맞춘다.
/// 보유 판정은 언제나 서버 구매 기록이 하므로, 이미 지급된 거래를 복원해도 두 번 지급되지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class IapRestoreButton : HudInjectedBehaviour {
    [SerializeField] private Button button;

    private bool _busy;

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        if (Ctx == null || _busy) return;
        Ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        RestoreAsync().Forget();
    }

    private async UniTaskVoid RestoreAsync() {
        var ct = this.GetCancellationTokenOnDestroy();
        if (Ctx.Iap == null || !Ctx.Iap.IsSupported) {
            Ctx.Ui.ShowToast("이 기기에서는 구매 복원을 지원하지 않습니다.");
            return;
        }

        _busy = true;
        try {
            bool restored;
            using (Ctx.Ui.ShowLoading("구매 내역을 확인하는 중입니다...")) {
                restored = await Ctx.Iap.RestoreAsync(ct);
                if (Ctx.Pass != null) await Ctx.Pass.RefreshAsync(ct);
                if (Ctx.Badges != null) Ctx.Badges.RefreshAsync(ct).Forget();
            }
            Ctx.Ui.ShowToast(restored ? "구매 내역을 복원했습니다." : "구매 내역을 복원하지 못했습니다.\n잠시 후 다시 시도해 주세요.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            Ctx.Ui.ShowToast("구매 내역을 복원하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }
}
