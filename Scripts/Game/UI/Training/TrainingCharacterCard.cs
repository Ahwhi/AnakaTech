using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 수련 팝업의 소녀 카드 1장. 초상/이름/레벨/등급 같은 공통 표시는 덱 카드(DeckCharacterCard)를 그대로 재사용하고,
/// 이 컴포넌트는 수련 화면에만 필요한 표시(수련 중 오버레이 / 보유 수련 효과)만 얹는다.
///
/// 덱 카드 스크립트에 수련 전용 필드를 넣지 않는 이유: 덱 카드는 여러 화면이 쓰는 공통 부품이라
/// 한 화면의 사정이 들어가면 계속 불어난다. 수련용 프리팹은 덱 카드 프리팹을 복제해 오버레이 오브젝트를
/// 추가하고 이 컴포넌트를 붙이면 되며, 덱 프리팹은 손대지 않아도 된다.
/// </summary>
public sealed class TrainingCharacterCard : MonoBehaviour {
    [Tooltip("같은 오브젝트(또는 자식)의 덱 카드. 초상/이름/레벨/등급/별 표시를 담당한다.")]
    [SerializeField] private DeckCharacterCard card;

    [Header("Training Overlay")]
    [Tooltip("수련 중일 때 카드 위에 덮는 그룹(딤 + 안내). 수련 중이 아니면 꺼진다.")]
    [SerializeField] private GameObject busyOverlay;
    [Tooltip("수련 중 상태 문구. 남은 시간 또는 완료 안내가 들어간다. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text busyText;

    [Header("Training Result")]
    [Tooltip("이미 수련 효과를 가진 소녀에 표시할 그룹(효과 뱃지 등). 없으면 꺼진다.")]
    [SerializeField] private GameObject resultGroup;
    [Tooltip("보유 중인 수련 효과 문구. 능력치 이름과 수치가 두 줄로 들어간다(예: 사거리 / +2.3). " +
             "TMP 의 Rich Text 를 켜 두어야 아래 색이 적용되고, 두 줄이 들어갈 높이여야 한다.")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("능력치 이름 줄(사거리, 피격 데미지 감소 등)의 색.")]
    [SerializeField] private Color effectNameColor = Color.white;
    [Tooltip("수치 줄(+2.3, +30% 등)의 색.")]
    [SerializeField] private Color effectValueColor = Color.white;

    [Header("Selection")]
    [Tooltip("현재 선택된 카드 표시. 덱 카드의 선택 프레임과 별개로 두고 싶을 때만 배선한다(비워도 됨).")]
    [SerializeField] private GameObject selectedFrame;

    private CharacterData _data;

    public CharacterData Data => _data;

    /// <summary>카드의 정적 표시(초상/이름/등급)를 채운다. 수련 상태는 Refresh 로 따로 갱신한다.</summary>
    public void Bind(CharacterAppearance appearance, CharacterProgress progress, Action<TrainingCharacterCard> onClick) {
        _data = appearance.Character;
        if (card != null) card.Bind(appearance, progress, _ => onClick?.Invoke(this));
    }

    /// <summary>성장치(레벨/별/경험치)만 제자리 갱신한다(재생성 없이 실시간 반영용).</summary>
    public void RefreshProgress(CharacterProgress progress) {
        if (card != null) card.RefreshProgress(progress);
    }

    /// <summary>
    /// 수련 상태 표시. 수련 중인 소녀는 목록에서 빼지 않고 오버레이로 덮는다.
    /// 목록에서 빼면 카드 위치가 매번 바뀌어 "내 소녀가 어디 갔는지" 알 수 없게 되기 때문이다.
    ///
    /// 수련 중에는 보유 효과 표시를 감춘다. 재수련 중인 소녀는 지금 걸린 수련이 끝나면 기존 효과가
    /// 대체되므로, 두 표시를 함께 두면 "지금 가진 효과"와 "곧 사라질 효과"가 한 카드에 겹쳐 보인다.
    /// </summary>
    /// <param name="busyText">수련 중 안내 문구(빈 문자열이면 수련 중이 아님).</param>
    /// <param name="effectName">보유 중인 수련 효과의 능력치 이름(빈 문자열이면 효과 없음).</param>
    /// <param name="effectValue">그 효과의 수치 문구.</param>
    public void SetTrainingState(string busyText, string effectName, string effectValue, bool selected,
                                 bool inDeck = false, bool isRepresentative = false) {
        bool busy = !string.IsNullOrEmpty(busyText);
        if (busyOverlay != null) busyOverlay.SetActive(busy);
        if (this.busyText != null) this.busyText.text = busyText;

        // 수련 중이면 효과 표시는 접는다(오버레이 하나만 보인다).
        bool showEffect = !busy && !string.IsNullOrEmpty(effectName);
        if (resultGroup != null) resultGroup.SetActive(showEffect);
        if (resultText != null && showEffect) resultText.text = EffectMarkup(effectName, effectValue);

        if (selectedFrame != null) selectedFrame.SetActive(selected);
        // 목록 정렬이 대표/편성 순이므로 그 표식도 함께 넘겨 카드 순서의 근거가 화면에 드러나게 한다.
        if (card != null) card.SetState(inDeck, isRepresentative, selected);
    }

    /// <summary>
    /// 능력치 이름과 수치를 두 줄로 나누고 줄마다 색을 입힌다.
    ///
    /// 카드가 좁아 한 줄로 두면 "피격 데미지 감소 +12%" 같은 긴 효과가 잘리거나 글자가 작아진다.
    /// 색을 나누는 것도 같은 이유다. 줄만 나누면 두 줄이 한 덩어리로 보여 무엇이 능력치이고
    /// 무엇이 수치인지 한눈에 갈리지 않는다.
    /// </summary>
    private string EffectMarkup(string effectName, string effectValue) {
        string name = $"<color=#{ColorUtility.ToHtmlStringRGBA(effectNameColor)}>{effectName}</color>";
        if (string.IsNullOrEmpty(effectValue)) return name;

        return $"{name}\n<color=#{ColorUtility.ToHtmlStringRGBA(effectValueColor)}>{effectValue}</color>";
    }
}
