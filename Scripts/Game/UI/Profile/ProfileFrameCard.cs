using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로필 테두리 선택 그리드의 한 칸. 테두리 스프라이트 + 이름 + 미보유(잠금)/장착중 표시.
/// 테두리는 가운데가 비어 있는 액자라 칸 안에서도 그대로 그려 장착 모습을 미리 보여준다.
/// </summary>
public sealed class ProfileFrameCard : MonoBehaviour {
    [Tooltip("테두리 스프라이트.")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("미보유 시 노출할 잠금 오버레이(선택).")]
    [SerializeField] private GameObject lockedOverlay;
    [Tooltip("장착 중일 때 노출할 표시(선택).")]
    [SerializeField] private GameObject equippedIndicator;
    [SerializeField] private Button button;
    [Tooltip("선택. 기간제 테두리의 남은 시간(예: 1일 04:12:07 남음). 영구 테두리/미보유면 꺼진다.")]
    [SerializeField] private TMP_Text expiryText;
    [Tooltip("선택. 남은 기간 표시를 감싸는 부모(배경 등). 넣으면 텍스트 대신 이 그룹을 켜고 끈다.")]
    [SerializeField] private GameObject expiryGroup;

    public int ProfileFrameId { get; private set; }

    private long _expiresUnix;
    private IDisposable _tick;

    /// <summary>
    /// 기간제 테두리의 만료 시각(UTC epoch). 0 이하면 영구(표시를 끈다).
    /// 남은 시간은 "00:00:00 남음"(하루 이상이면 "1일 00:00:00 남음")으로 보이고, 칸이 켜져 있는 동안 1초마다 줄어든다.
    /// </summary>
    public void SetExpiry(long expiresUnix) {
        _expiresUnix = expiresUnix;
        RefreshExpiry();
        RestartTick();
    }

    private void OnEnable() => RestartTick();
    private void OnDisable() => StopTick();

    // 기간제일 때만 1초 타이머를 돌린다(영구 테두리 칸마다 타이머가 생기지 않게).
    private void RestartTick() {
        StopTick();
        if (_expiresUnix <= 0 || !isActiveAndEnabled) return;
        _tick = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => RefreshExpiry());
    }

    private void StopTick() {
        _tick?.Dispose();
        _tick = null;
    }

    private void RefreshExpiry() {
        long remain = _expiresUnix > 0 ? _expiresUnix - KstTime.NowUnix : 0;
        bool show = remain > 0;
        var toggle = expiryGroup != null ? expiryGroup : expiryText != null ? expiryText.gameObject : null;
        if (toggle != null) toggle.SetActive(show);
        if (expiryText != null) expiryText.text = show ? $"{KstTime.Countdown(remain)} 남음" : "";
        // 만료되면 더 셀 것이 없다(표시는 꺼졌다).
        if (!show) StopTick();
    }

    public void Bind(int profileFrameId, string displayName, Sprite sprite, bool owned, bool equipped, Action<ProfileFrameCard> onClick) {
        ProfileFrameId = profileFrameId;
        if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
        if (nameText != null) nameText.text = displayName;
        if (lockedOverlay != null) lockedOverlay.SetActive(!owned);
        if (equippedIndicator != null) equippedIndicator.SetActive(equipped);
        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }
}
