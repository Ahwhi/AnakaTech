using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 길드장 위임 대상 선택 팝업. 길드장이 관리 탭에서 열며, 넘길 길드원을 목록에서 고른다.
///
/// 고른 뒤 한 번 더 확인을 받는다. 위임은 되돌릴 수 없고(넘긴 순간 나는 평길드원이 된다) 되돌리려면
/// 새 길드장이 다시 넘겨 줘야 하므로, 잘못 눌러 일어나기에는 대가가 너무 크다.
///
/// 목록에는 나를 뺀 길드원만 담는다. 자기 자신에게 넘기는 것은 아무 일도 아니고, 서버도 거절한다.
/// </summary>
public sealed class GuildTransferMasterPopup : UIView {
    [Header("List")]
    [Tooltip("길드원 줄이 생성될 부모. 스크롤뷰 안에 둔다.")]
    [SerializeField] private Transform content;
    [Tooltip("길드원 줄 프리팹(GuildMemberSlot). 관리 탭과 같은 프리팹을 넣으면 표시가 저절로 같아진다.")]
    [SerializeField] private GuildMemberSlot memberSlotPrefab;
    [Tooltip("넘길 수 있는 길드원이 하나도 없을 때 켜는 안내 묶음. 선택.")]
    [SerializeField] private GameObject emptyGroup;
    [Tooltip("선택. 안내 문구(예: '길드장을 넘길 길드원을 선택하세요').")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private UniTaskCompletionSource<int> _result;
    private readonly List<GuildMemberSlot> _slots = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => Resolve(0));
    }

    /// <summary>
    /// 길드원 목록을 보여 주고 고른 사람을 돌려준다. 고르지 않고 닫으면 0.
    /// 확인 절차는 연 쪽(관리 탭)이 맡는다. 이 팝업은 "누구를 골랐는가"까지만 책임진다.
    /// </summary>
    public async UniTask<int> ShowAsync(BattleHudContext hud, IReadOnlyList<GuildMemberDto> members) {
        ClearSlots();

        if (content != null && memberSlotPrefab != null && members != null) {
            foreach (var member in members) {
                // 길드장(= 이 팝업을 연 나 자신)은 넘길 대상이 아니다. 길드장은 길드에 한 명뿐이라
                // 계정 id 를 따로 들고 오지 않아도 역할만으로 정확히 걸러진다.
                if (member == null || member.Role == GuildRole.Master) continue;

                var slot = Instantiate(memberSlotPrefab, content);
                slot.BindPick(member, hud, Resolve);
                _slots.Add(slot);
            }
        }

        if (emptyGroup != null) emptyGroup.SetActive(_slots.Count == 0);
        if (descriptionText != null) descriptionText.gameObject.SetActive(_slots.Count > 0);

        _result = new UniTaskCompletionSource<int>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void Resolve(int accountId) {
        _result?.TrySetResult(accountId);
        CloseAsync().Forget();
    }

    private void ClearSlots() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    // 고르지 않은 경로(배경 탭, 뒤로가기, 외부 CloseAllPopups)로 닫혀도 결과를 확정해
    // await 중인 호출부가 영원히 멈추지 않게 한다(고르지 않음 = 0).
    protected override void OnClosed() {
        _result?.TrySetResult(0);
        ClearSlots();
    }
}
