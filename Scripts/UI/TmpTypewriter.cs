using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP 텍스트를 한 글자씩 드러내는 범용 타입라이터. 같은 오브젝트의 TMP_Text 에 붙여 쓴다.
///
/// 문자열을 잘라 붙이지 않고 maxVisibleCharacters 를 올린다. 줄바꿈 위치가 처음부터 확정되어
/// 글이 늘어나며 줄이 밀리는 현상이 없다(스토리 대사 연출과 같은 방식).
///
/// 새 문장을 Play 하면 진행 중이던 연출은 취소된다. 꺼지면 연출을 멈추고 전부 보여 준 상태로 둔다.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class TmpTypewriter : MonoBehaviour {
    [Tooltip("한 글자가 나타나는 간격(초). 0 이면 즉시 전부 보인다.")]
    [SerializeField, Min(0f)] private float charIntervalSeconds = 0.04f;

    private TMP_Text _text;
    private CancellationTokenSource _cts;

    private TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

    /// <summary>지금 글자를 드러내는 중인지.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>문장을 처음부터 다시 드러낸다(끝나기를 기다리지 않는다).</summary>
    public void Play(string text) {
        Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        PlayAsync(text, _cts.Token).Forget();
    }

    /// <summary>문장을 드러내고 다 나올 때까지 기다린다. 취소되면 그 자리에서 끝난다.</summary>
    public async UniTask PlayAsync(string text, CancellationToken ct) {
        var tmp = Text;
        tmp.text = text ?? string.Empty;
        tmp.maxVisibleCharacters = 0;
        tmp.ForceMeshUpdate();

        int total = tmp.textInfo.characterCount;
        float elapsed = 0f;
        IsPlaying = true;
        try {
            for (int shown = 0; shown < total; ) {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                // 프레임 시간이 글자 간격보다 길 수 있으므로 누적 시간으로 몇 글자를 드러낼지 정한다.
                elapsed += Time.unscaledDeltaTime;
                shown = charIntervalSeconds <= 0f ? total : Mathf.Min(total, Mathf.FloorToInt(elapsed / charIntervalSeconds));
                tmp.maxVisibleCharacters = shown;
            }
        } catch (System.OperationCanceledException) {
            // 취소는 정상 종료로 본다. 아래에서 전부 보여 준다.
        } finally {
            IsPlaying = false;
            if (tmp != null) tmp.maxVisibleCharacters = int.MaxValue;
        }
    }

    /// <summary>진행 중인 연출을 멈추고 전부 보여 준다.</summary>
    public void Complete() => Cancel();

    private void Cancel() {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private void OnDisable() => Cancel();
}
