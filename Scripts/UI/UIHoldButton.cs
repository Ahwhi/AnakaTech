using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// "길게 눌러 실행" 버튼 공용 컴포넌트. 실수로 눌리면 곤란한 동작(잠금 해제 등)에 쓴다.
/// 누르고 있는 동안 진행도를 채우고, 다 차면 완료를 한 번 통지한다. 손을 떼거나 영역을 벗어나면 처음으로 되돌린다.
/// 무엇을 할지는 구독하는 쪽이 정하므로 어느 화면에서든 그대로 재사용한다.
///
/// 진행도 이미지는 선택이며, 넣을 경우 Image Type 을 Filled 로 두어야 채워지는 것이 보인다.
/// 입력을 받으려면 이 오브젝트(또는 자식)에 Raycast Target 이 켜진 그래픽이 하나는 있어야 한다.
/// </summary>
public sealed class UIHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
    [Tooltip("완료까지 눌러야 하는 시간(초). 0 이면 누르는 즉시 완료된다.")]
    [SerializeField] private float holdSeconds = 0.8f;
    [Tooltip("진행도(0~1)를 채워 보여줄 이미지(선택). Image Type 을 Filled 로 둘 것.")]
    [SerializeField] private Image progressFill;

    private readonly Subject<Unit> _completed = new();
    private bool _pressed;
    private float _elapsed;

    /// <summary>길게 누르기가 끝났을 때 한 번 통지한다.</summary>
    public IObservable<Unit> Completed => _completed;

    private void OnDisable() => ResetHold();

    private void OnDestroy() {
        _completed.OnCompleted();
        _completed.Dispose();
    }

    public void OnPointerDown(PointerEventData eventData) {
        _pressed = true;
        _elapsed = 0f;
        ApplyProgress(0f);
    }

    public void OnPointerUp(PointerEventData eventData) => ResetHold();

    public void OnPointerExit(PointerEventData eventData) => ResetHold();

    private void Update() {
        if (!_pressed) return;

        // 타임스케일이 0 인 상황(일시정지/연출)에서도 같은 체감이 되도록 실시간으로 잰다.
        _elapsed += Time.unscaledDeltaTime;
        float progress = holdSeconds > 0f ? Mathf.Clamp01(_elapsed / holdSeconds) : 1f;
        ApplyProgress(progress);
        if (progress < 1f) return;

        ResetHold();   // 누르고 있는 동안 여러 번 통지되지 않도록 먼저 처음 상태로 되돌린다.
        _completed.OnNext(Unit.Default);
    }

    private void ResetHold() {
        _pressed = false;
        _elapsed = 0f;
        ApplyProgress(0f);
    }

    private void ApplyProgress(float progress) {
        if (progressFill != null) progressFill.fillAmount = progress;
    }
}
