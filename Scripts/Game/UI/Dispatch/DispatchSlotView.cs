using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파견 슬롯 1칸. 조건 하나와 편성 자리 5칸, 완료 시 받을 보상, 그리고 보내기/새로고침/완료 버튼을 갖는다.
///
/// 이 뷰는 판정을 하지 않는다. 조건 만족 여부와 상태(대기/파견중/완료대기)는 상위(DispatchPopup)가 정해
/// 넘겨 주고, 여기서는 받은 값을 그리고 버튼 클릭을 위로 올려 보내기만 한다. 편성 자리는 편성 화면과 같은
/// 부품(CompositionSlotView)을 그대로 쓰므로 드래그 규칙이 두 벌이 되지 않는다.
///
/// 편성 자리의 위치 번호는 "파견슬롯 x 인원수 + 자리번호"로 평탄하게 만든다(PositionOf).
/// 드래그 손잡이가 슬롯 번호를 정수 하나로만 다루므로, 이렇게 두면 25칸 전체가 한 축으로 표현된다.
/// </summary>
public sealed class DispatchSlotView : MonoBehaviour {
    [Header("Condition")]
    [Tooltip("파견 조건 문구(예: '검 타입 소녀 3명 이상').")]
    [SerializeField] private TMP_Text conditionText;
    [Tooltip("조건 충족 상태 문구. 상태에 따라 문구와 색이 함께 바뀐다.")]
    [SerializeField] private TMP_Text conditionStateText;
    [Tooltip("조건도 인원도 채웠을 때의 색.")]
    [SerializeField] private Color metColor = new Color(0.25f, 0.8f, 0.35f);
    [Tooltip("조건 수치가 모자랄 때의 색.")]
    [SerializeField] private Color unmetColor = Color.red;
    [Tooltip("조건 수치는 넘겼지만 인원이 덜 찼을 때의 색.")]
    [SerializeField] private Color shortHandedColor = new Color(1f, 0.72f, 0.25f);
    [Tooltip("불만족 문구 뒤에 붙는 진행도 \"(n/n)\" 의 색. 문구 본문과 다른 색으로 두어 수치가 눈에 띄게 한다.")]
    [SerializeField] private Color progressColor = new Color(1f, 0.84f, 0.29f);

    [Header("Members")]
    [Tooltip("편성 자리 5칸. 배열 순서가 곧 자리 번호다. 편성 화면과 같은 부품을 쓴다.")]
    [SerializeField] private CompositionSlotView[] memberSlots;
    [Tooltip("자리에 붙일 캐릭터 카드 프리팹. 목록 카드와 같은 프리팹을 넣어야 집은 카드가 그대로 보인다.")]
    [SerializeField] private DeckCharacterCard memberCardPrefab;

    [Header("Rewards")]
    [Tooltip("완료 시 받을 보상 슬롯이 생성될 부모.")]
    [SerializeField] private Transform rewardContent;
    [Tooltip("보상 슬롯 프리팹(RewardSlotView). 상점/우편과 같은 공용 부품이다.")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("State")]
    [Tooltip("파견 중일 때 켜는 묶음(남은 시간 등).")]
    [SerializeField] private GameObject runningGroup;
    [Tooltip("남은 시간 텍스트(\"04:12:07 남음\"). 파견이 끝나면 남은 시간이 없으므로 통째로 꺼진다.")]
    [SerializeField] private TMP_Text remainText;
    [Tooltip("파견 상태 문구(\"파견중\" / \"파견 종료\"). 남은 시간과 달리 파견 중 내내 켜져 있다.")]
    [SerializeField] private TMP_Text remainLabel;
    [Tooltip("대기 중일 때 켜는 묶음(편성/보내기).")]
    [SerializeField] private GameObject idleGroup;

