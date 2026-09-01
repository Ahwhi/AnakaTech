using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 본부 트리에서 한 계열(훈련)의 머리말. 계열 이름과 그 계열의 진행도만 보여주는 순수 뷰다.
///
/// 계열이 몇 개이고 각각 어디에 놓이는지는 노드 데이터(HqNodeData.Line / GridPos)가 정하므로,
/// 트리 화면이 계열 수만큼 이 프리팹을 만들어 해당 계열 노드들 위에 배치한다.
/// 노드를 추가하거나 계열을 늘려도 이 프리팹은 손대지 않아도 된다.
///
/// 계열 구분은 색 하나로 한다. 프리팹이 하나뿐이라 색을 여기 직렬화해 두면
/// 인스펙터에서 세 계열의 색을 한 곳에서 조정할 수 있다(노드 상태 색과 같은 방식).
/// </summary>
public sealed class HqLineHeaderView : MonoBehaviour {
    [Header("Visuals")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("이 계열의 진행도(예: 5 / 40). 선택.")]
    [SerializeField] private TMP_Text progressText;
    [Tooltip("계열 색이 입혀지는 테두리/배경. 이름 텍스트와 같은 색으로 칠해진다. 선택.")]
    [SerializeField] private Image frame;

    [Header("Line Colors")]
    [SerializeField] private Color offenseColor = new Color(0.93f, 0.35f, 0.31f);
    [SerializeField] private Color guardColor = new Color(0.42f, 0.66f, 0.96f);
    [SerializeField] private Color finesseColor = new Color(0.35f, 0.85f, 0.95f);

    /// <summary>트리 화면이 배치에 쓰는 기준점.</summary>
    public RectTransform Rect => (RectTransform)transform;

    /// <param name="level">이 계열 노드 레벨의 합.</param>
    /// <param name="maxLevel">이 계열 노드 최대 레벨의 합.</param>
    public void Bind(HqNodeLine line, int level, int maxLevel) {
        var color = ColorFor(line);

        if (nameText != null) {
            nameText.text = HqNodeLines.DisplayName(line);
            nameText.color = color;
        }
        if (progressText != null) progressText.text = $"{level} / {maxLevel}";
        if (frame != null) frame.color = color;
    }

    private Color ColorFor(HqNodeLine line) => line switch {
        HqNodeLine.Offense => offenseColor,
        HqNodeLine.Guard => guardColor,
        HqNodeLine.Finesse => finesseColor,
        _ => Color.white
    };
}
