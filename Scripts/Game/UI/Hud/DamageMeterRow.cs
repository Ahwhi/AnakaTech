using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 딜량 그래프의 행 1개(초상화 + 딜량 막대 + 비율%). 형태와 자기 연출만 담당하고
/// 순위 계산/집계는 전혀 모른다(패널이 넘겨주는 값과 목표 위치만 반영한다).
///
/// 연출은 세 가지다:
///  - 순위 변동: 목표 y 로 트윈 이동(MoveTo). 같은 시간으로 움직이므로 자리를 맞바꾸는 두 행이 서로 스쳐 지나간다.
///  - 수치 변동: 막대/퍼센트를 한 트윈으로 함께 보간해 숫자와 막대가 어긋나지 않는다.
///  - 등장/퇴장: 알파 페이드(파티 재구성 시).
/// </summary>
public sealed class DamageMeterRow : MonoBehaviour {
    [Tooltip("초상화 슬롯(마스크 + CharacterIllustrationView).")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("캐릭터 이름(선택).")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("딜량 막대(MinValue=0, MaxValue=1, Interactable=off). 1위가 가득 차는 상대 길이.")]
    [SerializeField] private Slider damageSlider;
    [Tooltip("전체 딜량 대비 비율. 예: 32.5%")]
    [SerializeField] private TMP_Text percentText;
    [Tooltip("누적 딜량 수치(선택). 비워도 된다.")]
    [SerializeField] private TMP_Text damageText;
    [Tooltip("등장/퇴장 페이드용. 비우면 자동으로 추가된다.")]
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform _rt;
    private Tween _moveTween;
    private Tween _valueTween;
    private Tween _fadeTween;

    // 현재 표시 중인 값(트윈 시작점). 목표값과 같으면 트윈을 새로 만들지 않는다.
    private float _shownTopRatio;
    private float _shownShare;
    private float _shownDamage;
    private int _rankIndex = -1;

    /// <summary>이 행이 표시 중인 캐릭터 id(미바인드면 -1).</summary>
    public int CharId { get; private set; } = -1;

    public RectTransform Rect => _rt != null ? _rt : (_rt = (RectTransform)transform);

    private void Awake() {
        _rt = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>캐릭터를 이 행에 바인딩한다(런 시작/파티 재구성 시 1회). 수치는 0 에서 시작한다.</summary>
    /// <param name="appearance">전투 유닛이 들고 있는 외형(코스튬을 끼고 있으면 그 일러스트).</param>
    public void Bind(CharacterAppearance appearance) {
        var data = appearance.Character;
        CharId = data != null ? data.Id : -1;
        if (illustrationView != null) illustrationView.Bind(appearance);
        if (nameText != null) nameText.text = data != null ? data.DisplayName : "";

        KillTweens();
        _shownTopRatio = 0f;
        _shownShare = 0f;
        _shownDamage = 0f;
        _rankIndex = -1;
        ApplyValueUI(0f, 0f, 0f);
        // 이어지는 Show(true, ...) 가 페이드로 들어오도록 투명에서 시작한다(런 시작 시 목록이 부드럽게 뜬다).
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    /// <summary>행을 비활성 상태로 되돌린다(풀 반환 시).</summary>
    public void Clear() {
        KillTweens();
        CharId = -1;
        _rankIndex = -1;
        if (illustrationView != null) illustrationView.Clear();
    }

    /// <summary>순위 위치로 이동시킨다. 이미 그 순위면 아무 것도 하지 않아 트윈이 끊기지 않는다.
    /// instant 면 트윈 없이 즉시 배치한다(첫 배치/펼치기 직후).</summary>
    public void MoveTo(int rankIndex, float y, float duration, Ease ease, bool instant) {
        if (_rankIndex == rankIndex && !instant) return;
        _rankIndex = rankIndex;

        _moveTween?.Kill();
        var pos = Rect.anchoredPosition;
        if (instant || duration <= 0f) {
            Rect.anchoredPosition = new Vector2(pos.x, y);
            return;
        }
        _moveTween = Rect.DOAnchorPosY(y, duration).SetEase(ease).SetUpdate(true);
    }

    /// <summary>막대/퍼센트/딜량 표시를 목표값으로 부드럽게 보간한다. 세 값을 한 트윈으로 묶어 서로 어긋나지 않는다.</summary>
    public void SetValues(float topRatio, float share, float damage, float duration) {
        bool same = Mathf.Approximately(_shownTopRatio, topRatio)
                 && Mathf.Approximately(_shownShare, share)
                 && Mathf.Approximately(_shownDamage, damage);
        if (same) return;

        _valueTween?.Kill();

        if (duration <= 0f) {
            _shownTopRatio = topRatio; _shownShare = share; _shownDamage = damage;
            ApplyValueUI(topRatio, share, damage);
            return;
        }

        float fromTop = _shownTopRatio, fromShare = _shownShare, fromDamage = _shownDamage;
        _valueTween = DOVirtual.Float(0f, 1f, duration, t => {
            _shownTopRatio = Mathf.Lerp(fromTop, topRatio, t);
            _shownShare = Mathf.Lerp(fromShare, share, t);
            _shownDamage = Mathf.Lerp(fromDamage, damage, t);
            ApplyValueUI(_shownTopRatio, _shownShare, _shownDamage);
        }).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    /// <summary>행 등장/퇴장 페이드. 퇴장이 끝나면 오브젝트를 비활성화한다.</summary>
    public void Show(bool visible, float duration) {
        // 이미 꺼져 있는 행은 트윈이 돌지 않아(비활성 오브젝트) 완료 콜백도 오지 않으므로 그대로 둔다.
        if (!visible && !gameObject.activeSelf) return;

        _fadeTween?.Kill();
        if (visible && !gameObject.activeSelf) gameObject.SetActive(true);

        if (duration <= 0f) {
            canvasGroup.alpha = visible ? 1f : 0f;
            if (!visible) gameObject.SetActive(false);
            return;
        }

        _fadeTween = canvasGroup.DOFade(visible ? 1f : 0f, duration).SetUpdate(true);
        if (!visible) _fadeTween.OnComplete(() => gameObject.SetActive(false));
    }

    private void ApplyValueUI(float topRatio, float share, float damage) {
        if (damageSlider != null) damageSlider.value = Mathf.Clamp01(topRatio);
        if (percentText != null) percentText.text = $"{share * 100f:0.0}%";
        if (damageText != null) damageText.text = Mathf.RoundToInt(damage).ToString("N0");
    }

    private void KillTweens() {
        _moveTween?.Kill();
        _valueTween?.Kill();
        _fadeTween?.Kill();
        _moveTween = _valueTween = _fadeTween = null;
    }

    private void OnDestroy() => KillTweens();
}
