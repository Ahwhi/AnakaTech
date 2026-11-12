using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 증강 선택지 한 장. 종류/수치와 "이 칸의 새로고침이 남았는지"를 보여 주고, 고르거나 새로고침을 알린다.
/// 새로고침은 칸마다 따로이므로 버튼도 카드가 들고 있다.
/// </summary>
public sealed class DoomAugmentCard : MonoBehaviour {
    [Tooltip("증강 이름(예: 공격력). 종류별 색이 적용된다.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("효과 문구(예: 1.5배 / +7%). 이름과 같은 색을 쓴다.")]
    [SerializeField] private TMP_Text valueText;
    [Tooltip("선택. 종류별 색을 입힐 이미지들(테두리/배경 등).")]
    [SerializeField] private Graphic[] tintTargets;

    [Header("Buttons")]
    [Tooltip("이 증강을 고르는 버튼. 카드 전체에 붙이면 된다.")]
    [SerializeField] private Button selectButton;
    [Tooltip("이 칸만 다시 뽑는 버튼. 다 쓰면 꺼진다.")]
    [SerializeField] private Button rerollButton;
    [Tooltip("선택. 남은 새로고침 횟수 표시.")]
    [SerializeField] private TMP_Text rerollCountText;

    private Action<DoomAugmentCard> _onSelect;
    private Action<DoomAugmentCard> _onReroll;
    private bool _wired;

    /// <summary>이 카드가 들고 있는 증강 종류.</summary>
    public DoomAugmentKind Kind { get; private set; }

    /// <summary>굴려진 수치(배수 증강이면 1.5 같은 배수, 가산이면 더할 양).</summary>
    public float Value { get; private set; }

    public void Initialize(Action<DoomAugmentCard> onSelect, Action<DoomAugmentCard> onReroll) {
        _onSelect = onSelect;
        _onReroll = onReroll;

        if (_wired) return;
        if (selectButton != null) selectButton.onClick.AddListener(() => _onSelect?.Invoke(this));
        if (rerollButton != null) rerollButton.onClick.AddListener(() => _onReroll?.Invoke(this));
        _wired = true;
    }

    public void Bind(DoomAugmentKind kind, float value, int rerollLeft) {
        Kind = kind;
        Value = value;

        var def = DoomAugmentRules.Get(kind);
        if (nameText != null) { nameText.text = def.DisplayName; nameText.color = def.Color; }
        // 수치 표기는 규칙이 정한다. 카드가 따로 "+{값}%" 를 붙이면 회복/부활처럼 퍼센트가 아닌 것이 어긋난다.
        if (valueText != null) { valueText.text = DoomAugmentRules.DescribeValue(kind, value); valueText.color = def.Color; }
        if (tintTargets != null)
            foreach (var g in tintTargets)
                if (g != null) g.color = def.Color;

        SetRerollLeft(rerollLeft);
    }

    public void SetRerollLeft(int left) {
        if (rerollButton != null) rerollButton.gameObject.SetActive(left > 0);
        if (rerollCountText != null) {
            rerollCountText.gameObject.SetActive(left > 0);
            rerollCountText.text = left.ToString();
        }
    }

    /// <summary>고르는 동안 다른 카드가 눌리지 않게 잠근다(통신 없이 즉시 닫히지만 연타 방지).</summary>
    public void SetInteractable(bool on) {
        if (selectButton != null) selectButton.interactable = on;
        if (rerollButton != null) rerollButton.interactable = on;
    }
}
