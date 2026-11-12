using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 투자 슬롯 한 칸. 열린 칸은 현재 효과/레벨/비용과 업그레이드 버튼을, 잠긴 칸은 해금 조건 문구를 보여 준다.
///
/// 무엇을 할지는 콜백을 받은 쪽(InvestmentPopup)이 정하고, 이 컴포넌트는 표시와 입력 전달만 한다.
/// 레벨 색은 구간별로 인스펙터에서 조정한다(수치가 아니라 취향이라 코드에 박지 않는다).
/// </summary>
public sealed class InvestmentSlotView : MonoBehaviour {
    [Header("Unlocked")]
    [Tooltip("열린 칸에서 켜는 묶음(효과/레벨/버튼). 잠기면 꺼진다.")]
    [SerializeField] private GameObject unlockedGroup;
    [Tooltip("슬롯 이름(예: 스테이지 경험치).")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("슬롯 설명(무엇이 오르는지).")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("현재 효과 표시(예: +5%). 0 레벨이면 +0%.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("현재 레벨 표시(예: 5 / 100).")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("업그레이드 비용(100 레벨까지 골드, 이후 다이아). 최대 레벨이면 묶음이 꺼진다.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("비용 아이콘. 재화 카탈로그에서 자동 배선되며, 다이아 구간에 들어서면 다이아 아이콘으로 바뀐다. 선택.")]
    [SerializeField] private Image costIcon;
    [Tooltip("비용 묶음. 최대 레벨에서 꺼진다.")]
    [SerializeField] private GameObject costGroup;
    [SerializeField] private Button upgradeButton;
    [Tooltip("최대 레벨일 때만 켜는 묶음(MAX 표시 등). 선택.")]
    [SerializeField] private GameObject maxGroup;

    [Header("Locked")]
    [Tooltip("잠긴 칸에서 켜는 묶음. 열리면 꺼진다.")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("해금 조건 문구(예: 캐릭터 별레벨 300에서 슬롯 개방).")]
    [SerializeField] private TMP_Text lockedText;

    [Header("Level Color")]
    [Tooltip("0 레벨(아직 효과 없음).")]
    [SerializeField] private Color level0Color = new(0.6f, 0.6f, 0.6f, 1f);
    [Tooltip("1~9 레벨.")]
    [SerializeField] private Color level1Color = Color.white;
    [Tooltip("10~49 레벨.")]
    [SerializeField] private Color level10Color = new(1f, 0.85f, 0.25f, 1f);
    [Tooltip("50~99 레벨.")]
    [SerializeField] private Color level50Color = new(0.35f, 0.9f, 0.4f, 1f);
    [Tooltip("100~149 레벨(골드 구간 완료, 다이아 구간 시작).")]
    [SerializeField] private Color level100Color = new(0.35f, 0.9f, 0.95f, 1f);
    [Tooltip("150~199 레벨.")]
    [SerializeField] private Color level150Color = new(0.75f, 0.55f, 1f, 1f);
    [Tooltip("최대(200) 레벨.")]
    [SerializeField] private Color levelMaxColor = new(1f, 0.45f, 0.75f, 1f);

    private const int NotBound = -1;

    private int _slotIndex;
    private int _boundLevel = NotBound;
    private Action<int> _onUpgrade;
    private ButtonHoldRepeater _hold;
    private bool _wired;

    /// <summary>
    /// 슬롯 한 칸을 그린다. 잠긴 칸이면 해금 조건만 보여 주고 조작부는 전부 감춘다.
    /// </summary>
    /// <param name="gold">보유 골드. 골드 구간에서 비용을 감당할 수 있는지로 버튼 활성을 정한다.</param>
    /// <param name="diamond">보유 다이아. 다이아 구간에서 비용을 감당할 수 있는지로 버튼 활성을 정한다.</param>
    public void Bind(int slotIndex, int level, bool unlocked, int gold, int diamond, BattleHudContext hud, Action<int> onUpgrade) {
        // 다른 슬롯을 그리게 되면(재사용) 이전 레벨 기록은 의미가 없다.
        if (slotIndex != _slotIndex) _boundLevel = NotBound;
        _slotIndex = slotIndex;
        _onUpgrade = onUpgrade;

        StopHoldAtDiamondBoundary(level);
        _boundLevel = level;

        var slot = InvestmentRules.Get(slotIndex);
        if (slot == null) return;

        if (unlockedGroup != null) unlockedGroup.SetActive(unlocked);
        if (lockedGroup != null) lockedGroup.SetActive(!unlocked);

        if (!unlocked) {
            if (lockedText != null) lockedText.text = InvestmentRules.UnlockLabel(slotIndex);
            return;
        }

        if (nameText != null) nameText.text = slot.DisplayName;
        if (descriptionText != null) descriptionText.text = slot.Description;

        var color = ColorFor(level);
        if (effectText != null) {
            effectText.text = InvestmentRules.EffectLabel(slotIndex, level);
            effectText.color = color;
        }
        if (levelText != null) {
            levelText.text = $"{level} / {InvestmentRules.DisplayMaxLevel(level)}";
            levelText.color = color;
        }

        bool max = level >= InvestmentRules.MaxLevel;
        int cost = InvestmentRules.UpgradeCost(level);
        var currency = InvestmentRules.UpgradeCurrency(level);
        int owned = currency == CurrencyType.Diamond ? diamond : gold;

        if (maxGroup != null) maxGroup.SetActive(max);
        if (costGroup != null) costGroup.SetActive(!max);
        if (costText != null) costText.text = cost.ToString("N0");
        CurrencyIcon.Apply(costIcon, hud != null ? hud.Currencies : null, currency);

        if (upgradeButton != null) {
            upgradeButton.gameObject.SetActive(!max);
            upgradeButton.interactable = !max && owned >= cost;
            if (!_wired) {
                _wired = true;
                upgradeButton.onClick.AddListener(() => _onUpgrade?.Invoke(_slotIndex));
            }
        }
    }

    // 안전장치: 골드 구간 마지막 레벨업으로 다이아 구간에 막 들어섰다면, 꾹 누르고 있던 연타를 여기서 끊는다.
    // 연타가 이어지면 바로 다음 발동부터 다이아가 빠져나가므로, 손을 뗐다 다시 눌러야만 다이아를 쓰게 한다.
    // 서버도 화면이 본 레벨(expectedLevel)과 다르면 거부하므로, 겹친 요청이 다이아 구간에서 처리되지도 않는다.
    private void StopHoldAtDiamondBoundary(int level) {
        if (_boundLevel == NotBound) return;
        bool crossed = InvestmentRules.UpgradeCurrency(_boundLevel) != CurrencyType.Diamond &&
                       InvestmentRules.UpgradeCurrency(level) == CurrencyType.Diamond;
        if (!crossed || upgradeButton == null) return;

        if (_hold == null) _hold = upgradeButton.GetComponent<ButtonHoldRepeater>();
        if (_hold != null) _hold.Interrupt();
    }

    // 레벨 구간별 색. 경계는 기획이 정한 값이고 색만 인스펙터에서 조정한다.
    private Color ColorFor(int level) {
        if (level >= InvestmentRules.MaxLevel) return levelMaxColor;
        if (level >= 150) return level150Color;
        if (level >= InvestmentRules.GoldMaxLevel) return level100Color;
        if (level >= 50) return level50Color;
        if (level >= 10) return level10Color;
        if (level >= 1) return level1Color;
        return level0Color;
    }
}
