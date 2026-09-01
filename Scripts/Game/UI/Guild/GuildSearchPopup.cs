using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 길드 검색 팝업. 이름으로 찾아 가입/신청한다. 결과 카드는 추천 목록과 같은 GuildCard 를 쓴다.
/// 가입/신청이 성사되면 true 를 돌려주어 부모(GuildPopup)가 화면을 다시 받아 오게 한다.
/// </summary>
public sealed class GuildSearchPopup : UIView {
    [Header("Search")]
    [SerializeField] private TMP_InputField keywordInput;
    [SerializeField] private Button searchButton;

    [Header("Result")]
    [SerializeField] private Transform resultRoot;
    [SerializeField] private GuildCard guildCardPrefab;
    [Tooltip("검색 결과가 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyLabel;
    [Tooltip("검색 중 표시할 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;

    [SerializeField] private Button closeButton;

    private readonly List<GuildCard> _cards = new();

    private MainHudContext _ctx;
    private GuildPopup _owner;
    private GuildHomeDto _home;
    private UniTaskCompletionSource<bool> _result;
    private bool _changed;   // 이 팝업에서 가입/신청 상태를 바꿨는지
    private bool _busy;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => Resolve());
        if (searchButton != null) searchButton.onClick.AddListener(() => SearchAsync().Forget());
        if (keywordInput != null) keywordInput.onSubmit.AddListener(_ => SearchAsync().Forget());
    }

    /// <param name="home">내 소속/신청 상태. 카드 버튼이 가입/신청/취소 중 무엇이 될지 판단하는 근거.</param>
    /// <returns>가입/신청 상태가 바뀌었으면 true.</returns>
    public async UniTask<bool> ShowAsync(MainHudContext ctx, GuildPopup owner, GuildHomeDto home) {
        _ctx = ctx;
        _owner = owner;
        _home = home;
        _changed = false;

        if (keywordInput != null) keywordInput.SetTextWithoutNotify("");
        ClearCards();
        if (emptyLabel != null) emptyLabel.SetActive(false);

        _result = new UniTaskCompletionSource<bool>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private async UniTaskVoid SearchAsync() {
        if (_busy) return;
        _busy = true;
        if (loadingGroup != null) loadingGroup.SetActive(true);

        try {
            _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
            string keyword = keywordInput != null ? keywordInput.text.Trim() : "";
            var list = await _ctx.Guild.RequestListAsync(search: true, keyword, this.GetCancellationTokenOnDestroy());
            Build(list);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("검색에 실패했습니다.");
        } finally {
            _busy = false;
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    private void Build(List<GuildSummaryDto> list) {
        ClearCards();
        if (resultRoot == null || guildCardPrefab == null) return;

        var marks = _owner != null ? _owner.Marks : null;
        foreach (var g in list) {
            var card = Instantiate(guildCardPrefab, resultRoot);
            card.Bind(g, marks, _home != null && _home.AppliedGuildIds.Contains(g.GuildId), OnCardAction);
            _cards.Add(card);
        }
        if (emptyLabel != null) emptyLabel.SetActive(list.Count == 0);
    }

    // 가입/신청/신청취소. 성사되면 검색 결과의 버튼 상태가 달라지므로 목록을 다시 그린다.
    private void OnCardAction(GuildSummaryDto guild) {
        if (guild == null || _busy) return;
        ActAsync(guild).Forget();
    }

    private async UniTaskVoid ActAsync(GuildSummaryDto guild) {
        _busy = true;
        try {
            _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
            bool applied = _home != null && _home.AppliedGuildIds.Contains(guild.GuildId);
            var action = GuildRules.JoinActionOf(guild, applied);

            long power = _owner != null ? _owner.MyCombatPower() : 0;
            var res = await _ctx.Guild.ActAsync(action, guildId: guild.GuildId, combatPower: power,
                                                ct: this.GetCancellationTokenOnDestroy());
            if (!res.IsSuccess) { _ctx.Ui.ShowToast(GuildRules.FailMessage(res.FailReason)); return; }

            _changed = true;
            // 즉시 가입은 이 팝업에 더 있을 이유가 없다(이미 길드원이 됐다).
            if (action == GuildActionType.Join) { Resolve(); return; }

            // 신청/취소는 내 신청 목록만 바뀌므로 화면에서 바로 반영한다.
            if (_home != null) {
                if (action == GuildActionType.Apply) _home.AppliedGuildIds.Add(guild.GuildId);
                else _home.AppliedGuildIds.Remove(guild.GuildId);
            }
            RefreshCards();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("요청을 처리하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    private void RefreshCards() {
        var marks = _owner != null ? _owner.Marks : null;
        foreach (var card in _cards) {
            if (card == null || card.Data == null) continue;
            card.Bind(card.Data, marks, _home != null && _home.AppliedGuildIds.Contains(card.Data.GuildId), OnCardAction);
        }
    }

    private void ClearCards() {
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    private void Resolve() {
        _result?.TrySetResult(_changed);
        CloseAsync().Forget();
    }

    protected override void OnClosed() {
        _result?.TrySetResult(_changed);
        ClearCards();
    }
}
