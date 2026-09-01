using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>상점 중분류 탭 버튼. 위치 고정 + 선택 표시(PLANNING 8).</summary>
public sealed class ShopSubTab : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private Button button;

    public string Sub { get; private set; }

    public void Bind(string sub, string display, Action<ShopSubTab> onClick) {
        Sub = sub;
        if (label != null) label.text = display;
        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
    }
}
