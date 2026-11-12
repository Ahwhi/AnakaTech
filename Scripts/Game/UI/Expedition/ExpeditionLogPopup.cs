using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정대 기록 팝업. 이번 주 파티에서 누가 어느 장소를 쳐서 피해를 얼마나 줬는지(레벨이 올랐는지)를 최신순으로 보여 준다.
/// 표시 전용이라 여기서 바꾸는 값은 없고, 열 때 한 번만 조회한다(대항전 기록 팝업과 같은 구조).
/// </summary>
public sealed class ExpeditionLogPopup : UIView {
    [Header("List")]
    [Tooltip("기록 줄이 생성될 부모.")]
    [SerializeField] private Transform logRoot;
    [SerializeField] private ExpeditionLogSlot logSlotPrefab;
    [Tooltip("기록이 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyLabel;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [SerializeField] private Button closeButton;

    private readonly List<ExpeditionLogSlot> _slots = new();
    private UniTaskCompletionSource _closed;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <param name="db">장소 이름 해석용. 비어 있으면 기본 이름으로 떨어진다.</param>
    public async UniTask ShowAsync(MainHudContext ctx, ExpeditionDatabase db) {
        Clear();
        if (loadingGroup != null) loadingGroup.SetActive(true);

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();

        try {
            var log = await ctx.Expedition.RequestLogAsync(this.GetCancellationTokenOnDestroy());
            Build(log, db);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ctx.Ui.ShowToast("원정대 기록을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }

        await _closed.Task;
    }

    private void Build(ExpeditionLogDto log, ExpeditionDatabase db) {
        Clear();
        if (emptyLabel != null) emptyLabel.SetActive(log.Entries.Count == 0);
        if (logRoot == null || logSlotPrefab == null) return;

        foreach (var entry in log.Entries) {
            var slot = Instantiate(logSlotPrefab, logRoot);
            string name = db != null ? db.NameOf(entry.Location) : entry.Location.ToString();
            slot.Bind(entry, name, log.MyAccountId);
            _slots.Add(slot);
        }
    }

    private void Clear() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        if (emptyLabel != null) emptyLabel.SetActive(false);
    }

    protected override void OnClosed() {
        Clear();
        _closed?.TrySetResult();
    }
}
