using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 길드 생성 팝업. 이름(중복확인 필수) + 가입 방식을 정하고 다이아로 생성한다.
/// 생성했으면 true, 취소하면 false 를 await 로 돌려준다.
///   bool created = await popup.ShowAsync(hud, myCombatPower);
///
/// 중복확인을 통과한 뒤 이름을 고치면 확인 상태를 다시 풀어(같은 규칙: 닉네임 변경 팝업) 확인 없이 생성되지 않게 한다.
/// </summary>
public sealed class GuildCreatePopup : UIView {
    [Header("Name")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button checkButton;
    [Tooltip("중복확인 결과/길이 안내 문구.")]
    [SerializeField] private TMP_Text nameHintText;
    [SerializeField] private Color okColor = new Color(0.30f, 0.80f, 0.40f, 1f);
    [SerializeField] private Color errorColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Join Type")]
    [Tooltip("가입 방식 라디오 버튼(즉시 가입, 가입 신청 순).")]
    [SerializeField] private Toggle[] joinTypeToggles;

    [Header("Cost")]
    [Tooltip("생성 비용 표시(다이아).")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("비용 재화 아이콘. 스프라이트는 재화 카탈로그(CurrencyCatalog)에서 타입으로 가져오므로 직접 할당하지 않는다.")]
    [SerializeField] private Image costIcon;
    [Tooltip("다이아가 부족할 때 비용 텍스트에 적용할 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Buttons")]
    [SerializeField] private Button createButton;
    [SerializeField] private Button cancelButton;

    private static readonly GuildJoinType[] JoinTypes = { GuildJoinType.Instant, GuildJoinType.Application };

    private MainHudContext _ctx;
    private long _combatPower;
    private UniTaskCompletionSource<bool> _result;
    private bool _nameChecked;
    private string _checkedName = "";
    private bool _busy;
    // 비용 텍스트의 평상시 색. 프리팹에서 정한 색을 그대로 쓰려고 첫 표시 때 한 번만 기억한다.
    private Color _costDefaultColor = Color.white;
    private bool _costColorCaptured;
    private bool _affordable = true;

    protected override void Awake() {
        base.Awake();
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(false));
        if (createButton != null) createButton.onClick.AddListener(() => CreateAsync().Forget());
        if (checkButton != null) checkButton.onClick.AddListener(() => CheckAsync().Forget());
        if (nameInput != null) nameInput.onValueChanged.AddListener(OnNameChanged);
    }

