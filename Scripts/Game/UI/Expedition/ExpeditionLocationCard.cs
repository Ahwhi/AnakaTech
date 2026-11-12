using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정대 장소 카드 1장(썸네일 + 이름 + 현재 레벨 + 진행 슬라이더 + 체력 + 점유 표시).
///
/// 진행 슬라이더는 레벨 단위다. 최댓값이 ExpeditionRules.ClearedLevel(8) 이고 값이 현재 레벨이라,
/// 7레벨까지 깨서 클리어하면 꽉 찬다. 현재 레벨 안의 체력은 따로 체력 슬라이더/문구로 보여 준다.
/// 누를 수 있는지(오늘 이미 침 / 클리어 / 남이 치는 중)는 팝업이 정해 넘겨 준다.
/// </summary>
public sealed class ExpeditionLocationCard : MonoBehaviour {
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("현재 레벨(Lv.n / 클리어).")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("레벨 진행 슬라이더(최댓값 = 클리어 레벨 8).")]
    [SerializeField] private Slider levelSlider;

    [Header("Level Color (선택)")]
    [Tooltip("레벨에 따라 색을 바꿀 이미지.")]
    [SerializeField] private Image levelImage;
    [Tooltip("레벨별 색. 0~6 = Lv.1~7, 7 = 클리어. 칸이 모자라면 마지막 색을 쓴다.")]
    [SerializeField] private Color[] levelColors = {
        Color.white, Color.white, Color.white, Color.white,
        Color.white, Color.white, Color.white, Color.white
    };

    [Header("HP (선택)")]
    [Tooltip("현재 레벨의 남은 체력 비율.")]
    [SerializeField] private Slider hpSlider;
    [Tooltip("현재 레벨의 남은 체력 비율(n.nn%).")]
    [SerializeField] private TMP_Text hpText;

    [Header("State (선택)")]
    [Tooltip("클리어했을 때 켜는 표시.")]
    [SerializeField] private GameObject clearedGroup;
    [Tooltip("다른 파티원이 치는 중일 때 켜는 표시.")]
    [SerializeField] private GameObject busyGroup;
    [Tooltip("치는 중인 파티원 표시 문구.")]
    [SerializeField] private TMP_Text busyText;
    [Tooltip("지금 입장할 수 없을 때 카드 위를 덮는 표시(오늘 이미 참여 등).")]
    [SerializeField] private GameObject lockedGroup;

    [SerializeField] private Button button;

    private ExpeditionLocation _location;

    public void Bind(ExpeditionLocationDto data, string displayName, Sprite thumbnail, bool interactable,
                     Action<ExpeditionLocation> onClick) {
        if (data == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);
        _location = data.Location;

        if (thumbnailImage != null) {
            thumbnailImage.sprite = thumbnail;
            thumbnailImage.enabled = thumbnail != null;
        }
        if (nameText != null) nameText.text = displayName;
        if (levelText != null) levelText.text = ExpeditionRules.LevelText(data.Level);
        if (levelSlider != null) {
            levelSlider.minValue = 0f;
            levelSlider.maxValue = ExpeditionRules.ClearedLevel;
            levelSlider.SetValueWithoutNotify(Mathf.Clamp(data.Level, 0, ExpeditionRules.ClearedLevel));
        }
        if (levelImage != null && levelColors != null && levelColors.Length > 0)
            levelImage.color = levelColors[Mathf.Clamp(data.Level - 1, 0, levelColors.Length - 1)];

        bool cleared = data.IsCleared;
        if (hpSlider != null) {
            hpSlider.gameObject.SetActive(!cleared);
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.SetValueWithoutNotify(data.HpRatio);
        }
        if (hpText != null) {
            hpText.gameObject.SetActive(!cleared);
            hpText.text = $"{data.HpRatio * 100f:F2}%";
        }

        if (clearedGroup != null) clearedGroup.SetActive(cleared);
        if (busyGroup != null) busyGroup.SetActive(data.IsBusy);
        if (busyText != null) busyText.text = data.IsBusy ? $"{data.BusyNickname} 공략 중" : "";
        if (lockedGroup != null) lockedGroup.SetActive(!interactable && !cleared && !data.IsBusy);

        if (button != null) {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(_location));
        }
    }
}
