using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로테이션 안내 팝업. 특수 뽑기의 픽업 대상이 지금 누구/무엇이고 다음에 무엇으로 바뀌는지,
/// 각 기간(KST)과 함께 캐릭터 트랙 -> 무기 트랙 순으로 보여준다.
///
/// 픽업 대상은 확률 표시 대상 정보에 해당하므로(특수 뽑기의 SSR 1% 가 누구인지가 바뀐다) 교체 예정을
/// 게임 안에서 미리 확인할 수 있어야 한다. 보여줄 개수는 서버 RotationConfig.PreviewCount 가 정한다
/// (현재=0, 다음=1). 서버 값만 올리면 이 목록도 그대로 길어진다.
/// </summary>
public sealed class RotationInfoPopup : UIView {
    [Tooltip("선택. 상단 제목.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("선택. 상단 안내 문구.")]
    [SerializeField] private TMP_Text noticeText;
    [Tooltip("로테이션 행을 생성할 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform content;
    [SerializeField] private RotationInfoRow rowPrefab;
    [Tooltip("선택. 표시할 픽업이 하나도 없을 때 노출할 오브젝트.")]
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private Button closeButton;

    private readonly List<GameObject> _rows = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    public void Show(RotationInfoDto info, CharacterDatabase charDb, EquipmentDatabase equipDb) {
        Clear();
        if (titleText != null && string.IsNullOrEmpty(titleText.text)) titleText.text = "픽업 로테이션";

        int shown = 0;
        if (info != null && content != null && rowPrefab != null) {
            // 캐릭터 트랙 먼저, 그 다음 무기 트랙. 각 트랙 안에서는 진행 중 -> 다음 순서.
            shown += BuildTrack(info.Track(isEquip: false), charDb, equipDb);
            shown += BuildTrack(info.Track(isEquip: true), charDb, equipDb);
        }

        if (emptyRoot != null) emptyRoot.SetActive(shown == 0);
        ShowAsync().Forget();
    }

    private int BuildTrack(List<RotationEntryDto> entries, CharacterDatabase charDb, EquipmentDatabase equipDb) {
        if (entries.Count == 0) return 0;

        // 순환열에 대상이 1종뿐이면 다음 주기에도 같은 대상이 온다. 그대로 "다음"으로 또 그리면 버그처럼 보이므로
        // 한 줄만 그리고 교체 예정이 없다는 것을 알린다(없는 교체를 예고하지 않으면서 사실도 숨기지 않는다).
        bool isFixed = true;
        for (int i = 1; i < entries.Count; i++)
            if (entries[i].Id != entries[0].Id) { isFixed = false; break; }

        int count = 0;
        foreach (var e in entries) {
            string itemName;
            IIllustrationSource art;
            if (e.IsEquip) {
                var d = equipDb != null ? equipDb.Get(e.Id) : null;
                itemName = d != null ? d.DisplayName : $"#{e.Id}";
                art = d;
            } else {
                var d = charDb != null ? charDb.Get(e.Id) : null;
                itemName = d != null ? d.DisplayName : $"#{e.Id}";
                art = d;
            }

            string status = isFixed ? "진행 중 (교체 예정 없음)" : (e.IsCurrent ? "진행 중" : "다음");
            var row = Instantiate(rowPrefab, content);
            row.Bind(status, e.IsCurrent, e.IsEquip ? "무기" : "소녀", itemName, (Grade)e.Grade,
                     KstTime.Period_Short(e.StartUnix, e.EndUnix), art);
            _rows.Add(row.gameObject);
            count++;

            if (isFixed) break;   // 고정 트랙은 현재 1줄만
        }
        return count;
    }

    private void Clear() {
        foreach (var r in _rows) if (r != null) Destroy(r);
        _rows.Clear();
    }

    protected override void OnClosed() => Clear();
}
