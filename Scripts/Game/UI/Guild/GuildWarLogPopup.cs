using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대항전 교전 기록 팝업. 진행 중인 매치에서 누가 누구를 쳤고 이겼는지를 최신순으로 보여 준다.
///
/// 참가자 행에는 최종 생사만 남아 "누가 쓰러뜨렸는지"를 알 수 없으므로 서버가 따로 쌓아 둔 기록을 받아 온다.
/// 표시 전용이라 여기서 바꾸는 값은 없고, 열 때 한 번만 조회한다(교전은 자주 일어나지 않는다).
/// </summary>
public sealed class GuildWarLogPopup : UIView {
    [Header("List")]
    [Tooltip("기록 줄이 생성될 부모.")]
    [SerializeField] private Transform logRoot;
    [SerializeField] private GuildWarLogSlot logSlotPrefab;
    [Tooltip("기록이 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyLabel;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [SerializeField] private Button closeButton;

    private readonly List<GuildWarLogSlot> _slots = new();
    private UniTaskCompletionSource _closed;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <param name="myGuildId">우리 길드 id. 이름 색과 결과 색의 기준이다.</param>
    /// <param name="myAccountId">내 계정 id. 내 기록을 배경색으로 가르는 기준이다.</param>
    public async UniTask ShowAsync(MainHudContext ctx, int myGuildId, int myAccountId) {
        Clear();
        if (loadingGroup != null) loadingGroup.SetActive(true);

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();

        try {
            var logs = await ctx.Guild.RequestWarLogsAsync(this.GetCancellationTokenOnDestroy());
            Build(logs, myGuildId, myAccountId);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ctx.Ui.ShowToast("교전 기록을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }

        await _closed.Task;
    }

    private void Build(List<GuildWarLogDto> logs, int myGuildId, int myAccountId) {
        Clear();
        if (emptyLabel != null) emptyLabel.SetActive(logs.Count == 0);
        if (logRoot == null || logSlotPrefab == null) return;

        // 색 판단(소속 / 유불리 / 내 기록)은 슬롯이 한다. 여기서는 기준값만 넘긴다.
        foreach (var log in logs) {
            var slot = Instantiate(logSlotPrefab, logRoot);
            slot.Bind(log, myGuildId, myAccountId);
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
