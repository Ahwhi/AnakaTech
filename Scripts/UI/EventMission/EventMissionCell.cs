using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이벤트 미션 1칸. 미션 설명(지표+임계치) + 진행도(현재/목표, 바) + 보상 아이콘/수량 + 수령 버튼을 표시한다.
/// 아이콘/수량은 페이지가 해석해 넣어준다(칸은 표시만, 디커플링). 수령은 콜백으로 페이지에 위임한다.
/// </summary>
public sealed class EventMissionCell : MonoBehaviour {
    [SerializeField] private TMP_Text titleText;      // "기력 100 소모" 등
    [SerializeField] private TMP_Text progressText;   // "50 / 100"
    [Tooltip("진행 비율 바(선택). MinValue=0, MaxValue=1, Interactable=off.")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardQtyText;
    [Tooltip("선택. 보상 아이콘 위 마우스오버 시 이름 툴팁을 띄우는 트리거. 비우면 툴팁 없음.")]
    [SerializeField] private TooltipTrigger rewardTooltip;
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimButtonText;
    [Tooltip("수령 완료 표시(체크/오버레이 등, 선택).")]
    [SerializeField] private GameObject claimedOverlay;

    [Header("Progress Colors")]
    [Tooltip("달성한 미션의 진행도 텍스트 색(수령 여부와 무관). 기본은 노란색.")]
    [SerializeField] private Color completedProgressColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Title Colors (리치 텍스트)")]
    [Tooltip("미션 문구에서 컨텐츠(상점 등)를 강조하는 색. 텍스트에 Rich Text 가 켜져 있어야 한다.")]
    [SerializeField] private string contentColorHex = "#39D353";
    [Tooltip("미션 문구에서 상품/재화 이름을 강조하는 색(상점의 다이아 상품명과 같은 색).")]
    [SerializeField] private string itemColorHex = "#7FD1FF";

    private int _missionId;
    private Action<int> _onClaim;
    private bool _wired;
    private Color _progressDefaultColor;   // 프리팹이 지정한 원래 색(달성 색에서 되돌릴 때 쓴다)
    private bool _capturedProgressColor;

    /// <summary>
    /// 미션 1칸을 채운다. title 을 넘기면 그 문구를 그대로 쓰고, 비우면 지표/임계치로 만든 기본 문구를 쓴다
    /// (스테이지 이름처럼 칸이 알 수 없는 데이터가 필요한 미션은 페이지가 문구를 만들어 넘긴다).
    /// characterNoun 은 기본 문구에서 캐릭터를 부르는 말이다(이벤트마다 바꿔 부를 수 있다). 비우면 "소녀".
    /// </summary>
    public void Bind(EventMission m, Sprite rewardSprite, string rewardQty, Action<int> onClaim,
                     string rewardName = null, string title = null, string characterNoun = null) {
        _missionId = m.Id;
        _onClaim = onClaim;

        if (titleText != null) titleText.text = string.IsNullOrEmpty(title) ? Label(m, characterNoun) : title;

        // 임계치가 1인 미션도 "0 / 1"로 보여준다. 표시할지 말지는 프리팹이 정한다(필요 없으면 슬롯을 비워 둔다).
        int shown = Mathf.Min(m.Progress, m.Threshold);
        if (progressText != null) {
            progressText.text = $"{shown:N0} / {m.Threshold:N0}";
            // 칸은 재사용되므로 달성/미달성 양방향으로 색을 확정한다(원래 색은 첫 바인딩 때 한 번만 기억한다).
            if (!_capturedProgressColor) { _progressDefaultColor = progressText.color; _capturedProgressColor = true; }
            progressText.color = m.Unlocked ? completedProgressColor : _progressDefaultColor;
        }
        if (progressBar != null) progressBar.value = m.Threshold > 0 ? Mathf.Clamp01((float)m.Progress / m.Threshold) : 0f;

        if (rewardIcon != null) {
            rewardIcon.sprite = rewardSprite;
            rewardIcon.enabled = rewardSprite != null;
        }
        if (rewardQtyText != null) rewardQtyText.text = rewardQty ?? "";
        if (rewardTooltip != null) rewardTooltip.SetContent(rewardName, null);

        if (claimedOverlay != null) claimedOverlay.SetActive(m.Claimed);

        if (claimButton != null) {
            if (!_wired) {
                claimButton.onClick.AddListener(() => _onClaim?.Invoke(_missionId));
                _wired = true;
            }
            claimButton.interactable = m.Claimable;
            claimButton.gameObject.SetActive(!m.Claimed);
        }
        if (claimButtonText != null)
            claimButtonText.text = m.Claimable ? "받기" : (m.Claimed ? "완료" : (m.Locked ? "잠김" : "진행중"));
    }

    // 지표 + 임계치로 미션 설명 문구를 만든다(라벨은 클라에서 구성, 서버는 지표/임계치만 내려줌).
    private string Label(EventMission m, string characterNoun) {
        string ch = string.IsNullOrEmpty(characterNoun) ? "소녀" : characterNoun;
        return (EventMissionMetric)m.Metric switch {
            EventMissionMetric.CharGacha => $"{ch} 모집 {m.Threshold:N0}회 진행",
            EventMissionMetric.EquipGacha => $"장비 모집 {m.Threshold:N0}회 진행",
            EventMissionMetric.StaminaSpent => $"기력 {m.Threshold:N0} 소모",
            EventMissionMetric.CharLevelUp => $"{ch} 레벨 상승 {m.Threshold:N0}회",
            EventMissionMetric.CharStarUpTotal => $"{ch} 성급업 {m.Threshold:N0}회",
            EventMissionMetric.GachaTotal => $"누적 모집 {m.Threshold:N0}회",
            EventMissionMetric.RaidClear => $"레이드 의뢰 {m.Threshold:N0}회 도전",
            EventMissionMetric.SubjugationClear => $"토벌 의뢰 {m.Threshold:N0}회 도전",
            EventMissionMetric.EscortClear => $"호위 의뢰 {m.Threshold:N0}회 도전",
            EventMissionMetric.CharacterCollected => $"소녀 {m.Threshold:N0}명 수집",
            // 스테이지 이름은 던전 데이터에 있으므로 페이지가 문구를 만들어 넘긴다. 여기 문구는 그러지 못했을 때의 폴백이다.
            EventMissionMetric.StageCleared => "스테이지 클리어",
            EventMissionMetric.DiaPackPurchased =>
                $"{Colored("상점", contentColorHex)}에서 {Colored($"[다이아 {m.Param:N0}]", itemColorHex)} 상품 구매",
            EventMissionMetric.EquipEnhanceTotal => $"장비 강화 {m.Threshold:N0}회",
            EventMissionMetric.EquipAwakenTotal => $"장비 각성 {m.Threshold:N0}회",
            EventMissionMetric.EquipGachaTotal => $"장비 모집 {m.Threshold:N0}회",
            EventMissionMetric.SessionMinutesToday => $"접속 {m.Threshold:N0}분 유지",
            // 핫타임은 시간대가 서버 설정이라 Param(시작/종료 시각)으로 문구를 만든다. 길이는 시간대마다 다르다.
            EventMissionMetric.HotTimeAttended =>
                $"{HourText(HotTimeMissionParam.StartOf(m.Param))} ~ {HourText(HotTimeMissionParam.EndOf(m.Param))} 사이에 접속",
            EventMissionMetric.CommissionStageCleared =>
                $"{CommissionName(CommissionMissionParam.TypeOf(m.Param))} 의뢰 {CommissionMissionParam.StageOf(m.Param):N0}단계 해결",
            // 출석형 이벤트. 임계치가 곧 일차라 "n일차"로 읽힌다(진행도는 지금까지 접속한 날 수).
            EventMissionMetric.EventAttendanceDays => $"{m.Threshold:N0}일차 출석",
            EventMissionMetric.MissionGroupCleared => "모든 미션 완료",
            EventMissionMetric.MissionSetCleared => $"전체 미션 {m.Threshold:N0}개 완료",
            EventMissionMetric.InvestmentUpgrade => $"투자 {m.Threshold:N0}회 하기",
            EventMissionMetric.DispatchSend => $"파견 {m.Threshold:N0}회 보내기",
            EventMissionMetric.GuildRaidFinish => $"길드 레이드 {m.Threshold:N0}회 하기",
            EventMissionMetric.ArenaFinish => $"쟁탈전 {m.Threshold:N0}회 하기",
            EventMissionMetric.DoomFinish => $"멸망전 {m.Threshold:N0}회 하기",
            EventMissionMetric.AttendanceClaim => "출석하기",
            EventMissionMetric.DailyMissionClaimed => $"일일 미션 {m.Threshold:N0}회 완료",
            EventMissionMetric.TowerFinish => $"무한 타워 {m.Threshold:N0}회 도전",
            EventMissionMetric.ExpeditionFinish => $"원정대 {m.Threshold:N0}회 도전",
            EventMissionMetric.HeroesEntry => $"영웅전 {m.Threshold:N0}회 도전",
            _ => ""
        };
    }

    // 24시간제 시각 -> "오후 8시" 형태. 시간대 문구를 쓰는 미션이 늘어도 표기가 한 곳에서 통일된다.
    private static string HourText(int hour24) {
        int h = ((hour24 % 24) + 24) % 24;
        string meridiem = h < 12 ? "오전" : "오후";
        int h12 = h % 12 == 0 ? 12 : h % 12;
        return $"{meridiem} {h12}시";
    }

    // 의뢰 종류 표시명. 값은 서버 CommissionType 순서(0=레이드,1=토벌,2=호위)와 같다.
    private static string CommissionName(int commissionType) => commissionType switch {
        0 => "레이드",
        1 => "토벌",
        2 => "호위",
        _ => "의뢰"
    };

    // 리치 텍스트 색 태그로 감싼다. 색이 비어 있으면 원문을 그대로 돌려주므로 색을 지정하지 않은 프리팹에서도 문구가 깨지지 않는다.
    private static string Colored(string text, string colorHex) {
        if (string.IsNullOrEmpty(colorHex)) return text;
        string hex = colorHex[0] == '#' ? colorHex : "#" + colorHex;
        return $"<color={hex}>{text}</color>";
    }
}
