using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가입 시 받는 약관 동의 팝업. 서버가 내려준 항목을 그대로 목록으로 그린다.
/// 시작 경로는 두 가지이며 둘 다 같은 저장 경로를 탄다.
///   전체 동의 버튼 : 선택 항목까지 모두 동의로 채우고 바로 시작한다(체크를 하나도 누르지 않아도 된다).
///   시작 버튼      : 화면에 체크된 그대로 시작한다. 필수 항목이 모두 체크됐을 때만 열린다.
/// 항목의 상세 내용은 이 화면이 아니라 전문 팝업(TermsDetailPopup)이 보여 준다.
///
/// 저장은 이 화면이 직접 하지 않고 주입받은 제출 함수(프레젠터)가 한다. 그래야 뷰는 통신을 모른 채
/// 대기 표시와 실패 안내만 책임지고, 실패해도 유저가 체크한 상태를 잃지 않는다.
///
/// 항목 간 관계는 서버가 내려준 선행 조건(PrerequisiteKey)만 따른다. 예를 들어 야간 푸시는 마케팅 수신
/// 동의가 켜져 있을 때만 선택할 수 있고, 마케팅 수신을 끄면 함께 꺼진다.
/// </summary>
public sealed class TermsAgreementPopup : UIView {
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Agree All")]
    [Tooltip("전체 동의 버튼. 선택 항목까지 모두 동의한 것으로 보고 그대로 저장하고 시작한다.")]
    [SerializeField] private Button agreeAllButton;
    [SerializeField] private TextMeshProUGUI agreeAllLabel;