    [Header("Buttons")]
    [Tooltip("실행 버튼 하나로 보내기/진행중/수령을 겸한다. 상태에 따라 문구와 활성이 바뀌므로 " +
             "Idle Group / Running Group 안이 아니라 그 밖에 배치해야 항상 보인다.")]
    [SerializeField] private Button actionButton;
    [Tooltip("실행 버튼의 문구(파견 / 파견중 / 완료). 비우면 문구를 바꾸지 않는다.")]
    [SerializeField] private TMP_Text actionButtonText;
    [Tooltip("완료 수령 가능 표시. 실행 버튼에 붙이고 Source=Manual 로 둔다. 이 뷰가 켜고 끈다.")]
    [SerializeField] private BadgeView actionBadge;
    [Tooltip("조건 새로고침. 파견 중이 아니고 갱신 횟수가 남아야 눌린다.")]
    [SerializeField] private Button refreshButton;
    [Tooltip("추천 배치. 남은 소녀 중 조건을 만족하는 가장 아깝지 않은 편성으로 이 슬롯을 채운다. 파견 중에는 숨긴다. 선택.")]
    [SerializeField] private Button recommendButton;

    /// <summary>이 뷰가 담당하는 파견 슬롯 번호. 상위가 생성하며 정해 준다.</summary>
    public int SlotIndex { get; private set; }

    /// <summary>슬롯 하나에 편성하는 인원(자리 배열 길이).</summary>
    public int MemberCapacity => memberSlots != null ? memberSlots.Length : 0;

    /// <summary>버튼이 눌렸을 때 상위에 알린다(인자는 이 뷰의 슬롯 번호).</summary>
    public event Action<int> SendRequested;
    public event Action<int> RefreshRequested;
    public event Action<int> ClaimRequested;
    public event Action<int> RecommendRequested;

    private readonly List<RewardSlotView> _rewardSlots = new();
    private DeckCharacterCard[] _memberCards;
    private long _endUnix;
    private bool _running;
    private bool _finished;
    private bool _canSend;
    private float _tick;

