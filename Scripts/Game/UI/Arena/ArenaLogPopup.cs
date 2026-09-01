using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 쟁탈전 내역 팝업. 내가 공격한 기록과 내가 방어자인 기록을 최신순으로 함께 보여 준다.
/// 목록은 서버가 만들어 주므로 여기서는 정렬하지 않고 받은 순서대로 그린다(대항전 교전 기록과 같은 구조).
/// </summary>
public sealed class ArenaLogPopup : UIView {
    [Tooltip("내역 줄이 생성될 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform content;
    [SerializeField] private ArenaLogSlot slotPrefab;
    [Tooltip("기록이 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyLabel;
    [Tooltip("불러오는 동안 켜는 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private Button closeButton;

    private readonly List<ArenaLogSlot> _slots = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>내역을 받아 그리고 팝업을 연다. 내 계정 id 는 응답에 함께 실려 오므로 호출부가 넘기지 않는다.</summary>
    public async UniTask ShowAsync(MainHudContext ctx) {
        Clear();
        if (emptyLabel != null) emptyLabel.SetActive(false);
        if (loadingGroup != null) loadingGroup.SetActive(true);

        await base.ShowAsync();

        var ct = this.GetCancellationTokenOnDestroy();
        try {
            var log = await ctx.Arena.RequestLogAsync(ct);
            Build(log.Entries, log.MyAccountId);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ctx.Ui.ShowToast("쟁탈전 내역을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    private void Build(List<ArenaLogEntryDto> entries, int myAccountId) {
        Clear();
        int count = entries != null ? entries.Count : 0;
        if (emptyLabel != null) emptyLabel.SetActive(count == 0);
        if (count == 0 || content == null || slotPrefab == null) return;

        foreach (var e in entries) {
            var slot = Instantiate(slotPrefab, content);
            slot.Bind(e, myAccountId);
            _slots.Add(slot);
        }
    }

    private void Clear() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    protected override void OnClosed() => Clear();
}
