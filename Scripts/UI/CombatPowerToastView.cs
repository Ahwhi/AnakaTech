using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 총 전투력 상승 토스트(Overlay 레이어). 상승분(+N)과 바뀐 총 전투력을 보여주며, 숫자가 올라가는 연출을 준다.
/// ToastView 와 같은 규약이다(OverlayToastView): 입력을 막지 않고, 활성/비활성을 스스로 관리하며,
/// UIRoot 의 자식으로 배치하고 표시는 UIRoot.ShowCombatPowerGain 을 통해서만 한다.
///
/// 연출 도중 다시 오르면(연속 강화) 지금 화면에 떠 있는 숫자에서 이어서 올라가고, 상승분도 그 지점 기준으로 다시 적는다.
/// 숫자만 그리는 표시 전용 컴포넌트라 "무엇이 전투력인지"는 전혀 알지 않는다(판단은 CombatPowerAnnouncer).
/// </summary>
public class CombatPowerToastView : OverlayToastView {
    [Tooltip("상승분 텍스트(예: +1,234).")]
    [SerializeField] private TextMeshProUGUI gainText;
    [Tooltip("바뀐 총 전투력 텍스트. 이 숫자가 올라가는 연출의 대상이다.")]
    [SerializeField] private TextMeshProUGUI totalText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [Tooltip("숫자가 목표치까지 올라가는 데 걸리는 시간(초).")]
    [SerializeField] private float countDuration = 0.6f;

    [Header("Punch (선택)")]
    [Tooltip("숫자가 다 오른 순간 톡 튀게 할 대상(보통 총 전투력 텍스트의 RectTransform). 비우면 연출 없음.")]
    [SerializeField] private RectTransform punchTarget;
    [SerializeField] private float punchScale = 0.15f;
    [SerializeField] private float punchDuration = 0.25f;

    private bool _showing;
    private long _displayed;  // 지금 그려져 있는 숫자. 연속 상승 시 여기서 이어 올린다(21억을 넘을 수 있어 long).

    /// <summary>상승을 표시한다. 이미 떠 있으면 현재 숫자에서 after 까지 이어서 올린다. 오르지 않았으면 아무것도 하지 않는다.</summary>
    public void Show(long before, long after, float duration) {
        if (canvasGroup == null) return;

        long from = _showing ? _displayed : before;
        if (after <= from) return;

        OpenRoot();
        Fade?.Kill();
        if (punchTarget != null) {
            punchTarget.DOKill();
            punchTarget.localScale = Vector3.one;
        }

        _showing = true;
        SetNumbers(from, after - from);
        canvasGroup.alpha = 0f;

        Fade = DOTween.Sequence().SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, fadeInDuration))
            // 진행률(0 에서 1)만 트윈하고 숫자는 long 으로 계산한다. 값 자체를 float 으로 트윈하면 21억대에서 정밀도가 모자라 끝자리가 뭉개진다.
            .Join(DOVirtual.Float(0f, 1f, countDuration, t => SetTotal(from + (long)Math.Round((after - from) * (double)t))).SetUpdate(true))
            .AppendCallback(PlayPunch)
            .AppendInterval(duration)
            .Append(canvasGroup.DOFade(0f, fadeOutDuration))
            .OnComplete(() => {
                _showing = false;
                CloseRoot();
            });
    }

    private void SetNumbers(long total, long gain) {
        SetTotal(total);
        if (gainText != null) gainText.text = $"+{gain:N0}";
    }

    private void SetTotal(long value) {
        _displayed = value;
        if (totalText != null) totalText.text = value.ToString("N0");
    }

    private void PlayPunch() {
        if (punchTarget == null) return;
        punchTarget.DOPunchScale(Vector3.one * punchScale, punchDuration, 6, 0.6f).SetUpdate(true);
    }

    protected override void OnDisable() {
        // 연출 도중 꺼지면 다음 상승은 이어 올리지 않고 새로 시작한다.
        base.OnDisable();
        _showing = false;
    }
}
