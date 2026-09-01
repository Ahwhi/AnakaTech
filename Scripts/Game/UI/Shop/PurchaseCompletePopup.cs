using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "무엇을 받았는지"를 구성품 슬롯(RewardSlotView)으로 보여주는 공용 결과 팝업.
/// 상점 구매가 첫 사용처라 이름이 그렇지만, 받은 목록을 그리는 화면이 필요한 곳은 모두 이것을 쓴다
/// (상점 구매 / 쿠폰 사용). 표시 규칙은 구매확인 팝업(BuyConfirmPopup)과 같다.
///
/// 보여줄 목록은 호출부가 확정해 넘긴다. 서버가 실제로 지급한 목록을 넘기면 랜덤 상품도
/// "랜덤 만능조각"이 아니라 실제로 받은 "SSR 만능조각"으로 표시된다.
/// 이 팝업은 넘어온 항목을 그대로 그리기만 하므로 품목이 늘어도 수정할 필요가 없다.
///
/// 호출부는 닫힐 때까지 await 한다.
///   await popup.ShowAsync(hud, offer, rewards);                       // 상점 구매
///   await popup.ShowAsync(hud, "쿠폰 보상", code, null, rewards);       // 그 외
///
/// 가챠/장비 뽑기는 전용 연출 팝업이 결과를 보여주므로 이 팝업을 쓰지 않는다.
/// </summary>
public sealed class PurchaseCompletePopup : UIView {
    [Header("Header")]
    [Tooltip("선택. 상단 제목(비워 두면 '구매 완료'가 들어간다).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("선택. 구매한 상품 이름. 카탈로그의 이름 색을 그대로 적용한다.")]
    [SerializeField] private TMP_Text itemNameText;
    [Tooltip("선택. 마무리 안내 문구(예: 구매가 완료되었습니다 / 우편함으로 지급되었습니다). 호출부가 정한다.")]
    [SerializeField] private TMP_Text messageText;

    [Header("Rewards (획득 구성품)")]
    [Tooltip("선택. 구성품 슬롯 그룹 루트(헤더 포함).")]
    [SerializeField] private GameObject rewardGroup;
    [Tooltip("구성품 슬롯을 생성할 부모.")]
    [SerializeField] private Transform rewardRoot;
    [Tooltip("구성품 1개를 표시할 공용 슬롯 프리팹(RewardSlotView).")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Close")]
    [SerializeField] private Button okButton;

    /// <summary>상점 구매에서 쓰는 제목/안내 문구.</summary>
    private const string PurchaseTitle = "구매 완료";
    /// <summary>구매 완료 안내. 팝업을 띄우지 못할 때 토스트로도 같은 문구를 쓰도록 공개한다.</summary>
    public const string PurchaseMessage = "구매가 완료되었습니다.";

    private readonly List<RewardSlotView> _rewardSlots = new();
    private UniTaskCompletionSource _closed;

    protected override void Awake() {
        base.Awake();
        // 배경 탭으로 닫기는 베이스(UIView.tapToCloseArea)가 처리한다.
        if (okButton != null) okButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>상점 구매 결과. 상품 이름과 이름 색을 오퍼에서 꺼내 공용 표시로 넘긴다.</summary>
    /// <param name="hud">구성품 아이콘/이름 해석에 쓰는 카탈로그 묶음(RewardVisualResolver 가 사용).</param>
    /// <param name="offer">구매한 상품(이름 표시용).</param>
    /// <param name="rewards">표시할 획득 항목. 수량은 이미 확정된 값이어야 한다(여기서 배수를 곱하지 않는다).</param>
    public UniTask ShowAsync(BattleHudContext hud, ShopOfferDto offer, IReadOnlyList<RewardItem> rewards) =>
        ShowAsync(hud, PurchaseTitle, offer != null ? offer.DisplayName : "",
                  offer != null ? offer.NameColorHex : null, PurchaseMessage, rewards);

    /// <summary>
    /// 받은 목록을 보여준다(공용). 제목/부제/안내 문구를 호출부가 정하므로 구매 외의 지급에도 그대로 쓸 수 있다.
    /// </summary>
    /// <param name="title">상단 제목(예: 구매 완료 / 쿠폰 보상).</param>
    /// <param name="subject">제목 아래 한 줄(상품 이름 또는 쿠폰 번호). 비우면 표시하지 않는다.</param>
    /// <param name="subjectColorHex">부제 색(#RRGGBB). 비우면 프리팹 색을 그대로 쓴다.</param>
    /// <param name="message">마무리 안내 문구. 받은 것이 어디로 갔는지가 사용처마다 달라 호출부가 정한다.</param>
    public async UniTask ShowAsync(BattleHudContext hud, string title, string subject, string subjectColorHex,
                                   string message, IReadOnlyList<RewardItem> rewards) {
        // 제목과 안내 문구는 넘어온 값으로 항상 덮는다. "비어 있을 때만" 채우면 프리팹에 글자를 적어 둔 순간
        // 호출부가 지정한 문구가 무시되어, 쿠폰 결과에 "구매가 완료되었습니다"가 뜨는 식으로 어긋난다.
        if (titleText != null) titleText.text = title ?? "";

        if (itemNameText != null) {
            bool hasSubject = !string.IsNullOrEmpty(subject);
            itemNameText.gameObject.SetActive(hasSubject);
            itemNameText.text = subject ?? "";
            if (hasSubject && !string.IsNullOrEmpty(subjectColorHex)
                && ColorUtility.TryParseHtmlString(subjectColorHex, out var col))
                itemNameText.color = col;
        }

        if (messageText != null) {
            bool hasMessage = !string.IsNullOrEmpty(message);
            messageText.gameObject.SetActive(hasMessage);
            messageText.text = message ?? "";
        }

        BuildRewardSlots(hud, rewards);

        // 확인/배경 탭 외의 경로(외부 CloseAllPopups 등)로 닫혀도 OnClosed 가 완료를 확정하므로
        // await 중인 구매 흐름이 멈춘 채로 남지 않는다.
        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();
        await _closed.Task;
    }

    // 넘어온 획득 항목만큼 슬롯을 만들어 바인딩한다.
    private void BuildRewardSlots(BattleHudContext hud, IReadOnlyList<RewardItem> rewards) {
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();

        bool hasRewards = rewards != null && rewards.Count > 0;
        if (rewardGroup != null) rewardGroup.SetActive(hasRewards);
        if (!hasRewards || rewardRoot == null || rewardSlotPrefab == null) return;

        for (int i = 0; i < rewards.Count; i++) {
            var slot = Instantiate(rewardSlotPrefab, rewardRoot);
            slot.Bind(RewardVisualResolver.Resolve(hud, rewards[i]));
            _rewardSlots.Add(slot);
        }
    }

    protected override void OnClosed() {
        _closed?.TrySetResult();
    }
}
