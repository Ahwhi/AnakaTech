using TMPro;
using UnityEngine;

/// <summary>
/// 활성화된 시너지 1줄(이름 + 효과). 편성 화면의 시너지 목록이 이 프리팹을 채워 쓴다.
///
/// 시너지 조건(어떤 캐릭터가 필요한지)은 일부러 그리지 않는다. 조건을 화면에 적어 두면 유저가 조합을
/// 찾아내는 재미가 사라지므로, 이미 맞춘 시너지의 이름과 효과만 보여 주는 것이 이 시스템의 설계다.
/// </summary>
public sealed class SynergyRowView : MonoBehaviour {
    [Tooltip("시너지 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("시너지 효과 요약. 비우면 이름만 표시한다.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("구성 인원 수 표시(예: 4인). 선택.")]
    [SerializeField] private TMP_Text memberCountText;

    public void Bind(SynergyData synergy) {
        if (synergy == null) return;

        if (nameText != null) nameText.text = synergy.DisplayName;
        if (effectText != null) {
            string summary = synergy.EffectSummary();
            effectText.gameObject.SetActive(!string.IsNullOrEmpty(summary));
            effectText.text = summary;
        }
        if (memberCountText != null) memberCountText.text = $"{synergy.MemberCount}인";
    }
}
