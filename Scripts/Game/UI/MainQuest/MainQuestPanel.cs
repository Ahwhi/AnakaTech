using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 메인 화면의 메인 퀘스트 안내(PLANNING: 유저의 다음 행동을 한 줄로 알려 주는 자리).
/// 한 번에 하나만 진행되므로 목록이 아니라 한 줄이고, 완료되면 색이 바뀌고 완료 묶음(보상 + 수령 안내)이 켜진다.
///
/// 완료 상태에서 이 UI 를 누르면 곧 보상 수령이다. 완료와 수령을 다른 버튼으로 나누면 한 번 더 눌러야 해서,
/// "완료 표시를 보면 바로 누르게" 하려는 목적과 어긋난다.
///
/// 현황은 서비스의 ReactiveProperty 를 구독한다. 퀘스트는 다른 화면에서 한 행동 때문에 완료되므로
/// 이 패널이 서버에 물어보는 것이 아니라 서버가 알려 주는 값이다.
/// </summary>
public sealed class MainQuestPanel : MonoBehaviour {
    [Header("Root")]
    [Tooltip("퀘스트 표시 묶음. 준비된 퀘스트를 전부 끝내면 통째로 꺼진다.")]
    [SerializeField] private GameObject questGroup;

    [Header("Quest")]
    [Tooltip("퀘스트 레벨 표시(예: 메인 퀘스트 Lv.7). 선택.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("목표 안내 문구. 완료되면 색이 completedColor 로 바뀐다.")]
    [SerializeField] private TMP_Text objectiveText;
    [Tooltip("진행도 표시(예: 0/1). 선택.")]
    [SerializeField] private TMP_Text progressText;
    [Tooltip("진행 중일 때의 목표 문구 색.")]
    [SerializeField] private Color inProgressColor = Color.white;
    [Tooltip("완료했을 때의 목표 문구 색(수령을 유도하는 색).")]
    [SerializeField] private Color completedColor = new Color(1f, 0.85f, 0.25f, 1f);

    [Header("Completed")]
    [Tooltip("완료했을 때만 켜지는 묶음(보상 슬롯 + '수령' 안내). 진행 중에는 꺼진다.")]
    [SerializeField] private GameObject completedGroup;
    [Tooltip("완료했을 때만 켜지는 추가 묶음(테두리/강조 연출 등). 하이어라키상 completedGroup 과 같이 둘 수 없는 " +
             "요소를 위한 자리이며, 켜고 끄는 시점은 completedGroup 과 완전히 같다. 비워도 된다.")]
    [SerializeField] private GameObject completedFrame;
    [Tooltip("완료 묶음 안의 보상 슬롯이 생성될 부모.")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Input")]
    [Tooltip("퀘스트 UI 전체를 덮는 버튼. 완료 상태에서 누르면 보상을 수령한다.")]
    [SerializeField] private Button claimButton;

    [Header("Layout")]
    [Tooltip("완료 묶음이 켜지고 꺼질 때 크기를 다시 맞출 LayoutAutoRebuild(보통 이 패널의 " +
             "VerticalLayoutGroup + ContentSizeFitter 가 붙은 오브젝트). 비워도 동작은 하지만 배경 이미지가 " +
             "늘어난 내용에 맞춰 커지지 않을 수 있다.")]
    [SerializeField] private LayoutAutoRebuild layoutRebuild;

    private MainHudContext _ctx;
    private readonly List<RewardSlotView> _slots = new();
    private readonly CompositeDisposable _subs = new();

    private MainQuestDto _quest;
    private bool _busy;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        if (claimButton != null) claimButton.onClick.AddListener(OnClaim);

        if (ctx.MainQuest == null) {
            // 서비스가 없으면 표시할 근거가 없다(퀘스트 정의는 전부 서버에 있다).
            if (questGroup != null) questGroup.SetActive(false);
            return;
        }

        ctx.MainQuest.Current.Subscribe(Bind).AddTo(_subs);
    }

    private void Bind(MainQuestDto quest) {
        _quest = quest;

        bool has = quest != null && quest.HasQuest;
        if (questGroup != null) questGroup.SetActive(has);
        if (!has) {
            // 내용이 통째로 사라졌으므로 패널도 그만큼 줄어야 한다.
            if (layoutRebuild != null) layoutRebuild.RebuildDeferred();
            return;
        }

        if (levelText != null) levelText.text = MainQuestRules.LevelLabel(quest.Level);
        if (objectiveText != null) {
            objectiveText.text = MainQuestRules.Describe(quest.Objective, quest.Param, _ctx.DungeonDb);
            objectiveText.color = quest.IsCompleted ? completedColor : inProgressColor;
        }
        if (progressText != null)
            progressText.text = MainQuestRules.ProgressLabel(quest.Progress, quest.Threshold);

        if (completedGroup != null) completedGroup.SetActive(quest.IsCompleted);
        if (completedFrame != null) completedFrame.SetActive(quest.IsCompleted);
        if (claimButton != null) claimButton.interactable = quest.IsCompleted && !_busy;

        BuildRewards(quest.Rewards);

        // 완료 묶음이 켜지고 보상 슬롯이 늘어나면 패널 높이가 달라진다. 이 변화는 글자가 아니라 오브젝트의
        // 켜짐/생성이라 자동 감지(문자열 비교)에 걸리지 않으므로, 바꾼 쪽이 직접 알린다.
        if (layoutRebuild != null) layoutRebuild.RebuildDeferred();
    }

    // 보상 슬롯을 필요한 만큼만 만들어 재사용한다(퀘스트가 넘어갈 때마다 다시 그려진다).
    private void BuildRewards(IReadOnlyList<RewardItem> rewards) {
        if (rewardSlotPrefab == null || rewardParent == null) return;

        int count = rewards != null ? rewards.Count : 0;
        while (_slots.Count < count) _slots.Add(Instantiate(rewardSlotPrefab, rewardParent));

        for (int i = 0; i < _slots.Count; i++) {
            bool used = i < count;
            _slots[i].gameObject.SetActive(used);
            if (used) _slots[i].Bind(RewardVisualResolver.Resolve(_ctx, rewards[i]));
        }
    }

    private void OnClaim() {
        if (_busy || _quest == null || !_quest.IsCompleted) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        ClaimAsync(_quest.Level).Forget();
    }

    private async UniTaskVoid ClaimAsync(int level) {
        _busy = true;
        if (claimButton != null) claimButton.interactable = false;

        try {
            var res = await _ctx.MainQuest.ClaimAsync(level, this.GetCancellationTokenOnDestroy());
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(MainQuestRules.ClaimFailMessage(res.FailReason));
                return;
            }

            // 다이아/경험치는 서버가 확정해 내려 준 값을 그대로 반영한다(화면이 다시 계산하지 않는다).
            _ctx.Store.ApplyMainQuestReward(res);
            _ctx.Sound.PlaySfx(SfxKey.Claim);

            await RefreshInventoryIfNeededAsync(res);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("보상을 받지 못했습니다. 잠시 후 다시 시도해 주세요.");
        } finally {
            _busy = false;
            // 다음 퀘스트 현황은 서버가 이어서 보내 주므로 여기서는 버튼 상태만 지금 값에 맞춘다.
            if (claimButton != null) claimButton.interactable = _quest != null && _quest.IsCompleted;
        }
    }

    /// <summary>
    /// 장비를 보상으로 받았으면 가방을 다시 받아 온다.
    ///
    /// 장비는 개수가 아니라 인스턴스다(고유 id + 서버가 굴린 옵션 수치). 수령 응답은 "무엇을 몇 개 줬는가"만
    /// 담고 있어 클라가 인스턴스를 만들어 낼 수 없으므로, 목록을 한 번 다시 받는 것이 유일한 방법이다.
    /// 장비 보상이 있을 때만 하므로 평소 수령에는 추가 통신이 없다.
    ///
    /// 조각/소모품 같은 개수형 보상은 이미 응답만으로 반영되므로(ApplyGrantedRewards) 여기 해당하지 않는다.
    /// </summary>
    private async UniTask RefreshInventoryIfNeededAsync(MainQuestClaimResultDto res) {
        if (_ctx.Inventory == null || !HasEquip(res.Granted)) return;

        var inventory = await _ctx.Inventory.RequestInventoryAsync(this.GetCancellationTokenOnDestroy());
        _ctx.Store.ApplyInventory(inventory);
    }

    private static bool HasEquip(IReadOnlyList<RewardItem> granted) {
        if (granted == null) return false;
        for (int i = 0; i < granted.Count; i++)
            if (granted[i].Kind == RewardItemKind.Equip || granted[i].Kind == RewardItemKind.RandomEquip
                || granted[i].Kind == RewardItemKind.RandomGeneralEquip) return true;
        return false;
    }

    private void OnDestroy() => _subs.Dispose();
}
