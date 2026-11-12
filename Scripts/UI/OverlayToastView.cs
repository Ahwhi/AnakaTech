using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Overlay 레이어 토스트(안내/시너지/전투력 상승 등)의 공통 바탕. 입력을 막지 않고(blocksRaycasts off),
/// 표시할 때 activationRoot 를 켜고 연출이 끝나면 끈다.
///
/// 여러 토스트가 같은 activationRoot(보통 꺼진 채로 시작하는 전용 Canvas)를 함께 쓸 수 있다.
/// 그래서 Root 는 "지금 쓰고 있는 토스트 수"로 켜고 끈다. 마지막 토스트가 끝날 때만 꺼지므로
/// 한 토스트가 끝나면서 아직 연출 중인 다른 토스트를 꺼 버리는 일이 없다.
///
/// 어떤 이유로든 꺼지면(OnDisable) 연출을 멈추고 투명하게 되돌린다. 연출 도중 멈춘 채 알파가 남아 있으면
/// 다음에 Root 가 켜질 때 지난 내용이 다른 토스트와 함께 다시 보이기 때문이다.
/// </summary>
public abstract class OverlayToastView : MonoBehaviour {
    [SerializeField] protected CanvasGroup canvasGroup;
    [Tooltip("표시할 때 켜고 끌 대상(보통 토스트 전용 Canvas). 비우면 이 오브젝트 자신을 켜고 끈다. " +
             "부모 Canvas 가 꺼져 있으면 자식만 켜도 렌더되지 않으므로 그 Canvas 를 여기에 지정한다. " +
             "여러 토스트가 같은 Canvas 를 지정해도 된다(마지막 토스트가 끝날 때 꺼진다).")]
    [SerializeField] private GameObject activationRoot;

    // Root 별로 지금 그 Root 를 쓰고 있는 토스트 수.
    private static readonly Dictionary<GameObject, int> RootUsers = new();

    /// <summary>지금 재생 중인 연출. 꺼질 때 여기서 멈춘다.</summary>
    protected Sequence Fade;
    private bool _usingRoot;

    private GameObject Root => activationRoot != null ? activationRoot : gameObject;

    // 비활성으로 시작하면 Awake 는 첫 표시 때 Root 가 켜지는 시점에 호출된다.
    protected virtual void Awake() {
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>표시를 시작할 때 부른다. Root 와 이 오브젝트를 켠다(이미 쓰고 있으면 한 번만 센다).</summary>
    protected void OpenRoot() {
        var root = Root;
        if (!_usingRoot) {
            _usingRoot = true;
            RootUsers.TryGetValue(root, out int users);
            RootUsers[root] = users + 1;
        }
        if (!root.activeSelf) root.SetActive(true);
        // activationRoot 를 부모 Canvas 로 지정했는데 이 오브젝트 자신이 꺼져 있으면 부모만 켜도 보이지 않는다.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    /// <summary>연출이 끝났을 때 부른다. 이 토스트가 Root 의 마지막 사용자면 Root 를 끈다.</summary>
    protected void CloseRoot() => ReleaseRoot(true);

    // deactivate=false 는 이미 꺼지는 중(OnDisable)이라 계층을 건드리지 않고 사용 수만 내린다.
    private void ReleaseRoot(bool deactivate) {
        if (!_usingRoot) return;
        _usingRoot = false;

        var root = Root;
        RootUsers.TryGetValue(root, out int users);
        if (users > 1) { RootUsers[root] = users - 1; return; }

        RootUsers.Remove(root);
        if (deactivate && root != null && root.activeSelf) root.SetActive(false);
    }

    protected virtual void OnDisable() {
        Fade?.Kill();
        Fade = null;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        ReleaseRoot(false);
    }
}
