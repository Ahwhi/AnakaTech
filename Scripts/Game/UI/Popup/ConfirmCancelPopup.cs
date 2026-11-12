using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 확인/취소 팝업(공용). 확인=true, 취소=false 를 await 로 돌려준다.
/// "현재 던전을 종료하시겠습니까?"(PLANNING 1-3), "다이아가 부족합니다. 구매하시겠습니까?"(PLANNING 10) 등 재사용.
///   var yes = await popup.ShowAsync("던전 종료", "현재 진행중인 던전을 종료하시겠습니까?");
///
/// 받게 될 품목을 함께 보여 줄 때는 보상 목록을 넘긴다(공용 RewardSlotView 로 그린다).
///   var yes = await popup.ShowAsync(hud, "성장 초기화", message, rewards);
/// 보상 칸(Rewards 그룹)은 선택이라 비워 둔 프리팹은 지금처럼 문구만 보여 준다.
/// </summary>
public sealed class ConfirmCancelPopup : UIView {
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    [Header("Rewards (optional)")]
    [Tooltip("선택. 보상 목록을 넘겼을 때만 켜는 그룹(제목/배경 포함). 보상이 없으면 꺼진다.")]
    [SerializeField] private GameObject rewardGroup;
    [Tooltip("선택. 보상 칸이 생성될 부모(HorizontalLayoutGroup / GridLayoutGroup 권장).")]
    [SerializeField] private Transform rewardRoot;
    [Tooltip("선택. 보상 1개를 그릴 공용 슬롯 프리팹(RewardSlotView).")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    private UniTaskCompletionSource<bool> _result;
    private readonly List<RewardSlotView> _rewardSlots = new();

    protected override void Awake() {
        base.Awake();
        if (okButton != null) okButton.onClick.AddListener(() => Resolve(true));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(false));
    }

    public UniTask<bool> ShowAsync(string title, string message) => ShowAsync(null, title, message, null);

    /// <param name="hud">보상 아이콘/이름 해석용 카탈로그 묶음. 보상이 없으면 null 이어도 된다.</param>
    /// <param name="rewards">받게 될 품목(없으면 null). 수량 0 이하인 항목은 그리지 않는다.</param>
    public async UniTask<bool> ShowAsync(BattleHudContext hud, string title, string message, IReadOnlyList<RewardItem> rewards) {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        BuildRewards(hud, rewards);

        _result = new UniTaskCompletionSource<bool>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void BuildRewards(BattleHudContext hud, IReadOnlyList<RewardItem> rewards) {
        ClearRewards();
        bool canDraw = hud != null && rewardRoot != null && rewardSlotPrefab != null;
        if (canDraw && rewards != null) {
            foreach (var r in rewards) {
                if (r.Amount <= 0) continue;
                var slot = Instantiate(rewardSlotPrefab, rewardRoot);
                slot.Bind(RewardVisualResolver.Resolve(hud, r));
                _rewardSlots.Add(slot);
            }
        }
        if (rewardGroup != null) rewardGroup.SetActive(_rewardSlots.Count > 0);
    }

    private void ClearRewards() {
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
    }

    private void Resolve(bool value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // OK/취소 외의 경로(배경 탭, 외부 CloseAllPopups 등)로 닫혀도 반드시 결과를 확정해,
    // await 중인 호출부가 영원히 멈추는 것을 막는다(취소로 간주).
    protected override void OnClosed() {
        _result?.TrySetResult(false);
        ClearRewards();
    }
}
