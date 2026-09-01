using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼을 꾹 누르고 있으면 onClick 을 반복 발동시키는 범용 컴포넌트(스탯 투자 +, 수량 증감 등).
/// 같은 오브젝트의 Button 만 참조하므로, 어느 버튼이든 이 스크립트를 붙이기만 하면 동작한다
/// (호출측 코드는 그대로 onClick 에 리스너를 달면 된다).
///
/// 탭과 홀드를 holdDelay 하나로 명확히 가른다.
///  - 짧은 탭(holdDelay 이내에 뗌): 이 컴포넌트는 아무 것도 하지 않는다. 기본 Button 동작대로
///    뗄 때 onClick 이 정확히 1회 발동한다(끌어서 취소도 그대로 된다).
///  - 홀드(holdDelay 초과): 그 시점부터 startInterval 간격으로 반복 발동하고, 간격이 accelRatio 배씩
///    짧아져 minInterval 까지 빨라진다. 뗄 때의 기본 클릭은 억제되므로 반복 횟수에 1이 더 붙지 않는다.
/// 즉 어느 쪽이든 "누른 만큼만" 발동하며, 경계에서 2회 발동하는 구간이 없다.
///
/// 멈추는 기준은 "손을 뗐는가" 하나뿐이다. 버튼 영역을 벗어난 것은 멈춤 신호로 쓰지 않는다.
/// 터치에서는 손가락이 조금만 흔들려도, 또 반복으로 값이 바뀌어 레이아웃이 다시 잡히면서 버튼이 손가락 아래에서
/// 밀려도 Exit 가 오는데, 그때마다 연타가 끊기면 실기기에서는 몇 번 눌리다 멈춘 것처럼 보인다.
/// 뗄 때의 PointerUp 은 누른 대상에게 위치와 무관하게 전달되므로 이것만으로 충분하다.
///
/// 반복 도중 버튼이 잠기면(interactable=false) 발동만 건너뛰고 루프는 유지한다. 서버 왕복이 필요한 버튼은
/// 응답을 기다리는 동안 잠깐씩 잠기는데, 그때 루프를 끝내 버리면 손을 뗐다 다시 눌러야 한다.
/// </summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class ButtonHoldRepeater : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    [Tooltip("누른 뒤 연타가 시작되기까지의 대기 시간(초). 짧은 탭이 연타로 이어지지 않게 하는 여유.")]
    [SerializeField] private float holdDelay = 0.4f;
    [Tooltip("연타 시작 간격(초).")]
    [SerializeField] private float startInterval = 0.15f;
    [Tooltip("연타가 빨라질 수 있는 하한 간격(초). 너무 작게 두면 서버 왕복이 필요한 동작에서는 의미가 없다.")]
    [SerializeField] private float minInterval = 0.04f;
    [Tooltip("반복 1회마다 간격에 곱해지는 값(1 미만이면 점점 빨라진다). 1 이면 등속 연타.")]
    [SerializeField] private float accelRatio = 0.85f;

    private const int NoPointer = int.MinValue;

    private Button _button;
    private CancellationTokenSource _holdCts;
    private bool _repeated;      // 이번 누름이 연타로 넘어갔는지(뗄 때 기본 클릭을 억제할지 판단).
    private int _pointerId = NoPointer;   // 연타를 시작한 손가락. 다른 손가락의 뗌으로 끊기지 않게 한다.

    private Button Target => _button != null ? _button : (_button = GetComponent<Button>());

    public void OnPointerDown(PointerEventData eventData) {
        _repeated = false;
        if (Target == null || !Target.interactable) return;
        _pointerId = eventData != null ? eventData.pointerId : NoPointer;
        StartHold();   // 여기서 발동하지 않는다. holdDelay 를 넘겨야 연타로 진입한다.
    }

    public void OnPointerUp(PointerEventData eventData) {
        // 멀티터치에서 다른 손가락이 떨어진 것이라면 이 누름은 계속 유지한다.
        if (eventData != null && _pointerId != NoPointer && eventData.pointerId != _pointerId) return;

        StopHold();
        // 연타로 넘어갔으면 뗄 때 Button 이 내는 기본 클릭을 막는다(연타 횟수에 1이 더 붙는 것을 방지).
        // EventSystem 은 PointerUp 을 먼저 처리하고 eligibleForClick 이 남아 있을 때만 Click 을 실행한다.
        // 반대로 짧은 탭은 아무 것도 막지 않으므로 기본 Button 동작 그대로 정확히 1회만 발동한다.
        if (_repeated && eventData != null) eventData.eligibleForClick = false;
    }

    private void OnDisable() => StopHold();

    private void OnDestroy() {
        StopHold();
        _holdCts?.Dispose();
        _holdCts = null;
    }

    private void StartHold() {
        StopHold();
        _holdCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        RepeatAsync(_holdCts.Token).Forget();
    }

    private void StopHold() {
        _pointerId = NoPointer;
        if (_holdCts == null) return;
        _holdCts.Cancel();
        _holdCts.Dispose();
        _holdCts = null;
    }

    // 대기 -> 간격을 줄여가며 반복 발동. 타임스케일을 무시하므로 시간이 멈추는 팝업 위에서도 동작한다.
    private async UniTaskVoid RepeatAsync(CancellationToken ct) {
        try {
            await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, holdDelay)), ignoreTimeScale: true, cancellationToken: ct);

            float interval = Mathf.Max(0.01f, startInterval);
            float floor = Mathf.Max(0.01f, minInterval);

            while (!ct.IsCancellationRequested) {
                if (Target == null) return;
                // 잠긴 동안(소진, 서버 응답 대기)에는 발동만 건너뛴다. 손을 떼면 어차피 취소되므로 루프를 끝내지 않는다.
                Fire();
                await UniTask.Delay(TimeSpan.FromSeconds(interval), ignoreTimeScale: true, cancellationToken: ct);
                interval = Mathf.Max(floor, interval * Mathf.Clamp(accelRatio, 0.1f, 1f));
            }
        } catch (OperationCanceledException) {
            // 손을 뗐거나 오브젝트가 사라진 정상 종료.
        }
    }

    private void Fire() {
        if (Target == null || !Target.interactable) return;
        _repeated = true;
        Target.onClick.Invoke();
    }
}
