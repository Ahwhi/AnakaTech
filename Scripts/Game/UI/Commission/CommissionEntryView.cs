using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의뢰 목록의 한 줄(레이드/토벌/호위). 세로 레이아웃 그룹 안에 하나씩 배치한다.
///
/// 이 줄은 "고르는 것"만 담당한다. 상세 정보와 [즉시해결][의뢰도전] 버튼은 목록 아래의 공용 상세 패널
/// (CommissionDetailPanel)이 하나만 갖는다. 줄마다 버튼을 달면 시선이 분산되고 단계가 늘수록 화면이 지저분해진다.
///
/// 아이콘/배경 같은 고정 아트는 코드가 다루지 않는다. 세 줄은 종류별로 하나씩 고정 배치되는 정적 UI라
/// 프리팹에 Image 를 두고 인스펙터에서 스프라이트를 지정하면 된다(단계에 따라 바뀌면 안 되는 값이다).
/// </summary>
public sealed class CommissionEntryView : MonoBehaviour {
    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("진행 상태 한 줄(예: 3단계 도전). 도전 단계가 곧 진행도라 클리어 기록을 따로 표시하지 않는다.")]
    [SerializeField] private TMP_Text stageText;

    [Header("Interaction")]
    [Tooltip("선택 표시 프레임.")]
    [SerializeField] private GameObject selectedFrame;
    [Tooltip("줄 전체를 덮는 선택 버튼.")]
    [SerializeField] private Button button;

    /// <summary>이 줄이 담당하는 의뢰 종류.</summary>
    public CommissionType Type { get; private set; }

    /// <param name="clearedStage">클리어한 최고 단계(0=없음).</param>
    /// <param name="challengeStage">이번에 도전할 단계.</param>
    /// <param name="hasNextStage">도전할 단계 데이터가 있는지(없으면 단계 표시를 안내 문구로 바꾼다).</param>
    /// <param name="onSelect">줄 클릭(선택) 통지.</param>
    public void Bind(CommissionType type, int clearedStage, int challengeStage, bool hasNextStage,
                     Action<CommissionEntryView> onSelect) {
        Type = type;

        if (nameText != null) nameText.text = CommissionRewardRules.TypeName(type);
        if (stageText != null) stageText.text = StageText(clearedStage, challengeStage, hasNextStage);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelect?.Invoke(this));
        }
    }

    /// <summary>
    /// 진행 상태 한 줄. 평소에는 도전 단계만 보여주면 진행도가 그대로 드러난다(도전 단계 = 클리어 단계 + 1).
    /// 더 도전할 단계가 없을 때만 "어디까지 깼는지"를 대신 보여줘 진행도가 사라지지 않게 한다.
    /// 상세 패널도 같은 규칙을 쓰므로 표시가 어긋나지 않는다.
    /// </summary>
    public static string StageText(int clearedStage, int challengeStage, bool hasNextStage) {
        if (hasNextStage) return $"{challengeStage}단계 도전";
        return clearedStage > 0 ? $"{clearedStage}단계 클리어" : "준비 중";
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
