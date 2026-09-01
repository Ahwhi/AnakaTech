using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 쿠폰 팝업. 번호를 입력하면 서버가 판정해 보상을 우편으로 보낸다.
///
/// 받은 목록은 이 화면이 아니라 공용 결과 팝업(PurchaseCompletePopup)이 보여 준다. 상점 구매 결과와
/// 똑같이 "무엇을 받았는지"를 보여 주는 화면이라, 여기에 같은 표시를 한 벌 더 만들면 두 화면이 서로 다르게
/// 자라난다. 이 화면은 입력과 실패 안내까지만 맡는다.
///
/// 보상을 즉시 지급하지 않고 우편으로 보내는 것은 서버의 선택이며, 그 덕에 가방이 꽉 찼거나 이미 보유한
/// 캐릭터가 나와도 기존 우편 수령 경로가 그대로 처리한다. 그래서 이 화면은 재화를 직접 건드리지 않는다.
///
/// 입력은 표준형(대문자 영숫자)으로 다듬어 보낸다. 유저가 하이픈이나 소문자를 섞어 적어도 같은 코드로 통한다.
/// </summary>
public sealed class CouponPopup : UIView, IHudPopup {
    [Header("Input")]
    [Tooltip("쿠폰 번호 입력칸. 입력하는 동안 표준형(대문자 영숫자)으로 자동 정리된다.")]
    [SerializeField] private TMP_InputField codeInput;
    [Tooltip("입력칸 안내 문구(Placeholder). 비우면 프리팹 문구를 그대로 쓴다.")]
    [SerializeField] private TMP_Text placeholderText;
    [Tooltip("사용 버튼. 코드 길이가 맞을 때만 켜진다.")]
    [SerializeField] private Button redeemButton;

    [Tooltip("서버 응답을 기다리는 동안 켜는 그룹(스피너 등). 그동안 사용 버튼은 잠긴다.")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Message")]
    [Tooltip("실패 안내 문구. 성공 결과는 공용 결과 팝업이 보여 주므로 여기서는 실패만 알린다.")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Color failColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private bool _busy;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (placeholderText != null && string.IsNullOrEmpty(placeholderText.text))
            placeholderText.text = CouponRules.InputPlaceholder;

        if (codeInput != null) {
            codeInput.characterLimit = CouponRules.MaxCodeLength;
            codeInput.text = "";
            // 입력하는 동안 표준형으로 정리한다. 보내는 값과 보이는 값이 같아야 결과를 예측할 수 있다.
            codeInput.onValueChanged.AddListener(OnCodeChanged);
            // 키보드에서 확인을 눌러도 바로 사용되게 한다(입력칸 하나뿐인 화면이라 버튼까지 옮겨 갈 이유가 없다).
            codeInput.onSubmit.AddListener(_ => Redeem());
        }

        if (redeemButton != null) redeemButton.onClick.AddListener(Redeem);
        if (closeButton != null)
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

        ClearResult();
        SetBusy(false);
    }

    private void OnCodeChanged(string raw) {
        if (codeInput == null) return;

        string normalized = CouponRules.Normalize(raw);
        if (normalized != raw) {
            // 커서가 맨 뒤로 튀지 않도록 통지 없이 바꾼다(한 글자 칠 때마다 앞으로 돌아가면 입력이 불가능하다).
            codeInput.SetTextWithoutNotify(normalized);
            codeInput.caretPosition = normalized.Length;
        }
        RefreshButton();
    }

    private void RefreshButton() {
        if (redeemButton == null) return;
        string code = codeInput != null ? codeInput.text : "";
        redeemButton.interactable = !_busy && CouponRules.IsValidCode(code);
    }

    private void Redeem() {
        if (_busy) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        RedeemAsync().Forget();
    }

    private async UniTaskVoid RedeemAsync() {
        string code = codeInput != null ? CouponRules.Normalize(codeInput.text) : "";
        if (!CouponRules.IsValidCode(code)) { ShowFail(CouponRules.FailMessage(2)); return; }

        SetBusy(true);
        ClearResult();

        try {
            var res = await _ctx.Coupon.RedeemAsync(code, _ct);
            if (!res.IsSuccess) {
                ShowFail(CouponRules.FailMessage(res.FailReason));
                return;
            }

            _ctx.Sound.PlaySfx(SfxKey.Claim);
            if (codeInput != null) codeInput.SetTextWithoutNotify("");

            // 보상이 우편으로 갔으므로 우편 배지가 그 자리에서 켜지도록 미수령 수를 다시 받아온다.
            if (_ctx.Mail != null) _ctx.Mail.RefreshAsync(_ct).Forget();

            // 결과 팝업이 닫힐 때까지 기다린다. 그 사이 사용 버튼이 열려 있으면 같은 코드를 다시 쳐 볼 수 있으므로
            // busy 를 유지한 채로 기다린다.
            await ShowRewardsAsync(code, res.Granted);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ShowFail(CouponRules.FailMessage(-1));
        } finally {
            SetBusy(false);
        }
    }

    // 받은 목록은 상점 구매와 같은 공용 결과 팝업이 보여 준다.
    // 팝업 프리팹을 아직 안 붙였거나 받을 것이 없으면 토스트로 대신 알린다(결과를 아무것도 못 보는 상황은 없어야 한다).
    private async UniTask ShowRewardsAsync(string code, List<RewardItem> granted) {
        var reference = _ctx.Popups.PurchaseCompletePopup;
        if (granted == null || granted.Count == 0 || reference == null || !reference.RuntimeKeyIsValid()) {
            _ctx.Ui.ShowToast(CouponRules.SuccessMessage);
            return;
        }

        var popup = await _ctx.Ui.ShowPopupAsync<PurchaseCompletePopup>(reference, _ct);
        await popup.ShowAsync(_ctx, CouponRules.RewardTitle, code, null, CouponRules.SuccessMessage, granted);
    }

    // 통신 중에는 로딩 그룹을 켜고 사용 버튼을 잠근다. 응답이 늦을 때 유저가 연타해서
    // 같은 코드를 여러 번 보내는 일을 막는다(서버가 중복 사용을 막긴 하지만 실패 안내가 뜨는 것은 부자연스럽다).
    private void SetBusy(bool busy) {
        _busy = busy;
        if (loadingGroup != null) loadingGroup.SetActive(busy);
        RefreshButton();
    }

    private void ShowFail(string message) {
        if (messageText == null) return;
        messageText.gameObject.SetActive(true);
        messageText.text = message;
        messageText.color = failColor;
    }

    private void ClearResult() {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    protected override void OnClosed() {
        ClearResult();
        SetBusy(false);
    }
}
