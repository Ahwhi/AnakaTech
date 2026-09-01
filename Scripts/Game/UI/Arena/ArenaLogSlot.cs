using TMPro;
using UnityEngine;

/// <summary>
/// 쟁탈전 내역 한 줄. 내가 공격한 기록과 내가 방어자인 기록이 한 목록에 섞여 오므로,
/// 내 계정 기준으로 "공격/방어"와 "승/패"를 다시 계산해 보여 준다(기록 자체는 공격자 기준으로 저장된다).
/// </summary>
public sealed class ArenaLogSlot : MonoBehaviour {
    [Tooltip("공격/방어 구분 문구.")]
    [SerializeField] private TMP_Text sideText;
    [Tooltip("상대 닉네임.")]
    [SerializeField] private TMP_Text opponentText;
    [Tooltip("승/패 문구.")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("내 기준 점수 변동(예: +54 / -77).")]
    [SerializeField] private TMP_Text scoreDeltaText;
    [Tooltip("발생 시각.")]
    [SerializeField] private TMP_Text timeText;

    [Header("Colors")]
    [Tooltip("내가 공격한 기록의 구분 문구 색.")]
    [SerializeField] private Color attackColor = new Color(0.33f, 0.82f, 0.45f, 1f);
    [Tooltip("내가 방어한 기록의 구분 문구 색.")]
    [SerializeField] private Color defenseColor = new Color(0.36f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color winColor = new Color(1f, 0.84f, 0.30f, 1f);
    [SerializeField] private Color loseColor = new Color(0.62f, 0.64f, 0.70f, 1f);

    public void Bind(ArenaLogEntryDto entry, int myAccountId) {
        if (entry == null) return;

        bool attacker = entry.IsAttacker(myAccountId);
        bool win = entry.DidIWin(myAccountId);

        // 공격/방어는 색으로도 갈라 준다. 목록이 두 종류가 섞여 오므로 문구만으로는 훑어보기 어렵다.
        if (sideText != null) {
            sideText.text = attacker ? "공격" : "방어";
            sideText.color = attacker ? attackColor : defenseColor;
        }
        if (opponentText != null) opponentText.text = entry.OpponentName(myAccountId);
        if (resultText != null) {
            resultText.text = win ? "승리" : "패배";
            resultText.color = win ? winColor : loseColor;
        }

        // 점수는 양쪽이 주고받으므로 방어 기록에도 내 몫이 있다. 부호는 내 기준으로 뒤집어 보여 준다.
        // 색은 승패와 짝이라 결과 문구와 같은 색을 쓴다(오른 줄과 내린 줄이 한눈에 갈린다).
        if (scoreDeltaText != null) {
            scoreDeltaText.text = ArenaRules.ScoreDeltaText(entry.MyScoreDelta(myAccountId));
            scoreDeltaText.color = win ? winColor : loseColor;
        }

        if (timeText != null) timeText.text = KstTime.Format(entry.CreatedUnix);
    }
}
