using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// URL 이미지를 받아 RawImage 에 표시하는 재사용 컴포넌트. 다운로드한 텍스처의 수명까지 관리해
/// (해제 시 Destroy) 메모리 누수를 막는다. 공지 상세/이벤트 화면 등 원격 이미지가 필요한 곳에서 공통으로 쓴다.
///
/// url 이 비어 있으면 이미지를 숨기고, 실패하면 조용히 숨긴다(호출부는 성공/실패를 신경 쓸 필요가 없다).
/// </summary>
[RequireComponent(typeof(RawImage))]
public sealed class RemoteImage : MonoBehaviour {
    [SerializeField] private RawImage target;
    [Tooltip("로딩 중 표시(선택). 로드가 끝나면 자동으로 꺼진다.")]
    [SerializeField] private GameObject loadingIndicator;

    private Texture2D _texture;

    private void Awake() => EnsureTarget();

    // 오브젝트가 비활성 상태로 시작하면 Awake 가 호출되지 않으므로, 사용하는 시점에도 한 번 더 보장한다.
    private void EnsureTarget() {
        if (target == null) target = GetComponent<RawImage>();
    }

    /// <summary>
    /// url 의 이미지를 받아 표시한다. 이전에 표시하던 텍스처는 먼저 해제한다.
    /// 시작 시 이미지를 꺼두고, 텍스처를 실제로 받아온 경우에만 켜서 로딩 중/실패 시 빈 이미지가 노출되지 않게 한다.
    /// </summary>
    public async UniTask LoadAsync(string url, CancellationToken ct = default) {
        EnsureTarget();
        Clear();
        SetVisible(false);

        if (string.IsNullOrEmpty(url)) return;

        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        try {
            using var request = UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest().WithCancellation(ct);

            if (request.result == UnityWebRequest.Result.Success) {
                _texture = DownloadHandlerTexture.GetContent(request);
                if (target != null) target.texture = _texture;
                SetVisible(true);
            }
            // 실패 시에는 비활성 상태를 그대로 유지한다.
        } catch (OperationCanceledException) {
            // 화면 전환/탭 변경 등으로 취소된 경우는 무시한다.
        } catch (Exception e) {
            Debug.LogError($"[RemoteImage] 이미지 로드 실패: {e.Message}");
        } finally {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// 이미지 오브젝트의 하이어라키 활성 상태를 토글한다. 컴포넌트 enabled 만 끄면 하이어라키에는
    /// 활성으로 남으므로, 이미지 GameObject 자체를 켜고 끈다. UniTask 는 PlayerLoop 에서 돌아
    /// 오브젝트가 비활성이어도 다운로드가 계속 진행되므로, 받아온 뒤 다시 켜는 흐름이 안전하다.
    /// </summary>
    private void SetVisible(bool visible) {
        if (target == null) return;
        if (target.gameObject.activeSelf != visible) target.gameObject.SetActive(visible);
    }

    /// <summary>표시 중인 텍스처를 해제한다.</summary>
    public void Clear() {
        if (_texture == null) return;
        if (target != null) target.texture = null;
        Destroy(_texture);
        _texture = null;
    }

    private void OnDestroy() => Clear();
}
