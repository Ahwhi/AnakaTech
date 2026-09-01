using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 정보 팝업(공용). "어떤 조건에서 무엇을 받는지"를 줄 단위로 보여 준다.
///
/// 컨텐츠를 전혀 모르는 순수 표시 화면이다. 제목/안내/줄 목록을 통째로 받아 그리기만 하므로
/// 길드 레이드(보스 레벨별 자정 보상), 대항전(승리/패배), 앞으로 붙일 쟁탈전까지 같은 프리팹 하나로 쓴다.
/// 새 컨텐츠가 이 팝업을 쓰려면 자기 규칙에서 Row 목록을 만들어 Show 에 넘기면 되고, 여기는 고칠 것이 없다.
///
/// 보상 칸은 공용 RewardSlotView + RewardVisualResolver 를 타므로 품목이 늘어도 아이콘/이름이 저절로 따라온다.
/// </summary>
public sealed class RewardInfoPopup : UIView {
    /// <summary>
    /// 보상 표의 한 줄. Label 은 조건(보스 레벨/승패 등), Rewards 는 그 조건에서 받는 보상,
    /// IsCurrent 는 지금 해당되는 줄인지(현재 레벨 표식)를 뜻한다.
    /// </summary>
    public readonly struct Row {
        public readonly string Label;
        public readonly IReadOnlyList<RewardItem> Rewards;
        public readonly bool IsCurrent;

        public Row(string label, IReadOnlyList<RewardItem> rewards, bool isCurrent = false) {
            Label = label;
            Rewards = rewards;
            IsCurrent = isCurrent;
        }
    }

    [Tooltip("상단 제목. 여는 쪽이 문구를 정한다(비우면 표시 안 함).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("상단 안내 문구(언제 지급되는지 등). 비우면 그룹째 숨긴다.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("안내 문구 묶음. 문구가 비었을 때 통째로 끄기 위한 것(비우면 descriptionText 만 끈다).")]
    [SerializeField] private GameObject descriptionGroup;

    [Tooltip("보상 줄이 생성될 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform content;
    [SerializeField] private RewardInfoRow rowPrefab;
    [Tooltip("줄이 하나도 없을 때 켜는 안내(선택).")]
    [SerializeField] private GameObject emptyLabel;
    [SerializeField] private Button closeButton;

    private readonly List<RewardInfoRow> _rows = new();

    // 현재 줄이 목록 어디쯤인지(0~1). 줄이 수십 개인 표(레이드 보스 단계)에서 그 자리로 스크롤을 맞추는 데 쓴다.
    private float _currentRatio = -1f;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>
    /// 보상 표를 그리고 팝업을 연다.
    /// </summary>
    /// <param name="hud">보상 아이콘/이름 해석에 쓰는 카탈로그 묶음.</param>
    /// <param name="title">상단 제목(예: 레이드 보상).</param>
    /// <param name="description">지급 시점 등 안내(비우면 숨김).</param>
    /// <param name="rows">조건별 보상 줄. 순서가 곧 표시 순서다.</param>
    public void Show(BattleHudContext hud, string title, string description, IReadOnlyList<Row> rows) {
        Clear();

        if (titleText != null) titleText.text = title ?? "";
        bool hasDescription = !string.IsNullOrEmpty(description);
        if (descriptionText != null) descriptionText.text = description ?? "";
        if (descriptionGroup != null) descriptionGroup.SetActive(hasDescription);
        else if (descriptionText != null) descriptionText.gameObject.SetActive(hasDescription);

        int count = rows != null ? rows.Count : 0;
        if (emptyLabel != null) emptyLabel.SetActive(count == 0);

        if (count > 0 && content != null && rowPrefab != null) {
            for (int i = 0; i < count; i++) {
                var r = rows[i];
                var row = Instantiate(rowPrefab, content);
                row.Bind(hud, r.Label, r.Rewards, r.IsCurrent);
                _rows.Add(row);

                // 현재 줄의 위치를 비율로 기억해 둔다(스크롤을 그 자리에 맞추기 위함).
                if (r.IsCurrent && count > 1) _currentRatio = (float)i / (count - 1);
            }
        }

        ShowAsync().Forget();
    }

    // 목록을 다 만든 뒤 현재 줄이 보이도록 스크롤을 옮긴다. 레이아웃이 확정된 다음에 옮겨야 정확하므로
    // 팝업이 열린 뒤(연출 종료 시점)에 한 번만 한다. 현재 줄이 없으면 맨 위 그대로 둔다.
    protected override void OnShown() {
        if (_currentRatio < 0f || content == null) return;

        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;

        Canvas.ForceUpdateCanvases();
        // 세로 스크롤은 1 이 맨 위라 비율을 뒤집는다.
        scroll.verticalNormalizedPosition = Mathf.Clamp01(1f - _currentRatio);
    }

    private void Clear() {
        foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
        _rows.Clear();
        _currentRatio = -1f;
    }

    protected override void OnClosed() => Clear();
}
