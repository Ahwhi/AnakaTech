using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수련 슬롯 1칸의 뷰. 네 가지 상태를 그린다.
///   잠김   : 해금 비용 + 해금 버튼
///   빈 칸  : 안내 문구(오른쪽 목록에서 소녀를 고르면 시작된다)
///   진행중 : 소녀 초상 + 남은 시간 카운트다운 + 진행 막대 + 중단/즉시완료 버튼
///   완료   : 소녀 초상 + 수련 완료 버튼
///
/// 판단(무엇을 할 수 있는가)은 팝업이 하고 이 뷰는 상태를 받아 그리기만 한다. 남은 시간만 스스로 흘려보내며
/// (1초마다 서버를 두드리지 않기 위함), 완료 시각을 넘기는 순간 팝업에 알려 버튼 구성을 바꾸게 한다.
/// </summary>
public sealed class TrainingSlotView : MonoBehaviour {
    /// <summary>슬롯이 지금 보여주는 상태.</summary>
    public enum State { Locked, Empty, Running, Ready }

    [Header("Groups (상태별로 켜고 끌 그룹)")]
    [SerializeField] private GameObject lockedGroup;
    [SerializeField] private GameObject emptyGroup;
    [SerializeField] private GameObject runningGroup;
    [SerializeField] private GameObject readyGroup;

    [Header("Locked")]
    [Tooltip("해금 비용 표시(다이아 수량).")]
    [SerializeField] private TMP_Text unlockCostText;
    [Tooltip("해금 비용 재화 아이콘(다이아). 스프라이트는 재화 카탈로그(CurrencyCatalog)에서 가져오므로 " +
             "여기에는 빈 Image 만 배치하면 된다. 비우면 아이콘 없음.")]
    [SerializeField] private Image unlockCostIcon;
    [SerializeField] private Button unlockButton;

