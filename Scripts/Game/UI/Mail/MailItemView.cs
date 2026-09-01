using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 우편함 목록의 우편 1건. 제목/내용/남은 기간과 함께, 첨부된 보상들을 각각 "아이콘 + 수량" 슬롯으로 그대로 나열한다.
/// 보상 아이콘/수량 해석은 카탈로그를 가진 상위(MailboxPopup)가 resolver 로 넘겨주므로, 품목 종류(재화/캐릭터/장비/경험치 등)가
/// 늘어나도 이 뷰는 수정할 필요가 없다(확장은 resolver 만 손대면 됨).
/// </summary>
public sealed class MailItemView : MonoBehaviour {
    /// <summary>보상 1개의 표시 데이터(아이콘 + 수량 + 툴팁 이름). resolver 가 품목별로 채운다.</summary>
    public readonly struct RewardDisplay {
        public readonly Sprite Icon;
        public readonly string Quantity;
        public readonly string Name;   // 마우스오버 툴팁에 띄울 보상 이름(없으면 툴팁 없음).
        public RewardDisplay(Sprite icon, string quantity, string name = null) {
            Icon = icon; Quantity = quantity; Name = name;
        }
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("만료까지 남은 기간(예: 29일 남음).")]
    [SerializeField] private TMP_Text expiryText;

    [Header("Rewards")]
    [Tooltip("보상 슬롯(아이콘+수량)이 생성될 부모.")]
    [SerializeField] private Transform rewardSlotParent;
    [Tooltip("보상 1개를 표시하는 슬롯 프리팹.")]
    [SerializeField] private MailRewardView rewardSlotPrefab;

    [SerializeField] private Button claimButton;

    private readonly List<MailRewardView> _slots = new();

    public MailDto Mail { get; private set; }

    public void Bind(MailDto mail, Action<MailItemView> onClaim, Func<MailRewardDto, RewardDisplay> resolve) {
        Mail = mail;
        if (titleText != null) titleText.text = mail.Title;
        if (bodyText != null) bodyText.text = mail.Body;
        if (expiryText != null) expiryText.text = ExpiryText(mail.ExpiresAt);

        BuildRewardSlots(mail, resolve);

        if (claimButton != null) {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => onClaim?.Invoke(this));
        }
    }

    // 보상 개수만큼 아이콘+수량 슬롯을 생성한다. 각 슬롯의 아이콘/수량은 resolver 가 품목에 맞게 해석한다.
    private void BuildRewardSlots(MailDto mail, Func<MailRewardDto, RewardDisplay> resolve) {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        if (rewardSlotPrefab == null || rewardSlotParent == null || mail.Rewards == null) return;
        foreach (var r in mail.Rewards) {
            var display = resolve != null ? resolve(r) : new RewardDisplay(null, r.Value.ToString());
            var slot = Instantiate(rewardSlotPrefab, rewardSlotParent);
            slot.Bind(display.Icon, display.Quantity, display.Name);
            _slots.Add(slot);
        }
    }

    private static string ExpiryText(DateTime expires) {
        var remain = expires - DateTime.Now.AddSeconds(KstTime.DevOffsetSeconds);
        if (remain.TotalSeconds <= 0) return "만료됨";
        if (remain.TotalDays >= 1) return $"{(int)remain.TotalDays}일 남음";
        if (remain.TotalHours >= 1) return $"{(int)remain.TotalHours}시간 남음";
        return $"{Math.Max(1, (int)remain.TotalMinutes)}분 남음";
    }
}
