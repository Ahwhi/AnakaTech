using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 오른쪽에서 슬라이드로 열리고 닫히는 메뉴 패널. 열기 버튼을 누르면 버튼들을 담은 패널이 화면 안으로
/// 슬라이드해 들어오고, 닫기 버튼(또는 배경 탭)으로 다시 밀려 나간다. 패널 안의 실제 메뉴 버튼들은
/// HudMenuButton 이 각자 팝업을 열므로, 이 컴포넌트는 "열림/닫힘 연출"만 책임진다(역할 분리).
///
/// 패널(panel)은 씬에서 비활성으로 두어도 된다. 열 때 자동으로 활성화하고, 닫힘 연출이 끝나면 다시
/// 비활성화한다. 열기 버튼(openButton)을 비워두면 이 컴포넌트가 붙은 오브젝트의 Button 을 자동으로 쓴다.
/// 즉 햄버거 버튼 오브젝트에 이 컴포넌트만 붙이고 panel 만 연결하면 동작한다.
/// </summary>
public sealed class SlideMenuPanel : HudInjectedBehaviour {
    [Header("Slide Panel")]
    [Tooltip("슬라이드되어 열릴 패널(메뉴 버튼들을 담는 컨테이너). 씬에서 비활성으로 둬도 된다. 편집 시엔 '열린 위치'에 놓을 것.")]
    [SerializeField] private RectTransform panel;
    [Tooltip("숨김 위치의 '열린 위치' 대비 오프셋. 오른쪽 밖으로 숨기려면 X 를 양수로(예: 600,0).")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(600f, 0f);
    [SerializeField] private float duration = 0.3f;

    [Header("Buttons")]
    [Tooltip("메뉴 열기 버튼. 비우면 이 컴포넌트가 붙은 오브젝트의 Button 을 사용한다.")]
    [SerializeField] private Button openButton;
    [Tooltip("메뉴 닫기 버튼(선택).")]
    [SerializeField] private Button closeButton;
    [Tooltip("패널 밖을 눌러 닫는 투명 배경(선택). 열릴 때만 활성화된다.")]
    [SerializeField] private Button backdrop;

    [Header("FX (선택)")]
    [Tooltip("패널에 CanvasGroup 을 두면 슬라이드와 함께 페이드한다.")]
    [SerializeField] private CanvasGroup canvasGroup;

    private Vector2 _shownPos;
    private bool _open;
    private Tween _tween;

    private void Awake() {
        // 열기 버튼 미할당 시 같은 오브젝트의 Button 을 사용(햄버거에 바로 붙이는 경우).
        if (openButton == null) openButton = GetComponent<Button>();

        if (panel != null) {
            _shownPos = panel.anchoredPosition;               // 편집 시 놓아둔 '열린 위치'를 홈으로 기억.
            panel.anchoredPosition = _shownPos + hiddenOffset; // 숨김 위치로 밀어둠(비활성이어도 좌표는 설정됨).
            panel.gameObject.SetActive(false);                 // 닫힘 상태에서 시작.
        }
        if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }
        if (backdrop != null) backdrop.gameObject.SetActive(false);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (backdrop != null) backdrop.onClick.AddListener(Close);
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!_open);

    private void SetOpen(bool open) {
        if (_open == open || panel == null) return;
        _open = open;
        Ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

        _tween?.Kill();

        if (open) {
            // 슬라이드가 보이려면 먼저 활성화해야 한다(비활성 오브젝트는 트윈해도 안 보임).
            panel.gameObject.SetActive(true);
            if (backdrop != null) backdrop.gameObject.SetActive(true);
        }

        var target = open ? _shownPos : _shownPos + hiddenOffset;
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(panel.DOAnchorPos(target, duration).SetEase(open ? Ease.OutCubic : Ease.InCubic));
        if (canvasGroup != null) seq.Join(canvasGroup.DOFade(open ? 1f : 0f, duration));
        seq.OnComplete(() => {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = open;
            if (!open) {
                if (backdrop != null) backdrop.gameObject.SetActive(false);
                panel.gameObject.SetActive(false);   // 닫힘 연출이 끝나면 다시 비활성화.
            }
        });
        _tween = seq;
    }

    private void OnDestroy() => _tween?.Kill();
}
