using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 시너지 활성화 토스트(Overlay 레이어). 일반 안내 토스트(ToastView)와 자리/연출을 분리해, 편성을 바꾸다
/// 조합을 찾아냈다는 것이 실패 안내와 섞이지 않고 "발견"으로 읽히게 한다.
///
/// ToastView 와 같은 규약이다: 입력을 막지 않고(blocksRaycasts off), 활성/비활성을 스스로 관리하며,
/// UIRoot 의 자식으로 배치하고 표시는 UIRoot.ShowSynergyActivated 를 통해서만 한다.
///
/// 한 번의 편성 변경으로 시너지가 둘 이상 켜질 수 있어 큐로 받아 차례대로 보여 준다. 덮어써 버리면
/// 마지막 하나만 보이는데, 시너지는 유저가 직접 찾아내야 하는 정보라 알림이 사라지면 발견 자체가 무산된다.
/// 시너지 이름/효과 문구는 부르는 쪽이 만들어 넘기므로 이 컴포넌트는 시너지가 무엇인지 알지 않는다(표시 전용).
/// </summary>
public class SynergyToastView : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("시너지 이름.")]
    [SerializeField] private TextMeshProUGUI nameText;
    [Tooltip("시너지 효과 요약. 비우면 이름만 표시한다.")]
    [SerializeField] private TextMeshProUGUI effectText;
    [Tooltip("표시할 때 켜고 끌 대상(보통 전용 Canvas). 비우면 이 오브젝트 자신을 켜고 끈다. " +
             "부모 Canvas 가 꺼져 있으면 자식만 켜도 렌더되지 않으므로 그 Canvas 를 여기에 지정한다.")]
    [SerializeField] private GameObject activationRoot;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Punch (선택)")]
    [Tooltip("나타나는 순간 톡 튀게 할 대상. 비우면 연출 없음.")]
    [SerializeField] private RectTransform punchTarget;
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.3f;

    private readonly Queue<Entry> _queue = new();
    private Sequence _sequence;
    private bool _playing;

    private readonly struct Entry {
        public readonly string Name;
        public readonly string Effect;
        public readonly float Duration;
        public Entry(string name, string effect, float duration) {
            Name = name; Effect = effect; Duration = duration;
        }
    }

    private GameObject Root => activationRoot != null ? activationRoot : gameObject;

    // 비활성으로 시작하면 Awake 는 첫 Show 의 SetActive(true) 시점에 호출된다.
    private void Awake() {
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>시너지 활성화를 알린다. 이미 재생 중이면 뒤에 줄을 세워 차례대로 보여 준다.</summary>
    public void Show(string synergyName, string effectSummary, float duration) {
        if (string.IsNullOrEmpty(synergyName)) return;
        if (canvasGroup == null) {
            Debug.LogWarning("[SynergyToastView] canvasGroup 이 비어 있어 표시할 수 없습니다. " +
                             "이 오브젝트의 CanvasGroup 을 인스펙터에 연결하세요.", this);
            return;
        }

        _queue.Enqueue(new Entry(synergyName, effectSummary, duration));
        if (!_playing) PlayNext();
    }

    private void PlayNext() {
        if (_queue.Count == 0) {
            _playing = false;
            Root.SetActive(false);
            return;
        }

        _playing = true;
        var entry = _queue.Dequeue();

        Root.SetActive(true);
        // activationRoot 를 부모(전용 Canvas)로 지정했는데 이 오브젝트 자신이 꺼져 있으면 부모만 켜도 보이지 않는다.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (nameText != null) nameText.text = entry.Name;
        if (effectText != null) {
            effectText.gameObject.SetActive(!string.IsNullOrEmpty(entry.Effect));
            effectText.text = entry.Effect;
        }

        _sequence?.Kill();
        if (punchTarget != null) {
            punchTarget.DOKill();
            punchTarget.localScale = Vector3.one;
        }
        canvasGroup.alpha = 0f;

        _sequence = DOTween.Sequence().SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, fadeInDuration))
            .AppendCallback(PlayPunch)
            .AppendInterval(entry.Duration)
            .Append(canvasGroup.DOFade(0f, fadeOutDuration))
            .OnComplete(PlayNext);   // 큐가 비면 여기서 Root 가 꺼진다.
    }

    private void PlayPunch() {
        if (punchTarget == null) return;
        punchTarget.DOPunchScale(Vector3.one * punchScale, punchDuration, 6, 0.6f).SetUpdate(true);
    }

    private void OnDisable() {
        // 화면 전환 등으로 꺼지면 남은 알림은 의미가 없다(다음 편성 변경에서 다시 판정된다).
        _sequence?.Kill();
        _sequence = null;
        _queue.Clear();
        _playing = false;
    }
}
