using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>육성 팝업 스탯 페이지의 한 줄(스탯 이름 + 현재 투자값 + 투자 버튼).</summary>
public sealed class GrowthStatRow : MonoBehaviour {
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button plusButton;

    private Action _onPlus;

    public void Setup(string label, Action onPlus) {
        _onPlus = onPlus;
        if (labelText != null) labelText.text = label;
        if (plusButton != null) {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(() => _onPlus?.Invoke());
        }
    }

    /// <summary>
    /// 투자 버튼의 기본색을 바꾼다. 스탯마다 다른 색을 주어 줄이 여섯이어도 어느 스탯을 누르는지
    /// 글자를 읽지 않고 알아볼 수 있게 하려는 것이다.
    ///
    /// 나머지 상태색(눌림/비활성)은 프리팹이 정한 값을 그대로 둔다. 색을 바꾸는 목적이 "구분"이라
    /// 눌리는 느낌이나 잠긴 느낌까지 스탯마다 달라질 이유는 없다.
    /// </summary>
    public void SetPlusNormalColor(Color color) {
        if (plusButton == null) return;

        var colors = plusButton.colors;
        colors.normalColor = color;
        plusButton.colors = colors;
    }

    public void SetValue(int invested, bool canInvest) {
        if (valueText != null) valueText.text = invested.ToString();
        if (plusButton != null) plusButton.interactable = canInvest;
    }
}
