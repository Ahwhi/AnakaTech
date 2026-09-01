using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 선택 목록의 항목 1개. 던전 이름 + 예상 보상(경험치/골드) + 출현 몬스터(일러스트 + 마리 수)를 표시하고,
/// 누르면 해당 dungeonIndex 로 콜백한다. 잠금/선택 상태는 오버레이로 표시한다.
///
/// 보상 수치는 서버 권위(RewardService)라 상위(DungeonSelectPopup)가 서버 미리보기를 받아 넘겨준다.
/// 응답 전이거나 실패하면 preview 가 null 로 들어오며, 이때 보상 슬롯은 숨긴다(수치를 지어내지 않는다).
/// 잠금 여부도 서버 권위(StartDungeon 이 최종 판정)라 여기 표시는 미러다.
/// </summary>
public sealed class DungeonSelectItem : MonoBehaviour {
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    [Header("State Overlay")]
    [Tooltip("현재 플레이 중인 던전일 때 켜지는 오버레이(선택 표시). 비워도 동작한다.")]
    [SerializeField] private GameObject selectedOverlay;
    [Tooltip("아직 해금되지 않은 던전일 때 켜지는 오버레이(자물쇠 등). 켜지면 버튼도 비활성화된다.")]
    [SerializeField] private GameObject lockOverlay;

    [Header("Reward Preview")]
    [Tooltip("경험치 보상 슬롯(아이콘 + 수량). 아이콘은 ShopIconCatalog 의 reward_exp 키에서 온다.")]
    [SerializeField] private RewardSlotView expSlot;
    [Tooltip("골드 보상 슬롯(아이콘 + 수량). 아이콘은 CurrencyCatalog 의 Gold 에서 온다.")]
    [SerializeField] private RewardSlotView goldSlot;

    [Header("Monsters")]
    [Tooltip("출현 몬스터 슬롯을 생성할 부모.")]
    [SerializeField] private Transform monsterRoot;
    [Tooltip("몬스터 1종을 표시할 슬롯 프리팹(일러스트 + 마리 수). 보상 슬롯과 같은 뷰를 재사용한다.")]
    [SerializeField] private RewardSlotView monsterSlotPrefab;

    private readonly List<GameObject> _monsterSlots = new();

    /// <summary>이 항목이 표시 중인 던전의 인덱스(보상 미리보기/상태를 나중에 매칭할 때 사용).</summary>
    public int DungeonIndex { get; private set; }

    public void Bind(MainHudContext hud, DungeonData dungeon, DungeonRewardPreview preview, Action<int> onClick) {
        if (dungeon == null) return;

        DungeonIndex = dungeon.DungeonIndex;
        if (nameText != null) nameText.text = dungeon.DisplayName;

        BindRewards(hud, preview);
        BindMonsters(dungeon);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(dungeon.DungeonIndex));
        }
    }

    /// <summary>잠금/선택 상태만 갱신한다(목록을 다시 만들지 않아 스크롤 위치가 유지된다).</summary>
    public void ApplyState(bool unlocked, bool selected) {
        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
        if (selectedOverlay != null) selectedOverlay.SetActive(selected);
        // 잠긴 던전은 눌러도 서버가 거부하므로 아예 누르지 못하게 한다.
        if (button != null) button.interactable = unlocked;
    }

    /// <summary>서버 미리보기가 뒤늦게 도착했을 때 보상 슬롯만 갱신한다(목록을 다시 만들지 않는다).</summary>
    public void ApplyRewardPreview(MainHudContext hud, DungeonRewardPreview preview) => BindRewards(hud, preview);

    private void BindRewards(MainHudContext hud, DungeonRewardPreview preview) {
        bool has = preview != null;
        if (expSlot != null) {
            expSlot.gameObject.SetActive(has);
            if (has) expSlot.Bind(RewardVisualResolver.Resolve(hud, RewardItem.Exp(preview.ExpPerCharacter)));
        }
        if (goldSlot != null) {
            goldSlot.gameObject.SetActive(has);
            if (has) goldSlot.Bind(RewardVisualResolver.Resolve(hud, RewardItem.Gold(preview.Gold)));
        }
    }

    private void BindMonsters(DungeonData dungeon) {
        ClearMonsters();
        if (monsterRoot == null || monsterSlotPrefab == null) return;

        var counts = dungeon.BuildMonsterCounts();
        for (int i = 0; i < counts.Count; i++) {
            var entry = counts[i];
            var slot = Instantiate(monsterSlotPrefab, monsterRoot);
            slot.Bind(new RewardSlotView.RewardVisual(entry.monster.Illustration, $"x{entry.weight}"));
            _monsterSlots.Add(slot.gameObject);
        }
    }

    private void ClearMonsters() {
        foreach (var go in _monsterSlots) if (go != null) Destroy(go);
        _monsterSlots.Clear();
    }

    private void OnDestroy() => ClearMonsters();
}
