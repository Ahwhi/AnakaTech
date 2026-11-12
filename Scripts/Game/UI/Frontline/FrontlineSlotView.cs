using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전선 팝업의 전선 카드 1장(가로 3 x 세로 2 그리드에 6장을 배치한다). 순수 표시부라 상태를 들고 있지 않고,
/// 버튼 입력은 이벤트로만 알린다(무엇을 할지는 전선 팝업이 정한다).
///
///   썸네일/이름/레벨      전선 데이터와 지금 칠 단계
///   스쿼드               배치된 소녀 5칸(빈 칸은 꺼진다)
///   시너지               이 스쿼드로 켜진 시너지 이름만
///   전투력               이 스쿼드의 전투력 합(시너지 포함)
///   진행 상황            단계 / 최고 단계
///   획득 가능한 경험치    이 단계를 깼을 때 소녀 1명이 받는 경험치(RewardSlotView)
///   참여 버튼            기력 아이콘 + 소모량. 기력이나 오늘 한도가 모자라면 빨간 글씨 + 비활성
/// </summary>
public sealed class FrontlineSlotView : MonoBehaviour {
    [Header("Info")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("지금 칠 단계(예: Lv.12).")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("선택. 클리어 기록이 있어 스쿼드가 고정됐을 때 켜지는 표식.")]
    [SerializeField] private GameObject lockedMark;

