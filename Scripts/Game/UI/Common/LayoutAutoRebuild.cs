using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LayoutGroup / ContentSizeFitter 의 정렬이 한 박자 늦게 반영되는 문제를 덮는 범용 컴포넌트.
/// 정렬이 어긋나는 LayoutGroup 오브젝트에 붙이기만 하면 되고 배선은 필요 없다.
///
/// 왜 필요한가: TextMeshPro 의 선호 크기는 텍스트 메시가 다시 만들어진 뒤에 확정되는데,
/// LayoutGroup 의 정렬 계산이 그보다 먼저 도는 경우가 있어 한 프레임 낡은 크기로 배치된다.
/// 그래서 "패널을 처음 열 때"나 "비용이 - 에서 숫자로 바뀔 때"처럼 텍스트 폭이 크게 달라지는 순간에
/// 아이콘/텍스트 간격이 어긋나 보이고, 이후 아무 동작이나 해서 레이아웃이 한 번 더 계산되면 맞춰진다.
///
/// 두 시점을 모두 덮는다.
///  - 켜질 때: 즉시 + 다음 프레임 재계산(활성화 프레임에는 TMP 크기가 아직 확정되지 않을 수 있다).
///  - 켜져 있는 동안 자식 텍스트 내용이 바뀔 때: 그 프레임의 LateUpdate 에 재계산
///    (레이아웃/렌더 갱신보다 앞선 시점이라 같은 프레임 안에 정렬이 맞는다).
/// 텍스트 감시는 자식 TMP_Text 의 문자열 비교뿐이라 비용이 사실상 없고, 실제로 바뀐 프레임에만 재계산한다.
///
/// 위 둘은 어디까지나 배선 없이 동작하는 안전망이고, 감지 근거가 텍스트 문자열뿐이라 한계가 있다.
/// 자식을 켜고 끄거나 슬롯을 새로 만드는 것처럼 문자열이 그대로인 변화는 감지되지 않으므로,
/// 자기가 무엇을 바꿨는지 아는 화면은 RebuildDeferred 로 직접 알린다(감지에 맡기지 않는다).
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public sealed class LayoutAutoRebuild : MonoBehaviour {
    [Tooltip("켜질 때 다음 프레임에 한 번 더 재계산한다. TMP 크기가 활성화 프레임에 늦게 잡히는 경우까지 덮는다.")]
    [SerializeField] private bool rebuildNextFrame = true;
    [Tooltip("켜져 있는 동안 자식 텍스트가 바뀌면 재계산한다. 장비 교체/비용 갱신처럼 내용만 바뀌는 경우를 덮는다.")]
    [SerializeField] private bool rebuildOnTextChange = true;
    [Tooltip("이 오브젝트 대신 재계산할 대상(보통 정렬이 어긋나는 LayoutGroup 루트). 비우면 자신.")]
    [SerializeField] private RectTransform target;

    private RectTransform _rt;
    private TMP_Text[] _texts;
    private string[] _lastTexts;
    private bool _nextFrameQueued;

    private RectTransform Target => target != null ? target : (_rt != null ? _rt : (_rt = (RectTransform)transform));

    private void Awake() => CacheTexts();

    private void OnEnable() {
        // 꺼져 있는 동안 바뀐 내용이 "변경"으로 잡히지 않도록 기준값을 현재 상태로 맞춘 뒤 시작한다.
        SyncTextSnapshot();
        Rebuild();
        if (rebuildNextFrame) RebuildNextFrameAsync().Forget();
    }

    private void LateUpdate() {
        if (rebuildOnTextChange && HasTextChanged()) Rebuild();
    }

    /// <summary>레이아웃을 즉시 다시 계산한다. 내용을 코드로 채운 직후 수동으로 부를 수도 있다.</summary>
    public void Rebuild() {
        var rt = Target;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    /// <summary>
    /// 내용을 코드로 바꾼 직후 부른다. 켜질 때와 같은 방식으로 즉시 + 다음 프레임에 재계산한다.
    ///
    /// 자동 감지(rebuildOnTextChange)는 자식 텍스트의 "문자열"이 달라졌는지만 본다. 그래서 자식을 켜고 끄거나,
    /// 슬롯을 새로 만들거나, 글자는 그대로인데 색만 바꾸는 변화는 감지되지 않는다. 자기가 무엇을 바꿨는지 아는
    /// 화면은 추측에 맡기지 말고 이 창구로 직접 알리는 것이 맞다.
    ///
    /// 다음 프레임 재계산까지 하는 이유는 켜질 때와 같다. TMP 의 선호 크기는 텍스트 메시가 다시 만들어진 뒤에
    /// 확정되므로, 내용을 바꾼 그 프레임의 계산 한 번만으로는 낡은 크기가 남을 수 있다.
    /// </summary>
    public void RebuildDeferred() {
        Rebuild();
        RebuildNextFrameAsync().Forget();
    }

    /// <summary>감시 대상 텍스트를 다시 수집한다(자식 텍스트를 런타임에 생성/제거한 경우에만 필요).</summary>
    public void RefreshTargets() {
        CacheTexts();
        SyncTextSnapshot();
    }

    private void CacheTexts() {
        var rt = Target;
        _texts = rt != null ? rt.GetComponentsInChildren<TMP_Text>(true) : Array.Empty<TMP_Text>();
        _lastTexts = new string[_texts.Length];
    }

    // 현재 텍스트를 기준값으로 저장(변경 감지의 출발점).
    private void SyncTextSnapshot() {
        if (_texts == null) CacheTexts();
        for (int i = 0; i < _texts.Length; i++)
            _lastTexts[i] = _texts[i] != null ? _texts[i].text : null;
    }

    // 하나라도 내용이 달라졌으면 true(그리고 기준값을 갱신).
    private bool HasTextChanged() {
        if (_texts == null) return false;
        bool changed = false;
        for (int i = 0; i < _texts.Length; i++) {
            var t = _texts[i];
            if (t == null) continue;
            if (t.text != _lastTexts[i]) {
                _lastTexts[i] = t.text;
                changed = true;
            }
        }
        return changed;
    }

    // 매 프레임 바뀌는 텍스트(카운트다운 등)가 붙어 있어도 대기가 쌓이지 않도록 한 번에 하나만 예약한다.
    private async UniTaskVoid RebuildNextFrameAsync() {
        if (_nextFrameQueued) return;
        _nextFrameQueued = true;
        try {
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
            if (isActiveAndEnabled) Rebuild();
        } catch (OperationCanceledException) {
            // 패널이 곧바로 닫히거나 파괴된 정상 종료.
        } finally {
            _nextFrameQueued = false;
        }
    }
}
