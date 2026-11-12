using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 닉네임 변경 팝업. 입력 + 중복확인 버튼 + 확인 버튼(닉네임 변경권 우선, 없으면 파견 증명서 500). 서버가 형식/중복/결제를 판정한다.
/// 성공 시 PlayerStateStore(닉네임/파견 증명서)를 갱신하고 닫는다. 계정 생성 시 변경권 1장이 무료 지급되어 있다.
/// </summary>
public sealed class NicknameChangePopup : UIView, IHudPopup {
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [Tooltip("중복확인 결과 텍스트.")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;
    [Tooltip("결제 수량 텍스트(변경권 1 또는 파견 증명서 500).")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("결제 재화 아이콘(변경권 또는 파견 증명서).")]
    [SerializeField] private Image costIcon;
    [SerializeField] private Button closeButton;

    [SerializeField] private Color availableColor = new Color(0.30f, 0.80f, 0.40f, 1f);
    [SerializeField] private Color unavailableColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    private MainHudContext _hud;
    private CancellationToken _ct;
    private int _ticketQty;
    private string _checkedName;      // 중복확인을 통과한 닉네임
    private bool _checkedAvailable;
    private bool _busy;

    protected override void Awake() {
        base.Awake();
        _ct = this.GetCancellationTokenOnDestroy();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (checkButton != null) checkButton.onClick.AddListener(() => Check());
        if (confirmButton != null) confirmButton.onClick.AddListener(() => Confirm());
        if (inputField != null) inputField.onValueChanged.AddListener(_ => OnInputChanged());
    }

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        try {
            var profile = await _hud.Profile.RequestProfileAsync(_ct);
            _ticketQty = profile.NicknameTicketQty;
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }
        if (inputField != null) {
            // 표시 폭 규칙(한글 2 / 영문 1)은 글자 수 제한으로 정확히 표현되지 않으므로, 입력 자체는 넉넉히 두고
            // 길이 판정은 NicknameRules 로 한다(길드 이름 입력과 같은 방식).
            inputField.characterLimit = NicknameRules.MaxWidth;
            inputField.text = "";
        }
        _checkedName = null;
        _checkedAvailable = false;
        if (resultText != null) resultText.text = "";
        RefreshCost();
        RefreshConfirm();
    }

    // 결제 표시: 변경권 보유 시 변경권 x1, 없으면 NicknameRules 의 대체 재화(파견 증명서 500).
    // 두 아이콘 모두 재화 카탈로그(CurrencyCatalog)에서 타입으로 조회한다 - 아이콘을 팝업마다 개별 할당하지 않아
    // 같은 재화를 쓰는 다른 UI 와 자동으로 일관된다(변경 시 카탈로그 한 곳만 수정).
    private void RefreshCost() {
        bool useTicket = _ticketQty > 0;
        if (costText != null) costText.text = useTicket ? "1" : NicknameRules.ChangeCost.ToString("N0");
        if (costIcon != null) {
            var sprite = _hud.Currencies != null
                ? _hud.Currencies.Icon(useTicket ? CurrencyType.NicknameTicket : NicknameRules.ChangeCurrency)
                : null;
            costIcon.sprite = sprite;
            costIcon.enabled = sprite != null;
        }
    }

    private void OnInputChanged() {
        // 입력이 바뀌면 이전 중복확인 결과 무효화.
        _checkedName = null;
        _checkedAvailable = false;
        if (resultText != null) resultText.text = "";
        RefreshConfirm();
    }

    private void Check() {
        if (_busy) return;
        CheckAsync().Forget();
    }

    private async UniTaskVoid CheckAsync() {
        string name = inputField != null ? inputField.text.Trim() : "";
        // 문자 종류/길이는 왕복 없이 여기서 걸러 준다. 중복과 금지어는 서버만 알 수 있어 물어봐야 한다.
        if (!NicknameRules.IsValidFormat(name)) {
            SetResult(NicknameRules.FormatMessage(name), false);
            return;
        }
        _busy = true;
        try {
            int fail = await _hud.Profile.CheckNicknameAsync(name, _ct);
            bool available = fail == 0;
            _checkedName = name;
            _checkedAvailable = available;
            SetResult(NicknameRules.FailMessage(fail), available);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        } finally {
            _busy = false;
            RefreshConfirm();
        }
    }

    private void SetResult(string msg, bool ok) {
        if (resultText == null) return;
        resultText.text = msg;
        resultText.color = ok ? availableColor : unavailableColor;
    }

    private void RefreshConfirm() {
        if (confirmButton == null) return;
        string name = inputField != null ? inputField.text.Trim() : "";
        // 중복확인을 통과한 동일 닉네임일 때만 확인 활성.
        confirmButton.interactable = _checkedAvailable && !string.IsNullOrEmpty(_checkedName) && _checkedName == name;
    }

    private void Confirm() {
        if (_busy) return;
        ConfirmAsync().Forget();
    }

    private async UniTaskVoid ConfirmAsync() {
        string name = inputField != null ? inputField.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) return;
        _busy = true;
        try {
            var res = await _hud.Profile.ChangeNicknameAsync(name, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            _hud.Store.Nickname.Value = res.Nickname;
            _hud.Store.DispatchCertificate.Value = res.NewCertificate;
            _ticketQty = res.NewTicketQty;
            _hud.Sound.PlaySfx(SfxKey.Purchase);
            _hud.Ui.ShowToast("닉네임이 변경되었습니다.");
            CloseAsync().Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("닉네임을 변경할 수 없습니다.");
        } finally {
            _busy = false;
        }
    }

    private static string FailMessage(int fail) => NicknameRules.FailMessage(fail);
}
