using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 버튼으로 내용을 접었다 펴는 공용 패널 컴포넌트.
///
/// 접기는 대상 루트의 세로 스케일을 0 으로 줄여 헤더 쪽으로 말려 들어가게 하고 함께 페이드한다.
/// 스케일은 레이아웃 리빌드를 일으키지 않아, 안쪽 내용이 자체 트윈을 돌리고 있어도 충돌하지 않는다
/// (높이 트윈은 매 프레임 재배치를 유발해 안쪽 연출과 싸운다).
///
/// 접힘 상태는 saveKey 를 주면 로컬 JSON(UserDataManager / GameData.json)에 기록되어 다음 실행에도 유지된다.
/// 키만 다르게 주면 패널이 몇 개로 늘어도 저장 코드를 고칠 필요가 없다.
///
/// 접히면 안쪽 내용을 비활성화하므로, 접혀 있는 동안 갱신을 멈추고 싶은 패널은 IsExpanded 를 보거나
/// ExpandedChanged 를 구독하면 된다.
/// </summary>
public sealed class FoldablePanel : MonoBehaviour {
    [Tooltip("접기/펴기 토글 버튼.")]
    [SerializeField] private Button toggleButton;
    [Tooltip("접을 대상(내용을 감싸는 루트). 피벗 y 를 1(위)로 두면 헤더 쪽으로 접힌다.")]
    [SerializeField] private RectTransform foldRoot;
    [Tooltip("접기 연출과 함께 페이드시킬 CanvasGroup(선택). 비우면 foldRoot 에서 탐색한다.")]
    [SerializeField] private CanvasGroup foldCanvasGroup;
    [Tooltip("접기/펴기 시간(초). 0 이면 즉시 전환.")]
    [SerializeField] private float foldDuration = 0.25f;
    [Tooltip("펴짐/접힘에 따라 회전시킬 화살표 아이콘(선택).")]
    [SerializeField] private RectTransform arrowIcon;
    [Tooltip("접혔을 때 화살표 Z 회전각(펴짐은 0).")]
    [SerializeField] private float arrowFoldedAngle = 180f;

    [Header("Persistence")]
    [Tooltip("접힘 상태를 로컬 JSON 에 기록할 때 쓰는 키(패널마다 고유해야 한다). " +
             "비우면 저장하지 않고 매번 defaultExpanded 로 시작한다.")]
    [SerializeField] private string saveKey;
    [Tooltip("저장된 값이 없을 때(첫 실행)의 초기 상태.")]
    [SerializeField] private bool defaultExpanded = true;

    private ISoundService _sound;
    private Tween _foldTween;
    private bool _expanded = true;

    /// <summary>현재 펼쳐져 있는지.</summary>
    public bool IsExpanded => _expanded;

    /// <summary>펼침/접힘이 바뀔 때 새 상태로 호출된다(연출 시작 시점).</summary>
    public event Action<bool> ExpandedChanged;

    private void Awake() {
        if (foldCanvasGroup == null && foldRoot != null) foldCanvasGroup = foldRoot.GetComponent<CanvasGroup>();
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);

        // 저장값은 부팅 단계(AppInitializeStep)에서 이미 로드돼 있다. 아직 없으면 기본값으로 시작한다.
        SetExpanded(LoadExpanded(), instant: true);
    }

    private void OnDestroy() => _foldTween?.Kill();

    /// <summary>토글 클릭음을 낼 사운드 서비스를 넣는다(선택). 넣지 않으면 무음으로 동작한다.</summary>
    public void Initialize(ISoundService sound) => _sound = sound;

    public void Expand() => SetExpanded(true, instant: false);
    public void Fold() => SetExpanded(false, instant: false);
    public void Toggle() => SetExpanded(!_expanded, instant: false);

    private bool LoadExpanded() =>
        UserDataManager.Data != null ? UserDataManager.Data.PanelExpanded(saveKey, defaultExpanded) : defaultExpanded;

    // instant=true 는 초기 복원용이다. 연출/효과음 없이 상태만 맞추고 저장도 건드리지 않는다
    // (복원값을 그대로 다시 쓰는 낭비를 막는다).
    private void SetExpanded(bool expanded, bool instant) {
        _expanded = expanded;
        if (!instant) {
            _sound?.PlaySfx(SfxKey.ButtonClick);
            Persist(expanded);
        }

        _foldTween?.Kill();
        ApplyArrow(expanded, instant);
        ExpandedChanged?.Invoke(expanded);

        if (foldRoot == null) return;
        if (expanded) foldRoot.gameObject.SetActive(true);   // 펼 때는 연출이 보이도록 먼저 켠다.

        float targetScale = expanded ? 1f : 0f;
        if (instant || foldDuration <= 0f) {
            foldRoot.localScale = new Vector3(1f, targetScale, 1f);
            if (foldCanvasGroup != null) foldCanvasGroup.alpha = expanded ? 1f : 0f;
            if (!expanded) foldRoot.gameObject.SetActive(false);
            return;
        }

        // 전투가 멈춘 상태(Time.timeScale=0)에서도 UI 는 접혀야 하므로 SetUpdate(true).
        var seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        seq.Join(foldRoot.DOScaleY(targetScale, foldDuration).SetEase(expanded ? Ease.OutBack : Ease.InCubic));
        if (foldCanvasGroup != null) seq.Join(foldCanvasGroup.DOFade(expanded ? 1f : 0f, foldDuration));
        seq.OnComplete(() => { if (!expanded) foldRoot.gameObject.SetActive(false); });
        _foldTween = seq;
    }

    private void ApplyArrow(bool expanded, bool instant) {
        if (arrowIcon == null) return;
        float angle = expanded ? 0f : arrowFoldedAngle;
        if (instant || foldDuration <= 0f) arrowIcon.localEulerAngles = new Vector3(0f, 0f, angle);
        else arrowIcon.DOLocalRotate(new Vector3(0f, 0f, angle), foldDuration).SetUpdate(true).SetLink(gameObject);
    }

    // 디스크 쓰기는 기존 SaveFlag/오토세이버 흐름에 맡긴다(토글할 때마다 파일을 쓰지 않는다).
    private void Persist(bool expanded) {
        if (string.IsNullOrEmpty(saveKey) || UserDataManager.Data == null) return;
        UserDataManager.Data.SetPanelExpanded(saveKey, expanded);
        UserDataManager.RequestSave();
    }
}