    public async UniTask<bool> ShowAsync(MainHudContext ctx, long combatPower) {
        _ctx = ctx;
        _combatPower = combatPower;
        _nameChecked = false;
        _checkedName = "";

        if (nameInput != null) {
            // 표시 폭 규칙(한글 2 / 영문 1)은 글자 수 제한으로 정확히 표현되지 않으므로, 입력 자체는 넉넉히 두고
            // 길이 판정은 GuildRules 로 한다(안내 문구가 실시간으로 알려 준다).
            nameInput.characterLimit = GuildRules.NameMaxWidth;
            nameInput.SetTextWithoutNotify("");
        }
        CurrencyIcon.Apply(costIcon, ctx.Currencies, CurrencyType.Diamond);
        RefreshCost();
        SetupJoinTypes();
        RefreshHint();

        _result = new UniTaskCompletionSource<bool>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void SetupJoinTypes() {
        if (joinTypeToggles == null) return;
        for (int i = 0; i < joinTypeToggles.Length; i++) {
            if (joinTypeToggles[i] == null) continue;
            joinTypeToggles[i].SetIsOnWithoutNotify(i == 0);   // 기본은 즉시 가입

            int index = i;
            joinTypeToggles[i].onValueChanged.RemoveAllListeners();
            joinTypeToggles[i].onValueChanged.AddListener(on => {
                if (!on) return;
                for (int k = 0; k < joinTypeToggles.Length; k++)
                    if (k != index && joinTypeToggles[k] != null) joinTypeToggles[k].SetIsOnWithoutNotify(false);
            });
        }
    }

    private GuildJoinType SelectedJoinType() {
        if (joinTypeToggles != null)
            for (int i = 0; i < joinTypeToggles.Length && i < JoinTypes.Length; i++)
                if (joinTypeToggles[i] != null && joinTypeToggles[i].isOn) return JoinTypes[i];
        return GuildJoinType.Instant;
    }

    // 이름을 고치면 중복확인을 다시 받아야 한다(확인한 이름과 달라지는 순간 풀린다).
    private void OnNameChanged(string value) {
        if (_nameChecked && value != _checkedName) _nameChecked = false;
        RefreshHint();
    }

    private async UniTaskVoid CheckAsync() {
        if (_busy || nameInput == null) return;
        string name = nameInput.text.Trim();
        // 문자 종류/길이는 왕복 없이 여기서 걸러 준다. 중복과 금지어는 서버만 알 수 있어 물어봐야 한다.
        if (!GuildRules.IsValidName(name)) { ShowHint(GuildRules.NameFormatMessage(name), false); return; }

        _busy = true;
        try {
            _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
            int fail = await _ctx.Guild.CheckNameAsync(name, this.GetCancellationTokenOnDestroy());
            bool available = fail == 0;
            _nameChecked = available;
            _checkedName = available ? name : "";
            ShowHint(available ? "사용할 수 있는 이름입니다." : GuildRules.FailMessage(fail), available);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ShowHint("확인에 실패했습니다.", false);
        } finally {
            _busy = false;
            RefreshCreateButton();
        }
    }

    private async UniTaskVoid CreateAsync() {
        if (_busy || !_nameChecked) return;
        _busy = true;
        try {
            _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
            var res = await _ctx.Guild.CreateAsync(_checkedName, SelectedJoinType(), _combatPower,
                                                   this.GetCancellationTokenOnDestroy());
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(GuildRules.FailMessage(res.FailReason));
                _nameChecked = false;   // 이름이 선점됐을 수 있으므로 다시 확인하게 한다
                RefreshCreateButton();
                return;
            }
            if (res.NewDiamond > 0) _ctx.Store.Diamond.Value = res.NewDiamond;
            Resolve(true);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("길드 생성에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 입력 중에는 길이 규칙만 알려 주고, 중복확인 결과가 있으면 그것을 우선 보여 준다.
    private void RefreshHint() {
        if (_nameChecked) { ShowHint("사용할 수 있는 이름입니다.", true); return; }

        string name = nameInput != null ? nameInput.text.Trim() : "";
        if (name.Length == 0) ShowHint(GuildRules.NameHint, true);
        else if (!GuildRules.IsValidName(name)) ShowHint($"사용할 수 없는 이름입니다. ({GuildRules.NameHint})", false);
        else ShowHint("중복확인이 필요합니다.", false);
        RefreshCreateButton();
    }

    private void ShowHint(string message, bool ok) {
        if (nameHintText == null) return;
        nameHintText.text = message;
        nameHintText.color = ok ? okColor : errorColor;
    }

    // 다이아가 모자라면 비용을 빨갛게 물들이고 생성 버튼을 잠근다(서버도 같은 규칙으로 막지만,
    // 누를 수 없는 버튼을 보여 주지 않는 편이 낫다). 팝업이 떠 있는 동안 보유량이 바뀔 일은 없으므로 1회 계산한다.
    private void RefreshCost() {
        _affordable = _ctx.Store.Diamond.Value >= GuildRules.CreateCostDiamond;
        if (costText == null) return;

        if (!_costColorCaptured) { _costDefaultColor = costText.color; _costColorCaptured = true; }
        costText.text = GuildRules.CreateCostDiamond.ToString("N0");
        costText.color = _affordable ? _costDefaultColor : insufficientColor;
    }

    private void RefreshCreateButton() {
        if (createButton != null) createButton.interactable = _nameChecked && !_busy && _affordable;
    }

    private void Resolve(bool created) {
        _result?.TrySetResult(created);
        CloseAsync().Forget();
    }

    // 배경 탭/외부 닫힘으로도 반드시 결과를 확정해 호출부가 멈춘 채로 남지 않게 한다.
    protected override void OnClosed() => _result?.TrySetResult(false);
}
