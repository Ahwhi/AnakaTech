using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 재화 설명 팝업. 재화 아이콘을 누르면 이름 + 설명 + 현재 보유량을 보여준다.
/// 표시 내용은 전부 CurrencyCatalog 에서 오므로, 새 재화를 추가할 때 이 스크립트는 손대지 않는다
/// (카탈로그 에셋에 항목만 채우면 된다).
///
/// 값을 구독하지 않고 열린 시점의 보유량만 찍는다. 팝업이 떠 있는 짧은 동안의 변동까지 따라갈 이유가 없고,
/// 구독을 두면 순수 표시 팝업이 스토어 수명주기에 묶이기 때문이다.
///
/// 다이아만 유상/무상 구분이 있다. 그 구분은 결제 이력에 관한 값이라 화면이 계산할 수 없고 서버가 권위이므로,
/// 다이아를 열었을 때만 한 번 조회해 채운다. 관련 필드를 배선하지 않으면 그냥 표시하지 않는다.
/// </summary>
public sealed class CurrencyInfoPopup : UIView, IHudPopup {
    [Tooltip("재화 아이콘.")]
    [SerializeField] private Image iconImage;
    [Tooltip("재화 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("재화 설명(카탈로그의 description).")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("선택. 현재 보유량 표시. 비우면 표시하지 않는다.")]
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button closeButton;

    [Header("Diamond (다이아 전용, 선택)")]
    [Tooltip("유상/무상 보유량을 담는 루트. 다이아를 열었을 때만 켜진다. 비우면 아래 텍스트만 켜고 끈다.")]
    [SerializeField] private GameObject diamondDetailGroup;
    [Tooltip("유상 다이아 보유량 텍스트.")]
    [SerializeField] private TMP_Text paidDiamondText;
    [Tooltip("무상 다이아 보유량 텍스트.")]
    [SerializeField] private TMP_Text freeDiamondText;
    [Tooltip("유상 + 무상 합계 텍스트. 위 보유량 표시(Amount Text)와 같은 값이며, 상세 안에서 합계를 함께 보여 줄 때 쓴다.")]
    [SerializeField] private TMP_Text totalDiamondText;

    /// <summary>소진 순서 안내. 무상부터 쓰는 규칙은 서버가 지키며, 여기서는 그 사실만 알린다.</summary>
    private const string SpendOrderHint = "무상 다이아부터 사용됩니다.";
    [Tooltip("선택. 소진 순서 안내 문구를 표시할 텍스트(다이아 그룹 안에 둔다).")]
    [SerializeField] private TMP_Text spendOrderText;

    private CurrencyType _type;
    private CancellationTokenSource _cts;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>표시할 재화를 지정한다. Initialize 보다 먼저 호출해야 한다(CurrencyView 가 그렇게 부른다).</summary>
    public void SetCurrency(CurrencyType type) => _type = type;

    public void Initialize(MainHudContext ctx) {
        if (ctx == null) return;

        CurrencyIcon.Apply(iconImage, ctx.Currencies, _type);

        if (nameText != null)
            nameText.text = ctx.Currencies != null ? ctx.Currencies.DisplayName(_type) : _type.ToString();

        if (descriptionText != null)
            descriptionText.text = ctx.Currencies != null ? ctx.Currencies.Description(_type) : "";

        if (amountText != null) {
            var prop = ctx.Store.Currency(_type);
            amountText.gameObject.SetActive(prop != null);
            if (prop != null) amountText.text = prop.Value.ToString("N0");
        }

        bool isDiamond = _type == CurrencyType.Diamond;
        ShowDiamondDetail(false);
        if (!isDiamond || ctx.DiamondDetail == null) return;

        if (spendOrderText != null) spendOrderText.text = SpendOrderHint;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        LoadDiamondDetailAsync(ctx, _cts.Token).Forget();
    }

    // 조회에 실패하면 구분을 감춘다. 합계는 이미 보이고 있으므로 화면이 비어 보이지는 않는다.
    private async UniTaskVoid LoadDiamondDetailAsync(MainHudContext ctx, CancellationToken ct) {
        try {
            var detail = await ctx.DiamondDetail.RequestAsync(ct);
            if (detail == null) return;

            if (paidDiamondText != null) paidDiamondText.text = detail.Paid.ToString("N0");
            if (freeDiamondText != null) freeDiamondText.text = detail.Free.ToString("N0");
            // 합계는 서버가 내려준 두 값의 합으로 적는다. 스토어의 보유량과 어긋나면 그 순간의 서버 값이 사실이다.
            if (totalDiamondText != null) totalDiamondText.text = detail.Total.ToString("N0");
            ShowDiamondDetail(true);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[Currency] 다이아 보유 내역을 불러오지 못했습니다: {e.Message}");
        }
    }

    private void ShowDiamondDetail(bool on) {
        if (diamondDetailGroup != null) { diamondDetailGroup.SetActive(on); return; }

        // 그룹을 배선하지 않았으면 텍스트만 개별로 켜고 끈다.
        if (paidDiamondText != null) paidDiamondText.gameObject.SetActive(on);
        if (freeDiamondText != null) freeDiamondText.gameObject.SetActive(on);
        if (totalDiamondText != null) totalDiamondText.gameObject.SetActive(on);
        if (spendOrderText != null) spendOrderText.gameObject.SetActive(on);
    }

    private void OnDestroy() {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
