using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대항전 교전 기록 한 줄(누가 누구를 쳐서 이겼는지 / 졌는지).
///
/// 색으로 세 가지를 한눈에 구분한다.
///   이름  : 그 사람이 우리 길드인지 상대 길드인지(공격자/대상 어느 자리에 있든 소속을 따라간다)
///   결과  : 이 기록이 우리 길드에 유리한지
///   배경  : 내가 낀 기록인지 남들 기록인지
/// 판단에 필요한 것은 "우리 길드 id"와 "내 계정 id" 둘뿐이라 목록을 만드는 쪽이 그것만 넘겨 준다.
/// </summary>
public sealed class GuildWarLogSlot : MonoBehaviour {
    [Tooltip("공격한 사람 이름.")]
    [SerializeField] private TMP_Text attackerText;
    [Tooltip("공격당한 사람 이름.")]
    [SerializeField] private TMP_Text targetText;
    [Tooltip("결과 문구(격파 / 방어 성공).")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("선택. 기록 시각.")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("선택. 줄 배경. 내 기록과 남의 기록을 색으로 가른다.")]
    [SerializeField] private Image background;

    [Header("Name Colors (소속)")]
    [Tooltip("우리 길드원 이름 색.")]
    [SerializeField] private Color ourGuildColor = new Color(0.35f, 0.85f, 0.45f, 1f);
    [Tooltip("상대 길드원 이름 색.")]
    [SerializeField] private Color enemyGuildColor = new Color(0.95f, 0.45f, 0.40f, 1f);

    [Header("Result Colors (우리 길드 기준)")]
    [Tooltip("우리에게 유리한 기록(우리가 격파했거나 우리가 막아냈을 때) 결과 문구 색.")]
    [SerializeField] private Color goodColor = new Color(0.30f, 0.80f, 0.40f, 1f);
    [Tooltip("우리에게 불리한 기록 결과 문구 색.")]
    [SerializeField] private Color badColor = new Color(0.90f, 0.35f, 0.35f, 1f);

    [Header("Background Colors (내 기록 여부)")]
    [Tooltip("내가 공격했거나 내가 공격당한 기록의 배경색.")]
    [SerializeField] private Color myRecordColor = new Color(1f, 0.95f, 0.70f, 0.25f);
    [Tooltip("나와 무관한 기록의 배경색.")]
    [SerializeField] private Color otherRecordColor = new Color(1f, 1f, 1f, 0f);

    /// <param name="myGuildId">우리 길드 id. 이름 색과 결과 색의 기준이다.</param>
    /// <param name="myAccountId">내 계정 id. 배경색(내 기록 여부)의 기준이다.</param>
    public void Bind(GuildWarLogDto data, int myGuildId, int myAccountId) {
        if (data == null) return;

        // 이름은 자리(공격자/대상)가 아니라 소속을 따라 칠한다.
        if (attackerText != null) {
            attackerText.text = data.AttackerNickname;
            attackerText.color = data.AttackerGuildId == myGuildId ? ourGuildColor : enemyGuildColor;
        }
        if (targetText != null) {
            targetText.text = data.TargetNickname;
            targetText.color = data.TargetGuildId == myGuildId ? ourGuildColor : enemyGuildColor;
        }

        if (resultText != null) {
            // 공격자가 이겼으면 대상이 쓰러진 것이고, 졌으면 대상이 막아낸 것이다.
            // 우리가 공격해 이겼거나 우리가 방어에 성공했으면 우리에게 유리한 기록이다.
            bool weAttacked = data.AttackerGuildId == myGuildId;
            bool ours = weAttacked == data.AttackerWon;
            resultText.text = data.AttackerWon ? "격파" : "방어 성공";
            resultText.color = ours ? goodColor : badColor;
        }

        if (background != null) {
            bool mine = data.AttackerAccountId == myAccountId || data.TargetAccountId == myAccountId;
            background.color = mine ? myRecordColor : otherRecordColor;
        }

        if (timeText != null) timeText.text = KstTime.Format(data.CreatedUnix);
    }
}
