using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 육성 팝업(GrowthPopup)의 한계돌파 페이지. 조건 충족 현황(한계돌파권/레벨/별레벨)과 돌파 전후 스킬을 보여 주고,
/// 한계돌파 요청까지 이 패널이 맡는다. GrowthPopup 은 페이지 전환과 배지만 알면 된다.
///
/// 조건 텍스트는 "현재 / 필요" 형태이며 충족하면 초록, 미충족이면 빨강이다(승급 페이지와 같은 규칙).
/// 한계돌파 후에는 돌파 전 그룹을 끄고 완료 그룹을 켜며, 남는 조각만큼의 보너스 스탯을 보여 준다.
/// 판정은 서버 권위이고, 여기 판정(LimitBreakRules)은 버튼/색 표시용 미러다.
/// </summary>
public sealed class LimitBreakPanel : MonoBehaviour {
    [Header("Requirements")]
    [Tooltip("한계돌파권 아이콘. 재화 카탈로그(CurrencyCatalog)에서 자동으로 찾아 표시한다.")]
    [SerializeField] private Image ticketIcon;
    [Tooltip("선택: 한계돌파권 재화 위젯(아이콘을 누르면 설명 팝업). CurrencyPanel 밖에 두므로 이 패널이 초기화한다.")]
    [SerializeField] private CurrencyView ticketView;
    [Tooltip("한계돌파권 '보유 / 필요'.")]
    [SerializeField] private TMP_Text ticketCountText;
    [Tooltip("레벨 '현재 / 필요'.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("별레벨 '현재 / 필요'.")]
    [SerializeField] private TMP_Text starText;
    [Tooltip("조건을 채웠을 때 글자색.")]
    [SerializeField] private Color metColor = new(0.25f, 0.8f, 0.35f);
    [Tooltip("조건을 채우지 못했을 때 글자색.")]
    [SerializeField] private Color unmetColor = Color.red;

    [Header("Skill - Before (한계돌파 전)")]
    [Tooltip("한계돌파 전 스킬 이름.")]
    [FormerlySerializedAs("skillNameText")]
    [SerializeField] private TMP_Text currentSkillNameText;
    [Tooltip("한계돌파 전 스킬 아이콘.")]
    [FormerlySerializedAs("skillIcon")]
    [SerializeField] private Image currentSkillIcon;
    [Tooltip("한계돌파 전 스킬 설명. Rich Text 가 켜져 있어야 설명 색이 적용된다.")]
    [SerializeField] private TMP_Text currentSkillDescText;

    [Header("Skill - After (한계돌파 후)")]
    [Tooltip("한계돌파 후 스킬 이름. 한계돌파 스킬을 할당하지 않은 캐릭터는 기존 스킬이 그대로 나온다(돌파해도 스킬이 바뀌지 않으므로). 아래 둘도 같다.")]
    [SerializeField] private TMP_Text limitBreakSkillNameText;
    [Tooltip("한계돌파 후 스킬 아이콘.")]
    [SerializeField] private Image limitBreakSkillIcon;
    [Tooltip("한계돌파 후 스킬 설명. Rich Text 가 켜져 있어야 설명 색이 적용된다.")]
    [SerializeField] private TMP_Text limitBreakSkillDescText;

    [Header("Action")]
    [SerializeField] private Button limitBreakButton;

    [Header("State Groups")]
    [Tooltip("한계돌파 전에만 켜는 그룹(조건/스킬 비교/버튼 등). 한계돌파하면 꺼진다.")]
    [SerializeField] private GameObject unlimitGroup;
    [Tooltip("한계돌파한 캐릭터일 때만 켜는 그룹. 조각 보너스 표시 등을 둔다.")]
    [SerializeField] private GameObject completedGroup;
    [Tooltip("한계돌파 후 조각 보너스(예: '최종데미지 +1.97%\\n피격데미지감소 +1.97%'). 조각 1개당 0.01%.")]
    [SerializeField] private TMP_Text shardBonusText;
    [Tooltip("조각 보너스 제목(ShardBonusTitle) 색. 아래 줄의 보너스 내용은 텍스트 기본색 그대로 둔다. Rich Text 가 켜져 있어야 한다.")]
    [SerializeField] private Color shardBonusTitleColor = Color.yellow;
    [Tooltip("완료 그룹의 조각 이름(캐릭터명 + ' 조각'). 별 페이지와 같은 형식이며 색은 바꾸지 않는다.")]
    [SerializeField] private TMP_Text shardNameText;
    [Tooltip("완료 그룹의 조각 일러스트(별 페이지의 ShardIllust 와 같은 방식). 현재 캐릭터 일러스트를 표시. 비우면 표시 안 함.")]
    [SerializeField] private CharacterIllustrationView shardIllustrationView;
    [Tooltip("완료 그룹의 보유 조각 수(숫자만). 보너스 계산에 쓰이는 수량과 같다.")]
    [SerializeField] private TMP_Text shardCountText;

    /// <summary>한계돌파에 성공했을 때 발생. 호스트(GrowthPopup)가 다른 페이지/배지를 다시 그리는 데 쓴다.</summary>
    public event Action LimitBroken;

    private MainHudContext _ctx;
    private int _charId;
    private CancellationToken _ct;
    private bool _busy;
    private bool _wired;

    /// <summary>표시할 캐릭터를 정한다. 같은 패널을 캐릭터만 바꿔 다시 쓰므로, 리스너와 구독은 처음 한 번만 건다.</summary>
    public void Initialize(MainHudContext ctx, int charDataId) {
        _ctx = ctx;
        _charId = charDataId;

        if (!_wired) {
            _ct = this.GetCancellationTokenOnDestroy();
            if (limitBreakButton != null)
                limitBreakButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); LimitBreakAsync().Forget(); });
            // 한계돌파권은 이 패널 밖에서도 바뀐다(상점 구매/우편 수령). 보유량과 버튼이 바로 따라오게 한다.
            _ctx.Store.LimitBreakTicket.Subscribe(_ => { if (isActiveAndEnabled) Refresh(); }).AddTo(this);
            _wired = true;
        }

