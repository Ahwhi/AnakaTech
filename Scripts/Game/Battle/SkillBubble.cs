using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 스킬 사용 말풍선 1개. 시전자 머리 위에 "스킬명 !" 을 팝(스케일 인) -> 잠깐 유지 -> 상승 + 페이드아웃 후 풀로 반환한다.
/// 월드공간 TextMeshPro(3D) 사용을 전제로 한다(데미지 팝업과 동일 방식, 캔버스 불필요).
/// 배경 이미지 등은 프리팹 구성으로 자유롭게 붙인다(이 스크립트는 텍스트/모션만 다룬다).
/// </summary>
[DisallowMultipleComponent]
public sealed class SkillBubble : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [Tooltip("등장 후 사라지기까지 총 시간(초).")]
    [SerializeField] private float lifetime = 1.0f;
    [Tooltip("머리 위 추가 오프셋(월드 units).")]
    [SerializeField] private float yOffset = 0.4f;
    [Tooltip("유지 후 상승 거리(월드 units).")]
    [SerializeField] private float riseDistance = 0.6f;
    [Tooltip("등장 팝 스케일 시작값(1로 튀어오른다).")]
    [SerializeField] private float startScale = 0.6f;
    [Tooltip("텍스트 정렬순서(유닛보다 위로 보이도록 크게).")]
    [SerializeField] private int sortingOrder = 100;

    private Action<SkillBubble> _onDone;
    private Sequence _seq;
    private Renderer _labelRenderer;

    public void Bind(Action<SkillBubble> onDone) => _onDone = onDone;

    public void Play(Vector3 worldPos, string text, Color color) {
        _seq?.Kill();

        Vector3 start = new Vector3(worldPos.x, worldPos.y + yOffset, worldPos.z);
        Vector3 end = start + Vector3.up * riseDistance;

        transform.position = start;
        transform.localScale = Vector3.one * startScale;
        if (label != null) {
            label.text = text;
            label.color = color;   // 퍼스널 컬러(rgb). 알파는 아래에서 애니메이션이 제어.
            label.alpha = 1f;
        }
        if (_labelRenderer == null && label != null) _labelRenderer = label.GetComponent<Renderer>();
        if (_labelRenderer != null) _labelRenderer.sortingOrder = sortingOrder;

        // 팝(스케일 인) -> 유지 -> 상승 + 페이드아웃.
        float pop = Mathf.Min(0.15f, lifetime * 0.25f);
        float hold = Mathf.Max(0f, lifetime * 0.35f);
        float fade = Mathf.Max(0.05f, lifetime - pop - hold);

        _seq = DOTween.Sequence();
        _seq.Append(transform.DOScale(Vector3.one, pop).SetEase(Ease.OutBack));
        _seq.AppendInterval(hold);
        _seq.Append(transform.DOMove(end, fade).SetEase(Ease.OutQuad));
        if (label != null)
            _seq.Join(DOTween.To(() => label.alpha, a => label.alpha = a, 0f, fade));
        _seq.OnComplete(() => _onDone?.Invoke(this));
    }

    private void OnDisable() => _seq?.Kill();
}