    [Header("List")]
    [Tooltip("ScrollRect 의 Content. 항목이 이 아래에 생성된다.")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private TermsItemView itemPrefab;

    [Header("Documents")]
    [Tooltip("개인정보처리방침 열람 버튼(선택). 동의 항목이 아니라 고지 문서이므로 체크박스와 분리해 링크로 노출한다. " +
             "서버가 문서를 내려주지 않으면 자동으로 감춰진다.")]
    [SerializeField] private Button privacyPolicyButton;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmLabel;
    [Tooltip("취소(뒤로가기) 버튼. 누르면 게임을 종료할지 다시 묻는다.")]
    [SerializeField] private Button cancelButton;

    [Header("State")]
    [Tooltip("필수 미동의/저장 실패 안내 문구.")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Color failColor = new Color(0.90f, 0.20f, 0.20f, 1f);
    [Tooltip("저장을 기다리는 동안 켜는 그룹(스피너 등).")]
    [SerializeField] private GameObject loadingGroup;

    private readonly List<TermsItemView> _items = new();

    private Action<string, string> _openDocument;
    private Func<IReadOnlyList<TermsConsent>, UniTask<TermsSubmitResultDto>> _submit;
    private UniTaskCompletionSource<bool> _result;
    private bool _busy;

    protected override void Awake() {
        base.Awake();
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        if (agreeAllButton != null) agreeAllButton.onClick.AddListener(AgreeAll);

        if (titleText != null) titleText.text = TermsRules.PopupTitle;
        if (descriptionText != null) descriptionText.text = TermsRules.PopupDescription;
        if (agreeAllLabel != null) agreeAllLabel.text = TermsRules.AgreeAllLabel;
        if (confirmLabel != null) confirmLabel.text = TermsRules.ConfirmLabel;
    }

    /// <summary>
    /// 문서 열기(주소, 제목)와 동의 저장을 프레젠터가 주입한다(뷰는 통신/웹뷰를 모른다).
    /// </summary>
    public void Initialize(Action<string, string> openDocument,
                           Func<IReadOnlyList<TermsConsent>, UniTask<TermsSubmitResultDto>> submit) {
        _openDocument = openDocument;
        _submit = submit;
    }

    /// <summary>
    /// 항목을 그리고 페이드 인한 뒤, 사용자가 동의를 마칠 때까지 기다린다.
    /// true = 서버 저장까지 성공, false = 취소(뒤로가기)나 강제 닫힘.
    /// </summary>
    public async UniTask<bool> ShowAsync(TermsStatusDto status) {
        BuildItems(status?.Items);
        BuildPrivacyPolicyLink(status);
        ClearMessage();
        SetBusy(false);

        _result = new UniTaskCompletionSource<bool>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void BuildItems(IReadOnlyList<TermsItemDto> items) {
        ClearItems();
        if (items == null || itemPrefab == null || contentRoot == null) return;

        foreach (var data in items) {
            var view = Instantiate(itemPrefab, contentRoot);
            view.Bind(data, OnItemChanged,
                      item => _openDocument?.Invoke(item.Url, TermsRules.DisplayTitle(item)));
            _items.Add(view);
        }

        RefreshPrerequisites();
        RefreshConfirm();
    }

    // 개인정보처리방침은 동의를 받는 문서가 아니라 고지 문서다. 체크박스로 만들면 "동의해야 가입되는 것"으로
    // 오인되므로, 가입 화면에서도 열람만 할 수 있는 링크로 둔다.
    private void BuildPrivacyPolicyLink(TermsStatusDto status) {
        if (privacyPolicyButton == null) return;

        var doc = status?.FindDocument(TermsRules.PrivacyPolicyKey);
        bool available = doc != null && !string.IsNullOrEmpty(doc.Url);
        privacyPolicyButton.gameObject.SetActive(available);
        if (!available) return;

        privacyPolicyButton.onClick.RemoveAllListeners();
        privacyPolicyButton.onClick.AddListener(() => _openDocument?.Invoke(doc.Url, doc.Title));
    }

    // ── 입력 ────────────────────────────────────────────────
    private void OnItemChanged(TermsItemView changed) {
        ClearMessage();
        RefreshPrerequisites();
        RefreshConfirm();
    }

    // 선행 동의가 꺼진 항목은 함께 끄고 잠근다(야간 푸시 <- 마케팅 수신).
    private void RefreshPrerequisites() {
        foreach (var item in _items) {
            var data = item.Data;
            if (data == null || !data.HasPrerequisite) continue;

            bool allowed = IsAgreed(data.PrerequisiteKey);
            if (!allowed && item.IsAgreed) item.SetAgreedWithoutNotify(false);
            item.SetSelectable(allowed);
        }
    }

    private void RefreshConfirm() {
        if (confirmButton != null) confirmButton.interactable = !_busy && AllRequiredAgreed();
    }

    private bool AllRequiredAgreed() {
        foreach (var item in _items)
            if (item.Data != null && item.Data.IsRequired && !item.IsAgreed) return false;
        return true;
    }

    private bool IsAgreed(string key) {
        foreach (var item in _items)
            if (item.Key == key) return item.IsAgreed;
        return false;
    }

    // ── 확정 ────────────────────────────────────────────────
    // 전체 동의: 선택 항목까지 모두 켠 뒤 바로 저장한다. 체크를 하나씩 누를 필요 없이 이 버튼 하나로 끝난다.
    private void AgreeAll() {
        if (_busy) return;

        ClearMessage();
        foreach (var item in _items) item.SetAgreedWithoutNotify(true);
        // 선행 조건이 있는 항목도 전제 항목이 함께 켜졌으므로 잠금이 풀린 상태로 갱신된다.
        RefreshPrerequisites();
        RefreshConfirm();

        SubmitAsync().Forget();
    }

    // 개별 선택 후 시작: 화면에 체크된 그대로 저장한다.
    private void Confirm() {
        if (_busy) return;
        if (!AllRequiredAgreed()) { ShowMessage(TermsRules.RequiredHint); return; }
        SubmitAsync().Forget();
    }

    private async UniTaskVoid SubmitAsync() {
        if (_submit == null) { ShowMessage(TermsRules.FailMessage(-1)); return; }

        SetBusy(true);
        ClearMessage();

        var consents = new List<TermsConsent>(_items.Count);
        foreach (var item in _items) {
            var data = item.Data;
            if (data != null) consents.Add(new TermsConsent(data.Key, data.Version, item.IsAgreed));
        }

        try {
            var res = await _submit(consents);
            if (!res.IsSuccess || res.NeedAgreement) {
                ShowMessage(TermsRules.FailMessage(res.FailReason));
                return;
            }

            _result?.TrySetResult(true);
            await CloseAsync();
        } catch (OperationCanceledException) {
            // 부트 취소/연결 끊김. 팝업은 상위(연결 가드/부트)가 정리한다.
        } catch (Exception e) {
            Debug.LogException(e);
            ShowMessage(TermsRules.FailMessage(-1));
        } finally {
            SetBusy(false);
        }
    }

    private void Cancel() {
        if (_busy) return;
        _result?.TrySetResult(false);
        CloseAsync().Forget();
    }

    // 배경 탭이나 외부 CloseAllPopups 로 닫혀도 대기 중인 호출부가 멈추지 않도록 취소로 확정한다.
    protected override void OnClosed() {
        _result?.TrySetResult(false);
        ClearItems();
    }

    // ── 표시 ────────────────────────────────────────────────
    private void SetBusy(bool busy) {
        _busy = busy;
        if (loadingGroup != null) loadingGroup.SetActive(busy);
        if (cancelButton != null) cancelButton.interactable = !busy;
        // 전체 동의는 필수 체크 여부와 무관하게 항상 누를 수 있다(저장 중에만 잠근다).
        if (agreeAllButton != null) agreeAllButton.interactable = !busy;
        RefreshConfirm();
    }

    private void ShowMessage(string message) {
        if (messageText == null) return;
        messageText.gameObject.SetActive(true);
        messageText.text = message;
        messageText.color = failColor;
    }

    private void ClearMessage() {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    private void ClearItems() {
        foreach (var item in _items)
            if (item != null) Destroy(item.gameObject);
        _items.Clear();
    }
}
