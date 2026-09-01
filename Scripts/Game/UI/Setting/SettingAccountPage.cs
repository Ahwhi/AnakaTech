using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 팝업의 계정 탭. 세 가지를 한 화면에서 처리한다.
///   1) 약관과 개인정보처리방침 상시 열람
///   2) 선택 동의(광고성 정보 앱 푸시 / 야간 광고성 정보 앱 푸시)의 동의와 철회
///   3) 회원 탈퇴
///
/// 가입 때 선택 항목을 거부했더라도 여기서 언제든 다시 동의할 수 있어야 하고, 반대로 언제든 철회할 수 있어야 한다.
/// 목록은 서버가 내려준 항목으로 만들어지므로, 약관이 늘거나 문구가 바뀌어도 이 스크립트는 고치지 않는다.
///
/// 다른 설정 페이지와 달리 서버 통신이 필요하므로 VContainer 로 서비스를 주입받는다.
/// </summary>
public sealed class SettingAccountPage : SettingPage {
    [Header("약관 및 정책")]
    [Tooltip("문서 열람 버튼이 생성될 부모. 서버가 내려준 상시 공개 문서 수만큼 만들어진다.")]
    [SerializeField] private Transform documentRoot;
    [Tooltip("문서 열람 버튼 프리팹. 자식에 TMP_Text 를 하나 두면 문서 이름이 채워진다.")]
    [SerializeField] private Button documentButtonPrefab;

    [Header("선택 동의")]
    [Tooltip("선택 동의 항목이 생성될 부모. 서버가 내려준 선택 항목 수만큼 만들어진다.")]
    [SerializeField] private Transform consentRoot;
    [Tooltip("선택 동의 항목 프리팹. 동의 화면과 같은 TermsItemView 를 재사용한다.")]
    [SerializeField] private TermsItemView consentItemPrefab;
    [Tooltip("선택 항목을 언제든 바꿀 수 있다는 안내 문구를 표시할 텍스트(선택).")]
    [SerializeField] private TMP_Text consentHintText;

    [Header("회원 탈퇴")]
    [SerializeField] private Button withdrawButton;

    [Header("상태")]
    [Tooltip("통신 중 조작을 막을 오버레이(선택). 비워 두면 버튼만 비활성화된다.")]
    [SerializeField] private GameObject busyBlocker;

    private readonly List<GameObject> _spawned = new();
    private bool _busy;

    private ITermsConsentFlow _terms => Ctx.Terms;
    private IAccountService _account => Ctx.Account;
    private IUIService _ui => Ctx.Ui;
    private UIPopupCatalog _popups => Ctx.Popups;
    private ILogoutService _logout => Ctx.Logout;

    protected override void OnInitialize() {
        if (consentHintText != null) consentHintText.text = TermsRules.OptionalConsentHint;

        if (withdrawButton != null) {
            withdrawButton.onClick.RemoveAllListeners();
            withdrawButton.onClick.AddListener(() => {
                PlayClick();
                WithdrawAsync().Forget();
            });
        }

        RefreshAsync().Forget();
    }

    // 서버가 내려준 목록으로 화면을 다시 만든다. 탭을 열 때마다 최신 상태를 보여 준다.
    private async UniTaskVoid RefreshAsync() {
        var ct = this.GetCancellationTokenOnDestroy();

        TermsStatusDto status;
        try {
            status = await _terms.GetStatusAsync(ct);
        } catch (System.OperationCanceledException) {
            return;
        } catch (System.Exception e) {
            Debug.LogWarning($"[Setting] 약관 상태 조회 실패: {e.Message}");
            return;
        }

        ClearSpawned();
        BuildDocuments(status);
        BuildConsents(status);
    }