    [Header("Squad")]
    [Tooltip("스쿼드 5칸. 배열 순서가 곧 배치 순서다.")]
    [SerializeField] private DeckMemberPreview[] memberViews;
    [Tooltip("선택. 배치된 소녀가 한 명도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptySquadGroup;
    [Tooltip("이 스쿼드의 전투력 합.")]
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("이 스쿼드로 켜진 시너지 이름들(쉼표로 잇는다).")]
    [SerializeField] private TMP_Text synergyText;
    [SerializeField] private string noSynergyText = "없음";

    [Header("Progress")]
    [Tooltip("진행 상황 비율(단계 / 최고 단계).")]
    [SerializeField] private Slider progressSlider;
    [Tooltip("진행 상황 표기(예: 12 / 50).")]
    [SerializeField] private TMP_Text progressText;

    [Header("Reward")]
    [Tooltip("이 단계를 깼을 때 소녀 1명이 받는 경험치 칸(투자 전선 경험치 반영).")]
    [SerializeField] private RewardSlotView expRewardSlot;
    [Tooltip("선택. 투자 반영 안내(예: 투자 15% 반영). 전선 경험치 투자가 0 이면 꺼진다.")]
    [SerializeField] private TMP_Text investmentText;

    [Header("Buttons")]
    [SerializeField] private Button deployButton;
    [SerializeField] private Button joinButton;
    [Tooltip("참여 버튼 안의 기력 소모량 텍스트(기력 아이콘 옆, 예: 100).")]
    [SerializeField] private TMP_Text joinCostText;
    [SerializeField] private Color joinCostColor = Color.white;
    [SerializeField] private Color joinCostShortColor = new(0.90f, 0.20f, 0.20f);
    [SerializeField] private Button resetButton;

    /// <summary>배치 버튼. 인자는 전선 id.</summary>
    public event Action<int> DeployRequested;
    /// <summary>참여 버튼. 인자는 전선 id.</summary>
    public event Action<int> JoinRequested;
    /// <summary>초기화 버튼. 인자는 전선 id.</summary>
    public event Action<int> ResetRequested;

    public int FrontlineId { get; private set; } = FrontlineSession.None;

    private readonly List<SynergyData> _synergies = new();
    private bool _wired;

    public void Initialize(int frontlineId) {
        FrontlineId = frontlineId;
        if (_wired) return;
        _wired = true;
        if (deployButton != null) deployButton.onClick.AddListener(() => DeployRequested?.Invoke(FrontlineId));
        if (joinButton != null) joinButton.onClick.AddListener(() => JoinRequested?.Invoke(FrontlineId));
        if (resetButton != null) resetButton.onClick.AddListener(() => ResetRequested?.Invoke(FrontlineId));
    }

    /// <param name="info">계정 현황. 받기 전이면 null(이름/썸네일만 그린다).</param>
    /// <param name="busy">통신 중이면 버튼을 잠근다.</param>
    public void Bind(MainHudContext ctx, FrontlineDatabase db, FrontlineInfoDto info, bool busy) {
        if (thumbnailImage != null) {
            var thumb = db != null ? db.ThumbnailOf(FrontlineId) : null;
            if (thumb != null) thumbnailImage.sprite = thumb;
        }
        if (nameText != null) nameText.text = db != null ? db.NameOf(FrontlineId) : FrontlineRules.DefaultName(FrontlineId);

        var squad = info?.Squad(FrontlineId);
        int stage = squad?.Stage ?? FrontlineRules.StartStage;
        int maxStage = info?.MaxStage ?? FrontlineRules.MaxStage;
        IReadOnlyList<int> members = squad != null ? squad.Members : Array.Empty<int>();

        if (levelText != null) levelText.text = FrontlineRules.StageText(stage);
        if (lockedMark != null) lockedMark.SetActive(squad != null && squad.Locked);
        if (progressSlider != null) progressSlider.value = maxStage > 0 ? (float)stage / maxStage : 0f;
        if (progressText != null) progressText.text = $"{stage} / {maxStage}";
        int bonus = FrontlineRules.InvestmentPermille(ctx.Store);
        if (expRewardSlot != null)
            expRewardSlot.Bind(RewardVisualResolver.Resolve(ctx, RewardItem.Exp(FrontlineRules.ExpFor(stage, bonus))));
        if (investmentText != null) {
            investmentText.gameObject.SetActive(bonus > 0);
            investmentText.text = InvestmentRules.BonusLabel(bonus);
        }

        BindMembers(ctx, members);
        BindSynergies(ctx, members);
        if (combatPowerText != null) combatPowerText.text = AccountStats.SquadCombatPower(ctx, members).ToString("N0");

        BindButtons(ctx, info, squad, busy);
    }

    private void BindMembers(MainHudContext ctx, IReadOnlyList<int> members) {
        if (emptySquadGroup != null) emptySquadGroup.SetActive(members.Count == 0);
        if (memberViews == null) return;

        for (int i = 0; i < memberViews.Length; i++) {
            var view = memberViews[i];
            if (view == null) continue;

            int id = i < members.Count ? members[i] : 0;
            var data = id != 0 ? ctx.CharDb.Get(id) : null;
            var progress = id != 0 ? ctx.Store.GetProgress(id) : null;
            if (data == null || progress == null) { view.Clear(); continue; }
            view.Bind(ctx.Appearance.Mine(data), progress.StarLevel, progress.Level, progress.LimitBroken);
        }
    }

    private void BindSynergies(MainHudContext ctx, IReadOnlyList<int> members) {
        if (synergyText == null) return;
        if (ctx.SynergyDb != null) ctx.SynergyDb.CollectActive(members, _synergies);
        else _synergies.Clear();

        var sb = new StringBuilder();
        foreach (var s in _synergies) {
            if (s == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(s.DisplayName);
        }
        synergyText.text = sb.Length > 0 ? sb.ToString() : noSynergyText;
    }

    private void BindButtons(MainHudContext ctx, FrontlineInfoDto info, FrontlineSquadDto squad, bool busy) {
        int cost = info?.StaminaPerClear ?? FrontlineRules.StaminaPerClear;
        // 빨간 글씨는 보유 기력이 모자랄 때만 쓴다. 오늘 한도가 모자란 것은 버튼 비활성만으로 알린다.
        bool enoughStamina = ctx.Store.Stamina.Value >= cost;
        bool affordable = info != null && enoughStamina && info.DailyStaminaRemaining >= cost;
        bool hasMembers = squad != null && squad.Members.Count > 0;

        if (joinCostText != null) {
            joinCostText.text = cost.ToString("N0");
            joinCostText.color = enoughStamina ? joinCostColor : joinCostShortColor;
        }
        if (joinButton != null) joinButton.interactable = !busy && affordable && hasMembers;

        // 클리어 기록이 있으면 스쿼드가 고정되므로 배치 버튼을 숨기고 초기화 버튼을 보인다.
        // 고정을 푸는 길은 초기화뿐이라 두 버튼이 같은 자리를 번갈아 쓴다.
        bool locked = squad != null && squad.Locked;
        if (deployButton != null) {
            deployButton.gameObject.SetActive(!locked);
            deployButton.interactable = !busy && info != null;
        }
        if (resetButton != null) {
            resetButton.gameObject.SetActive(locked);
            resetButton.interactable = !busy;
        }
    }
}