        CurrencyIcon.Apply(ticketIcon, _ctx.Currencies, CurrencyType.LimitBreakTicket);
        // CurrencyView 는 리스너를 지우고 다시 거므로 캐릭터를 바꿔 다시 불러도 중복되지 않는다.
        if (ticketView != null) ticketView.Initialize(_ctx);
        RefreshSkill();
        RefreshIllustration();
        Refresh();
    }

    /// <summary>지금 이 캐릭터를 한계돌파할 수 있는지. 호스트의 페이지 버튼 배지가 같은 판정을 쓴다.</summary>
    public bool CanLimitBreak =>
        _ctx != null && LimitBreakRules.CanLimitBreak(_ctx.Store.GetProgress(_charId), _ctx.Store.LimitBreakTicket.Value);

    /// <summary>조건 현황과 버튼 상태를 다시 그린다.</summary>
    public void Refresh() {
        if (_ctx == null) return;
        var p = _ctx.Store.GetProgress(_charId);
        if (p == null) return;

        int tickets = _ctx.Store.LimitBreakTicket.Value;
        SetRequirement(ticketCountText, $"{tickets:N0} / {LimitBreakRules.TicketCost:N0}", LimitBreakRules.IsTicketMet(tickets));
        SetRequirement(levelText, $"{p.Level:N0} / {LimitBreakRules.RequiredLevel:N0}", LimitBreakRules.IsLevelMet(p));
        SetRequirement(starText, $"{p.StarLevel} / {LimitBreakRules.RequiredStar}", LimitBreakRules.IsStarMet(p));

        if (limitBreakButton != null) limitBreakButton.interactable = !_busy && LimitBreakRules.CanLimitBreak(p, tickets);

        // 돌파 전에는 조건/버튼 그룹, 돌파 후에는 완료 그룹(조각 보너스)만 보인다.
        if (unlimitGroup != null) unlimitGroup.SetActive(!p.LimitBroken);
        if (completedGroup != null) completedGroup.SetActive(p.LimitBroken);
        if (shardBonusText != null) shardBonusText.text = p.LimitBroken ? ShardBonusDisplay(p) : string.Empty;
        if (shardCountText != null) shardCountText.text = p.Shards.ToString("N0");
    }

    // 제목 줄(색 지정) + 보너스 내용(텍스트 기본색). 내용 줄은 규칙(LimitBreakRules)이 만든 그대로 쓴다.
    private string ShardBonusDisplay(CharacterProgress p) {
        string summary = LimitBreakRules.ShardBonusSummary(p);
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(shardBonusTitleColor)}>{ShardBonusTitle}</color>\n{summary}";
    }

    private const string ShardBonusTitle = "스킬 강화 완료";

    // 완료 그룹의 조각 일러스트와 조각 이름. 캐릭터가 바뀔 때만 달라진다.
    private void RefreshIllustration() {
        var appearance = _ctx.Appearance.Mine(_charId);
        var data = appearance.Character;
        if (shardIllustrationView != null) {
            if (data != null) shardIllustrationView.Bind(appearance);
            else shardIllustrationView.Clear();
        }
        if (shardNameText != null) shardNameText.text = data != null ? $"{data.DisplayName} 조각" : string.Empty;
    }

    private void SetRequirement(TMP_Text text, string value, bool met) {
        if (text == null) return;
        text.text = value;
        text.color = met ? metColor : unmetColor;
    }

    // 돌파 전/후 스킬(이름/아이콘/설명). 캐릭터가 바뀔 때만 달라진다.
    private void RefreshSkill() {
        var data = _ctx.CharDb.Get(_charId);
        ApplySkill(data != null ? data.SkillFor(false) : null, currentSkillNameText, currentSkillIcon, currentSkillDescText);
        ApplySkill(data != null ? data.SkillFor(true) : null, limitBreakSkillNameText, limitBreakSkillIcon, limitBreakSkillDescText);
    }

    // 스킬 하나를 이름/아이콘/설명 칸에 그린다. 전/후 양쪽이 같은 규칙을 쓰도록 한 곳에 둔다.
    private static void ApplySkill(UnitSkill skill, TMP_Text nameText, Image icon, TMP_Text descText) {
        if (nameText != null) nameText.text = skill != null ? skill.SkillName : "";
        if (icon != null) {
            icon.sprite = skill != null ? skill.SkillIcon : null;
            icon.enabled = skill != null && skill.SkillIcon != null;
        }
        if (descText != null) descText.text = skill != null ? SkillTextFormat.Colorize(skill.Description) : "";
    }

    private async UniTaskVoid LimitBreakAsync() {
        if (_busy || !CanLimitBreak) return;
        _busy = true;
        Refresh();   // 응답을 기다리는 동안 버튼을 막는다(연타로 요청이 겹치지 않게).
        try {
            var res = await _ctx.MainData.LimitBreakAsync(_charId, _ct);
            if (res.IsSuccess) {
                _ctx.Store.LimitBreakTicket.Value = res.NewLimitBreakTicket;   // 소모 후 서버 확정 보유량
                _ctx.Store.ApplyCharacterSnapshot(res.Updated);
                _ctx.Sound.PlaySfx(SfxKey.StarUp);
                LimitBroken?.Invoke();
            } else {
                _ctx.Ui.ShowToast(LimitBreakRules.FailMessage(res.FailReason));
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("한계돌파에 실패했습니다.");
        } finally {
            _busy = false;
            if (this != null) Refresh();
        }
    }
}