    // 상시 공개 문서(이용약관/개인정보처리방침) 열람 버튼.
    private void BuildDocuments(TermsStatusDto status) {
        if (documentRoot == null || documentButtonPrefab == null) return;

        foreach (var doc in status.Documents) {
            // 주소가 없는 문서는 열 수 없으므로 버튼도 만들지 않는다(눌러도 아무 일이 없는 버튼을 두지 않는다).
            if (string.IsNullOrEmpty(doc.Url)) continue;

            var button = Instantiate(documentButtonPrefab, documentRoot);
            _spawned.Add(button.gameObject);

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = doc.Title;

            var captured = doc;
            button.onClick.AddListener(() => {
                PlayClick();
                _terms.ShowDocumentAsync(captured.Url, captured.Title).Forget();
            });
        }
    }

    // 선택 동의 항목. 전제 항목이 꺼져 있으면 딸린 항목은 잠근다(동의 화면과 같은 규칙).
    private void BuildConsents(TermsStatusDto status) {
        if (consentRoot == null || consentItemPrefab == null) return;

        var views = new List<(TermsItemView View, TermsItemDto Item)>();
        foreach (var item in status.OptionalItems) {
            var view = Instantiate(consentItemPrefab, consentRoot);
            _spawned.Add(view.gameObject);

            var captured = item;
            view.Bind(captured,
                changed => ChangeConsentAsync(captured, changed.IsAgreed).Forget(),
                data => _terms.ShowDocumentAsync(data.Url, data.Title).Forget());
            views.Add((view, captured));
        }

        ApplyPrerequisites(views, status);
    }

    // 전제 항목이 동의 상태가 아니면 딸린 항목을 잠근 채로 보여 준다.
    private static void ApplyPrerequisites(List<(TermsItemView View, TermsItemDto Item)> views, TermsStatusDto status) {
        foreach (var (view, item) in views) {
            if (!item.HasPrerequisite) continue;
            bool unlocked = status.Find(item.PrerequisiteKey)?.IsAgreed ?? false;
            view.SetSelectable(unlocked);
        }
    }

    private async UniTaskVoid ChangeConsentAsync(TermsItemDto item, bool agreed) {
        if (_busy) return;
        SetBusy(true);
        try {
            await _terms.SetOptionalConsentAsync(item.Key, item.Version, agreed,
                                                 this.GetCancellationTokenOnDestroy());
        } catch (System.OperationCanceledException) {
            return;
        } finally {
            SetBusy(false);
        }

        // 전제/연쇄 해제 결과는 서버가 정한다. 화면을 다시 그려 저장된 상태를 그대로 반영한다.
        RefreshAsync().Forget();
    }

    private async UniTaskVoid WithdrawAsync() {
        if (_busy) return;

        var confirm = await _ui.ShowPopupAsync<ConfirmCancelPopup>(_popups.ConfirmCancelPopup);
        if (!await confirm.ShowAsync(TermsRules.WithdrawTitle, TermsRules.WithdrawConfirm)) return;

        SetBusy(true);
        WithdrawResultDto res;
        try {
            res = await _account.WithdrawAsync(this.GetCancellationTokenOnDestroy());
        } catch (System.OperationCanceledException) {
            return;
        } catch (System.Exception e) {
            Debug.LogWarning($"[Setting] 탈퇴 요청 실패: {e.Message}");
            _ui.ShowToast(TermsRules.WithdrawFailMessage(0), 3f);
            return;
        } finally {
            SetBusy(false);
        }

        if (!res.IsSuccess) {
            _ui.ShowToast(TermsRules.WithdrawFailMessage(res.FailReason), 3f);
            return;
        }

        // 계정이 사라졌으므로 이 기기의 자동 로그인 토큰도 의미가 없다.
        // 서버가 곧 연결을 끊으므로, 로그아웃과 같은 경로로 타이틀까지 되돌린다.
        SessionTokenStore.Clear();
        _ui.ShowToast(TermsRules.WithdrawDone, 3f);
        await _logout.LogoutAsync();
    }

    private void SetBusy(bool on) {
        _busy = on;
        if (busyBlocker != null) busyBlocker.SetActive(on);
        if (withdrawButton != null) withdrawButton.interactable = !on;
    }

    private void ClearSpawned() {
        foreach (var go in _spawned)
            if (go != null) Destroy(go);
        _spawned.Clear();
    }
}