    [Header("Character")]
    [Tooltip("수련 중인 소녀의 초상. 진행중/완료 상태에서 표시한다.")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("수련 중인 소녀 이름. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text characterNameText;

    [Header("Running")]
    [Tooltip("남은 시간 카운트다운(예: 12:34:56).")]
    [SerializeField] private TMP_Text remainText;
    [Tooltip("진행률 막대(Slider, MinValue=0 MaxValue=1, Interactable=off). 비우면 표시 안 함.")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button cancelButton;
    [Tooltip("즉시 완료 버튼. 수련 완료권을 소모한다.")]
    [SerializeField] private Button instantButton;
    [Tooltip("즉시 완료에 드는 수련 완료권 장수 표시. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text instantCostText;
    [Tooltip("즉시 완료 비용 재화 아이콘(수련 완료권). 해금 아이콘과 같은 방식으로 카탈로그에서 가져온다. 비우면 아이콘 없음.")]
    [SerializeField] private Image instantCostIcon;

    [Header("Ready")]
    [Tooltip("수련 완료(효과 확정) 버튼.")]
    [SerializeField] private Button claimButton;
    [Tooltip("완료 대기 안내 문구. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text readyText;

    /// <summary>이 슬롯의 버튼이 눌렸을 때(슬롯 번호를 함께 넘긴다). 실제 처리는 팝업이 한다.</summary>
    public event Action<int> UnlockRequested;
    public event Action<int> CancelRequested;
    public event Action<int> InstantRequested;
    public event Action<int> ClaimRequested;

    /// <summary>진행 중이던 수련의 시간이 다 됐을 때(뷰가 스스로 알아채는 유일한 변화).</summary>
    public event Action<int> Finished;

    private int _slotIndex;
    private State _state = State.Locked;
    private long _startUnix;
    private long _endUnix;

    public int SlotIndex => _slotIndex;
    public State Current => _state;

    private void Awake() {
        if (unlockButton != null) unlockButton.onClick.AddListener(() => UnlockRequested?.Invoke(_slotIndex));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => CancelRequested?.Invoke(_slotIndex));
        if (instantButton != null) instantButton.onClick.AddListener(() => InstantRequested?.Invoke(_slotIndex));
        if (claimButton != null) claimButton.onClick.AddListener(() => ClaimRequested?.Invoke(_slotIndex));
    }

    /// <summary>
    /// 비용 재화 아이콘을 배선한다(팝업이 진입 시 1회 호출). 슬롯이 쓰는 재화 종류는 고정이라
    /// 갱신마다 다시 넣을 필요가 없고, 아이콘 자체는 재화 카탈로그가 단일 소스라 프리팹에 스프라이트를 박지 않는다.
    /// </summary>
    public void Configure(CurrencyCatalog currencies) {
        CurrencyIcon.Apply(unlockCostIcon, currencies, CurrencyType.Diamond);
        CurrencyIcon.Apply(instantCostIcon, currencies, CurrencyType.TrainingTicket);
    }

    /// <summary>잠긴 슬롯으로 그린다. cost 가 0 이면 해금 버튼을 잠근다(더 열 수 없는 상태).</summary>
    public void BindLocked(int slotIndex, int cost, bool affordable) {
        _slotIndex = slotIndex;
        _state = State.Locked;
        if (unlockCostText != null) unlockCostText.text = cost > 0 ? cost.ToString("N0") : "-";
        if (unlockButton != null) unlockButton.interactable = cost > 0 && affordable;
        ApplyGroups();
    }

    /// <summary>비어 있는(수련을 시작할 수 있는) 슬롯으로 그린다.</summary>
    public void BindEmpty(int slotIndex) {
        _slotIndex = slotIndex;
        _state = State.Empty;
        if (illustrationView != null) illustrationView.Clear();
        ApplyGroups();
    }

    /// <summary>수련이 걸린 슬롯으로 그린다. 완료 시각이 이미 지났으면 완료 상태로 시작한다.</summary>
    public void BindRunning(int slotIndex, CharacterAppearance appearance, long startUnix, long endUnix,
                            int instantTicketCost, bool canInstant) {
        _slotIndex = slotIndex;
        _startUnix = startUnix;
        _endUnix = endUnix;

        var data = appearance.Character;
        if (illustrationView != null) illustrationView.Bind(appearance);
        if (characterNameText != null) characterNameText.text = data != null ? data.DisplayName : "";
        if (instantCostText != null) instantCostText.text = instantTicketCost.ToString();
        if (instantButton != null) instantButton.interactable = canInstant;

        _state = KstTime.NowUnix >= endUnix ? State.Ready : State.Running;
        ApplyGroups();
        RefreshRemaining();
    }

    // 남은 시간은 초 단위로 계속 줄어드는 값이라 프레임마다 갱신한다(문자열은 초가 바뀔 때만 달라진다).
    private void Update() {
        if (_state != State.Running) return;

        if (KstTime.NowUnix >= _endUnix) {
            _state = State.Ready;
            ApplyGroups();
            RefreshRemaining();
            Finished?.Invoke(_slotIndex);
            return;
        }
        RefreshRemaining();
    }

    private void RefreshRemaining() {
        long now = KstTime.NowUnix;
        if (remainText != null)
            remainText.text = KstTime.Countdown(_endUnix - now);
        if (progressSlider != null)
            progressSlider.value = TrainingRules.Progress01(_startUnix, _endUnix, now);
    }

    private void ApplyGroups() {
        if (lockedGroup != null) lockedGroup.SetActive(_state == State.Locked);
        if (emptyGroup != null) emptyGroup.SetActive(_state == State.Empty);
        if (runningGroup != null) runningGroup.SetActive(_state == State.Running);
        if (readyGroup != null) readyGroup.SetActive(_state == State.Ready);
        if (readyText != null && _state == State.Ready && string.IsNullOrEmpty(readyText.text))
            readyText.text = "수련 완료";
    }
}
