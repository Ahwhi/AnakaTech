using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 모집 마일리지 픽업 팝업. SSR 소녀를 늘어놓고 하나를 고르면 그 소녀의 id 를 돌려준다(취소면 0).
/// 실제 지급과 연출은 부르는 쪽(GachaMenuPopup)이 맡는다. 이 화면은 "무엇을 고를지"만 책임진다.
///
/// 로테이션과 무관하게 모든 SSR 소녀를 보여 준다. 픽업 차례를 기다리지 않고 받는 것이 이 장치의 목적이라
/// 지금 픽업 중인 소녀만 고를 수 있으면 의미가 없다.
///
/// 이미 보유한 소녀도 고를 수 있게 둔다. 중복이면 조각이 들어오는 기존 규칙이 그대로 적용되며,
/// 별레벨을 올리려고 일부러 고르는 선택도 유효하기 때문이다. 대신 카드가 보유 중임을 미리 알린다.
/// </summary>
public sealed class MileagePickPopup : UIView {
    [Header("Grid")]
    [Tooltip("카드가 생성될 부모(Grid Layout). 도감 그리드와 같은 구성으로 두면 된다.")]
    [SerializeField] private Transform gridContent;
    [Tooltip("카드 프리팹(MileagePickCard). 도감 카드 프리팹을 복제해 컴포넌트만 바꿔 쓰면 된다.")]
    [SerializeField] private MileagePickCard cardPrefab;

