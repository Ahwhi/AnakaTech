using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 약관 동의 목록의 한 줄(체크 + 제목 + 상세 보기).
/// 표시와 입력 전달만 맡고, 필수 판정이나 항목 간 관계(선행 동의)는 팝업이 결정한다.
/// </summary>
public sealed class TermsItemView : MonoBehaviour {
    [Tooltip("동의 체크. 선행 동의가 필요한 항목은 팝업이 잠근다(interactable=false).")]
    [SerializeField] private Toggle agreeToggle;
    [Tooltip("(필수)/(선택) 표기가 붙은 한 줄 제목.")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("상세 보기 버튼. 누르면 약관 전문 팝업이 열린다.")]
    [SerializeField] private Button detailButton;
    [Tooltip("선행 동의가 필요해 잠긴 상태의 흐림 처리(선택). 없으면 체크만 잠긴다.")]
    [SerializeField] private CanvasGroup lockGroup;
    [SerializeField] private float lockedAlpha = 0.5f;

    private TermsItemDto _data;
    private Action<TermsItemView> _onChanged;

    public TermsItemDto Data => _data;
    public string Key => _data != null ? _data.Key : string.Empty;
    public bool IsAgreed => agreeToggle != null && agreeToggle.isOn;

    /// <summary>항목 데이터와 콜백을 연결한다. 목록에 생성된 직후 한 번 호출한다.</summary>
    public void Bind(TermsItemDto data, Action<TermsItemView> onChanged, Action<TermsItemDto> onDetail) {
        _data = data;
        _onChanged = onChanged;

        if (titleText != null) titleText.text = TermsRules.DisplayTitle(data);

        if (agreeToggle != null) {
            // 초기 상태 반영은 통지 없이 넣는다(구성 중에 팝업의 갱신 로직이 도는 것을 막는다).
            agreeToggle.SetIsOnWithoutNotify(data.IsAgreed);
            agreeToggle.onValueChanged.AddListener(_ => _onChanged?.Invoke(this));
        }

        if (detailButton != null) {
            // 문서 주소가 없는 항목(예: 만 14세 확인처럼 전문이 필요 없는 경우)은 상세 보기를 감춘다.
            detailButton.gameObject.SetActive(data.HasDocument);
            detailButton.onClick.AddListener(() => onDetail?.Invoke(_data));
        }
    }

    /// <summary>체크 상태를 통지 없이 바꾼다(전체 동의/선행 동의 해제에 따른 일괄 변경용).</summary>
    public void SetAgreedWithoutNotify(bool on) {
        if (agreeToggle != null) agreeToggle.SetIsOnWithoutNotify(on);
    }

    /// <summary>선행 동의 미충족 등으로 체크를 잠근다.</summary>
    public void SetSelectable(bool on) {
        if (agreeToggle != null) agreeToggle.interactable = on;
        if (lockGroup != null) lockGroup.alpha = on ? 1f : lockedAlpha;
    }
}
