using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정대 기록 한 줄(누가 어느 장소의 몇 레벨에서 피해를 얼마나 줬는지, 그 판에 레벨이 올랐는지).
/// 배경색으로 내 기록과 다른 파티원 기록을 가른다.
/// </summary>
public sealed class ExpeditionLogSlot : MonoBehaviour {
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("장소 이름 + 도전 당시 레벨.")]
    [SerializeField] private TMP_Text locationText;
    [Tooltip("깎은 피해량.")]
    [SerializeField] private TMP_Text damageText;
    [Tooltip("이 판에 레벨이 올랐을 때 켜는 표시(레벨 상승 / 클리어).")]
    [SerializeField] private GameObject levelUpGroup;
    [Tooltip("선택. 레벨 상승 문구(레벨 상승 / 클리어).")]
    [SerializeField] private TMP_Text levelUpText;
    [Tooltip("선택. 기록 시각.")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("선택. 줄 배경. 내 기록과 남의 기록을 색으로 가른다.")]
    [SerializeField] private Image background;
    [SerializeField] private Color myRecordColor = new Color(1f, 0.95f, 0.70f, 0.25f);
    [SerializeField] private Color otherRecordColor = new Color(1f, 1f, 1f, 0f);

    public void Bind(ExpeditionLogEntryDto data, string locationName, int myAccountId) {
        if (data == null) return;

        if (nicknameText != null) nicknameText.text = data.Nickname;
        if (locationText != null) locationText.text = $"{locationName} Lv.{data.Level}";
        if (damageText != null) damageText.text = data.Damage.ToString("N0");

        if (levelUpGroup != null) levelUpGroup.SetActive(data.LevelUp);
        // 마지막 레벨을 깬 판이면 레벨이 오른 것이 아니라 클리어다.
        if (levelUpText != null) levelUpText.text = data.LevelUp && data.Level >= ExpeditionRules.MaxLevel ? "클리어" : "레벨 상승";

        if (timeText != null) timeText.text = KstTime.Format(data.CreatedUnix);
        if (background != null) background.color = data.AccountId == myAccountId ? myRecordColor : otherRecordColor;
    }
}
