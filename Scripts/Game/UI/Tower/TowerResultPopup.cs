using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무한 타워 결과. 등반에 성공하면 받은 보상을 보여 주고, 실패하면 다시 도전할지 나갈지를 await 로 돌려준다.
/// 실패는 횟수 제한이 없으므로 재도전은 실패했을 때 언제나 열려 있다. 성공한 날은 더 오를 수 없어 나가기만 남는다.
/// </summary>
public sealed class TowerResultPopup : UIView {
    public enum Choice { Exit, Retry }

    [Header("Result")]
    [Tooltip("도전한 층.")]
    [SerializeField] private TMP_Text floorText;
    [Tooltip("상단 문구(등반 성공 / 실패).")]
    [SerializeField] private TMP_Text messageText;
    [Tooltip("선택. 성공했을 때만 켜는 묶음.")]
    [SerializeField] private GameObject clearedGroup;
    [Tooltip("선택. 실패했을 때만 켜는 묶음.")]
    [SerializeField] private GameObject defeatGroup;

    [Header("Rewards")]
    [Tooltip("받은 보상 칸이 생성될 부모.")]
    [SerializeField] private Transform rewardContent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [Tooltip("선택. 받은 보상이 없을 때만 켜지는 문구(보상 묶음 안에 둔다). 보상 묶음 자체는 항상 보인다.")]
    [SerializeField] private TMP_Text noRewardText;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    private readonly List<RewardSlotView> _slots = new();
    private UniTaskCompletionSource<Choice> _result;

    /// <param name="granted">등반 성공으로 받은 보상(실패면 비어 있다).</param>
    public async UniTask<Choice> ShowAsync(BattleHudContext ctx, TowerResult result, int floor,
                                           IReadOnlyList<RewardItem> granted) {
        bool cleared = result == TowerResult.Cleared;

        if (floorText != null) floorText.text = TowerRules.FloorText(floor);
        if (messageText != null) messageText.text = cleared ? "등반에 성공했습니다." : "소녀들이 모두 쓰러졌습니다.";
        if (clearedGroup != null) clearedGroup.SetActive(cleared);
        if (defeatGroup != null) defeatGroup.SetActive(!cleared);

        BindRewards(ctx, granted);

        if (retryButton != null) retryButton.gameObject.SetActive(!cleared);

        _result = new UniTaskCompletionSource<Choice>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void BindRewards(BattleHudContext ctx, IReadOnlyList<RewardItem> granted) {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        bool has = granted != null && granted.Count > 0;
        if (noRewardText != null) noRewardText.gameObject.SetActive(!has);
        if (!has || rewardContent == null || rewardSlotPrefab == null) return;

        foreach (var item in granted) {
            var slot = Instantiate(rewardSlotPrefab, rewardContent);
            slot.gameObject.SetActive(true);
            slot.Bind(RewardVisualResolver.Resolve(ctx, item));
            _slots.Add(slot);
        }
    }

    protected override void Awake() {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(() => Resolve(Choice.Retry));
        if (exitButton != null) exitButton.onClick.AddListener(() => Resolve(Choice.Exit));
    }

    private void Resolve(Choice value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로로 닫혀도 반드시 결과를 확정한다. 확정하지 않으면 await 중인 씬 매니저가 멈춘다.
    protected override void OnClosed() => _result?.TrySetResult(Choice.Exit);
}