    [Header("Currency Panel (선택)")]
    [Tooltip("보유 재화 표시. 무엇을 보여 줄지는 자식으로 배치한 CurrencyView 가 정하므로 " +
             "마일리지 위젯을 넣으면 보유량이 아이콘과 함께 나온다. 아래 mileageText 와 역할이 다르다" +
             "(이쪽은 '얼마 있나', 저쪽은 '한 번 쓰기까지 얼마 남았나').")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Mileage")]
    [Tooltip("픽업까지의 진척 표시(예: 34,000 / 20,000). 보유량만 보여 줄 거라면 CurrencyPanel 로 충분하다. 선택.")]
    [SerializeField] private TMP_Text mileageText;
    [Tooltip("소모량 안내(선택). 픽업 1회에 드는 양을 그대로 보여 준다.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("마일리지가 모자랄 때 켜는 안내 묶음(예: '마일리지가 부족합니다'). 선택. " +
             "모자라도 이 창을 열 수 있으므로, 왜 확정할 수 없는지 알려 줄 자리가 필요하다.")]
    [SerializeField] private GameObject insufficientHint;
    [Tooltip("마일리지가 모자랄 때 위 진척 문구에 쓸 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Buttons")]
    [Tooltip("고른 소녀로 확정하는 버튼. 아무것도 고르지 않았거나 마일리지가 모자라면 꺼진다.")]
    [SerializeField] private Button confirmButton;
    [Tooltip("고른 소녀의 상세 정보를 여는 버튼(도감 상세와 같은 팝업). 고른 것이 없으면 꺼진다. 선택.")]
    [SerializeField] private Button detailButton;
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private UniTaskCompletionSource<int> _result;
    private readonly List<MileagePickCard> _cards = new();
    private CharacterData _selected;

    protected override void Awake() {
        base.Awake();
        if (confirmButton != null) confirmButton.onClick.AddListener(() => ConfirmAsync().Forget());
        if (detailButton != null) detailButton.onClick.AddListener(() => OpenDetailAsync().Forget());
        if (closeButton != null) closeButton.onClick.AddListener(() => Resolve(0));
    }

    /// <returns>고른 소녀의 CharDataId. 취소하거나 창이 닫히면 0.</returns>
    public async UniTask<int> ShowAsync(MainHudContext ctx, CancellationToken ct = default) {
        _ctx = ctx;
        _selected = null;

        // 재화 위젯은 스토어를 구독해 스스로 갱신하므로, 픽업으로 마일리지가 줄어도 따로 손댈 것이 없다.
        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        Build();
        RefreshMileage();
        RefreshConfirm();

        _result = new UniTaskCompletionSource<int>();
        await base.ShowAsync();
        return await _result.Task;
    }

    // 고를 수 있는 소녀를 그린다. 대상은 SSR 전체이며 정적 데이터만으로 정해지므로 통신이 필요 없다.
    private void Build() {
        foreach (var card in _cards) if (card != null) Destroy(card.gameObject);
        _cards.Clear();

        if (gridContent == null || cardPrefab == null || _ctx == null || _ctx.CharDb == null) return;

        foreach (var data in _ctx.CharDb.All) {
            if (data == null || data.Grade != GachaMileageRules.PickGrade) continue;

            var card = Instantiate(cardPrefab, gridContent);
            bool owned = _ctx.Store.Characters.ContainsKey(data.Id);
            card.Bind(data, owned, GameEnumRules.ShardsOnDuplicate(data.Grade), OnCardClicked);
            _cards.Add(card);
        }
    }

    private void OnCardClicked(MileagePickCard card) {
        if (card == null || card.Data == null) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        _selected = card.Data;
        foreach (var c in _cards) if (c != null) c.SetSelected(c == card);
        RefreshConfirm();
    }

    private void RefreshMileage() {
        int mileage = _ctx.Store.GachaMileage.Value;
        bool enough = GachaMileageRules.CanPick(mileage);

        if (mileageText != null) {
            mileageText.text = GachaMileageRules.Describe(mileage);
            // 평소 색은 인스펙터에서 정한 값을 그대로 쓴다(모자랄 때만 경고색으로 바꾼다).
            if (!_capturedMileageColor) { _mileageDefaultColor = mileageText.color; _capturedMileageColor = true; }
            mileageText.color = enough ? _mileageDefaultColor : insufficientColor;
        }
        if (costText != null) costText.text = $"{GachaMileageRules.PickCost:N0}";
        if (insufficientHint != null) insufficientHint.SetActive(!enough);
    }

    private Color _mileageDefaultColor = Color.white;
    private bool _capturedMileageColor;

    // 고른 것이 있고 마일리지가 충분할 때만 누를 수 있다. 여기까지 왔다면 보통 충분하지만,
    // 창을 열어 둔 사이에 다른 경로로 줄어들 수 있으므로 값으로 다시 확인한다.
    private void RefreshConfirm() {
        if (confirmButton != null)
            confirmButton.interactable = _selected != null
                                         && GachaMileageRules.CanPick(_ctx.Store.GachaMileage.Value);

        // 상세 보기는 마일리지와 무관하다. 모자라도 무엇을 받게 되는지는 볼 수 있어야 한다.
        if (detailButton != null) detailButton.interactable = _selected != null;
    }

    /// <summary>
    /// 고른 소녀의 상세 정보. 도감에서 여는 것과 같은 팝업이라 보여 주는 내용도 같다
    /// (여기서 따로 그리면 두 화면의 표시가 조용히 갈린다).
    /// </summary>
    private async UniTaskVoid OpenDetailAsync() {
        if (_selected == null) return;

        var reference = _ctx.Popups.CharacterDetailPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<CharacterDetailPopup>(reference);
            popup.Initialize(_ctx, _selected);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("캐릭터 정보를 불러오지 못했습니다.");
        }
    }

    private async UniTaskVoid ConfirmAsync() {
        if (_selected == null) return;

        // 이미 보유한 소녀는 조각으로 바뀌므로 한 번 확인받는다. 되돌릴 수 없는 소모라
        // "새 소녀를 받는 줄 알았는데 조각이 들어왔다"는 일이 없어야 한다.
        if (_ctx.Store.Characters.ContainsKey(_selected.Id)) {
            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup);
            bool ok = await confirm.ShowAsync("마일리지 픽업",
                $"'{_selected.DisplayName}' 은(는) 이미 보유 중입니다.\n" +
                $"중복으로 조각 {GameEnumRules.ShardsOnDuplicate(_selected.Grade)}개를 받습니다.\n정말 진행할까요?");
            if (!ok) return;
        }

        Resolve(_selected.Id);
    }

    private void Resolve(int charDataId) {
        if (charDataId != 0 && _ctx != null) _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _result?.TrySetResult(charDataId);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭, 일괄 닫힘)로 닫혀도 반드시 결과를 확정한다(취소=0).
    protected override void OnClosed() {
        _result?.TrySetResult(0);
    }
}