    /// <summary>슬롯 번호를 정하고 자리/버튼을 1회 구성한다.</summary>
    public void Initialize(int slotIndex, int membersPerSlot) {
        SlotIndex = slotIndex;
        _memberCards ??= new DeckCharacterCard[MemberCapacity];

        if (memberSlots != null)
            for (int i = 0; i < memberSlots.Length; i++)
                if (memberSlots[i] != null) memberSlots[i].Initialize(PositionOf(slotIndex, i, membersPerSlot));

        if (actionButton != null) {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }
        if (refreshButton != null) {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => RefreshRequested?.Invoke(SlotIndex));
        }
        if (recommendButton != null) {
            recommendButton.onClick.RemoveAllListeners();
            recommendButton.onClick.AddListener(() => RecommendRequested?.Invoke(SlotIndex));
        }
    }

    // 버튼은 하나지만 뜻은 상태가 정한다. 어느 요청인지 고르는 판단을 여기 두면 상위는 보내기/수령을
    // 그대로 나눠 받으므로, 버튼을 다시 둘로 쪼개도 상위 코드는 바뀌지 않는다.
    // 진행 중(완료 전)에는 버튼이 꺼져 있어 이 경로로 들어오지 않는다.
    private void OnActionClicked() {
        if (!_running) SendRequested?.Invoke(SlotIndex);
        else if (_finished) ClaimRequested?.Invoke(SlotIndex);
    }

    /// <summary>
    /// 편성 자리의 평탄한 위치 번호. 드래그 손잡이는 슬롯 번호를 정수 하나로만 다루므로
    /// 파견 슬롯과 자리 번호를 이 한 축으로 합쳐 25칸 전체를 표현한다.
    /// </summary>
    public static int PositionOf(int slotIndex, int memberIndex, int membersPerSlot) =>
        slotIndex * membersPerSlot + memberIndex;

    public static int SlotOfPosition(int position, int membersPerSlot) =>
        membersPerSlot <= 0 ? 0 : position / membersPerSlot;

    public static int MemberOfPosition(int position, int membersPerSlot) =>
        membersPerSlot <= 0 ? 0 : position % membersPerSlot;

    /// <summary>
    /// 조건 문구와 충족 상태를 표시한다.
    ///
    /// 수치가 모자랄 때는 얼마나 모자란지를 함께 알려 준다("파견 조건 불만족 (3/5)"). 진행도만 본문과
    /// 다른 색으로 두어, 문구를 읽지 않고 숫자만 훑어도 남은 양이 보이게 한다.
    /// 수치는 채웠는데 인원이 덜 찬 경우는 원인이 다르므로 문구와 색을 따로 둔다(인원만 채우면 보낼 수 있다).
    /// </summary>
    public void BindCondition(string text, DispatchRules.ConditionState state, int progress, int threshold,
                              bool showState) {
        if (conditionText != null) conditionText.text = text;
        if (conditionStateText == null) return;

        conditionStateText.gameObject.SetActive(showState);
        switch (state) {
            case DispatchRules.ConditionState.Met:
                conditionStateText.text = DispatchRules.ConditionMetText;
                conditionStateText.color = metColor;
                break;
            case DispatchRules.ConditionState.ShortHanded:
                conditionStateText.text = DispatchRules.ConditionShortHandedText;
                conditionStateText.color = shortHandedColor;
                break;
            default:
                string tag = ColorUtility.ToHtmlStringRGB(progressColor);
                conditionStateText.text =
                    $"{DispatchRules.ConditionUnmetText} <color=#{tag}>({progress:N0}/{threshold:N0})</color>";
                conditionStateText.color = unmetColor;
                break;
        }
    }

    /// <summary>완료 시 받을 보상을 공용 보상 슬롯으로 그린다.</summary>
    public void BindRewards(MainHudContext hud, IReadOnlyList<RewardItem> rewards) {
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
        if (rewardContent == null || rewardSlotPrefab == null || rewards == null) return;

        foreach (var item in rewards) {
            var slot = Instantiate(rewardSlotPrefab, rewardContent);
            slot.Bind(RewardVisualResolver.Resolve(hud, item));
            _rewardSlots.Add(slot);
        }
    }

    /// <summary>슬롯 상태(대기/파견중/완료대기)에 맞춰 묶음과 버튼을 켜고 끈다.</summary>
    public void BindState(bool running, bool finished, long endUnix, bool canSend, bool canRefresh, bool canRecommend) {
        _running = running;
        _finished = finished;
        _endUnix = endUnix;

        if (idleGroup != null) idleGroup.SetActive(!running);
        if (runningGroup != null) runningGroup.SetActive(running);

        _canSend = canSend;

        if (refreshButton != null) {
            refreshButton.gameObject.SetActive(!running);
            refreshButton.interactable = canRefresh;
        }
        // 추천 배치는 편성을 바꾸는 버튼이라 편성을 바꿀 수 있는 대기 중에만 보인다(새로고침과 같은 규칙).
        if (recommendButton != null) {
            recommendButton.gameObject.SetActive(!running);
            recommendButton.interactable = canRecommend;
        }

        ApplyActionButton();
        RefreshRemain();
    }

    // 실행 버튼 하나가 보내기/진행중/수령을 겸한다. 진행 중(완료 전)에는 문구만 남기고 눌리지 않게 한다.
    // 완료 수령이 가능한 슬롯에만 배지가 켜진다(팝업/햄버거 배지와 같은 근거를 쓴다).
    private void ApplyActionButton() {
        if (actionButton != null) actionButton.interactable = !_running ? _canSend : _finished;
        if (actionButtonText != null) actionButtonText.text = DispatchRules.ActionText(_running, _finished);
        if (actionBadge != null) actionBadge.SetOn(_finished);
    }

    /// <summary>자리 하나에 캐릭터를 붙이거나 비운다. 카드는 자리마다 하나를 만들어 재사용한다.</summary>
    public void BindMember(int memberIndex, MainHudContext hud, int charDataId,
                           CompositionDragHandle.IHost dragHost, int position, bool draggable) {
        if (memberSlots == null || memberIndex < 0 || memberIndex >= memberSlots.Length) return;
        var slot = memberSlots[memberIndex];
        if (slot == null) return;

        var data = charDataId != 0 && hud.CharDb != null ? hud.CharDb.Get(charDataId) : null;
        if (data == null) {
            if (_memberCards[memberIndex] != null) {
                Destroy(_memberCards[memberIndex].gameObject);
                _memberCards[memberIndex] = null;
            }
            slot.SetFilled(false);
            return;
        }

        var card = _memberCards[memberIndex];
        if (card == null && memberCardPrefab != null) {
            card = Instantiate(memberCardPrefab);
            slot.Attach((RectTransform)card.transform);
            _memberCards[memberIndex] = card;
        }
        if (card == null) return;

        card.Bind(hud.Appearance.Mine(data), hud.Store.GetProgress(charDataId), null);
        // 파견 자리에서는 덱 표식을 쓰지 않는다(덱과 완전히 별개인 컨텐츠라 덱 편성 여부가 의미를 갖지 않는다).
        card.SetState(inDeck: false, isRepresentative: false, isSelected: false);
        card.SetUpgradeBadge(false);
        card.SetGuideTarget(false);
        // 파견 중인 슬롯은 편성을 바꿀 수 없으므로 손잡이를 꺼 둔다(host=null 이면 드래그가 꺼진다).
        card.SetDragSource(draggable ? dragHost : null, position);
        slot.SetFilled(true);
    }

    /// <summary>
    /// 남은 시간("04:12:07 남음")과 상태 문구("파견중" / "파견 종료")를 갱신한다.
    ///
    /// 끝난 파견은 남은 시간이 "00:00:00 남음"으로 남아 있으면 아직 기다려야 하는 것처럼 읽히므로
    /// 시간 표시를 통째로 끄고, 상태 문구만 "파견 종료"로 바꿔 수령하면 된다는 것을 알린다.
    /// 파견은 최대 8시간이라 일수가 붙을 일이 없지만, 표기는 앱 공용 카운트다운 규칙을 그대로 쓴다.
    /// </summary>
    private void RefreshRemain() {
        bool counting = _running && !_finished && _endUnix > 0;

        if (remainText != null) {
            remainText.gameObject.SetActive(counting);
            if (counting) remainText.text = $"{KstTime.Countdown(_endUnix - KstTime.NowUnix)} 남음";
        }
        if (remainLabel != null) {
            remainLabel.gameObject.SetActive(_running);
            if (_running) remainLabel.text = _finished ? DispatchRules.RunningDoneText : DispatchRules.RunningText;
        }
    }

    // 남은 시간은 초 단위로만 바뀌므로 매 프레임이 아니라 1초에 한 번만 다시 그린다
    // (파견 슬롯이 다섯 개 깔리는 화면이다).
    //
    // 화면을 열어 둔 채로 시간이 다 되는 경우가 있으므로, 여기서 완료로 넘어가는 것도 함께 본다.
    // 이걸 빠뜨리면 팝업을 닫았다 다시 열기 전까지 버튼이 "파견중"에 머문다.
    private void Update() {
        if (!_running || _endUnix <= 0) return;
        _tick += Time.unscaledDeltaTime;
        if (_tick < 1f) return;
        _tick = 0f;

        if (!_finished && KstTime.NowUnix >= _endUnix) {
            _finished = true;
            ApplyActionButton();
        }
        RefreshRemain();
    }

    /// <summary>붙여 둔 카드와 보상 슬롯을 모두 정리한다(팝업이 닫힐 때).</summary>
    public void Clear() {
        if (_memberCards != null)
            for (int i = 0; i < _memberCards.Length; i++) {
                if (_memberCards[i] != null) Destroy(_memberCards[i].gameObject);
                _memberCards[i] = null;
            }
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
    }
}
