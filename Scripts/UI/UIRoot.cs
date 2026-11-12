using System;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 전역 UI 루트(IUIService 구현). 레이어별 Canvas 와 시스템 UI(로딩/토스트)를 보유한다.
/// 씬 전환에도 살아있어야 하므로 부트 오브젝트(GameLifetimeScope)의 자식으로 두어 함께 유지된다.
///
/// 레이어 권장 sortingOrder:
///   Light(50) < Popup(100) < Overlay(200=토스트) < System(300=로딩)
/// 공용 팝업은 인스펙터에서 할당한 AssetReference 프리팹을 해당 레이어 밑에 인스턴스화하며, 닫힐 때 자동 해제한다.
/// Light 레이어는 던전 클리어 획득 연출처럼 "다른 팝업을 가리지 않고 뒤에 가볍게" 띄우는 비차단 표시용이다.
/// </summary>
public class UIRoot : MonoBehaviour, IUIService {
    [Header("Layers")]
    [Tooltip("공용 팝업이 인스턴스화될 부모(Popup Canvas, SortOrder 100).")]
    [SerializeField] private Transform popupLayer;
    [Tooltip("비차단 경량 표시가 인스턴스화될 부모(Light Canvas, SortOrder 50). 던전 클리어 획득 등.")]
    [SerializeField] private Transform lightPopupLayer;

    [Header("System UI")]
    [SerializeField] private LoadingOverlay loadingOverlay;
    [Tooltip("씬 전환 전용 전체 화면 로딩(진행도 + 팁). 위의 로딩 오버레이와 다른 오브젝트여야 한다.")]
    [SerializeField] private SceneLoadingScreen sceneLoadingScreen;
    [SerializeField] private ToastView toast;
    [Tooltip("총 전투력 상승 토스트(선택). 비우면 상승 연출이 표시되지 않는다. 일반 토스트와 겹치지 않는 위치에 배치할 것.")]
    [SerializeField] private CombatPowerToastView combatPowerToast;
    [Tooltip("시너지 활성화 토스트(선택). 비우면 시너지 알림이 표시되지 않는다. 일반 토스트와 겹치지 않는 위치에 배치할 것.")]
    [SerializeField] private SynergyToastView synergyToast;
    [Tooltip("전역 공유 툴팁(선택). 할당하면 tooltip 미지정 TooltipTrigger 들이 자동으로 이걸 쓴다. 오브젝트를 꺼둬도 됨(여기서 등록).")]
    [SerializeField] private TooltipView tooltip;
    [Tooltip("전역 UI 파티클 VFX 레이어(선택). 팝업보다 위 캔버스에 두고 항상 활성으로 유지. IUIService.PlayVfx 가 여기로 재생한다.")]
    [SerializeField] private UIVfxLayer vfxLayer;

    private IAssetService _asset;

    // 레이어별 컨테이너 활성 관리. 팝업이 하나라도 있으면 켜고, 마지막이 닫히면 끈다.
    private PopupLayerState _popup;
    private PopupLayerState _light;

    // 인스턴스화가 끝난(떠 있는) 팝업들. "연 순서"가 곧 쌓인 순서이므로 목록으로 들고 있는다.
    //   - 한꺼번에 닫기(CloseAllPopups) 는 순서와 무관하지만,
    //   - 뒤로가기는 반드시 가장 위(마지막에 연) 팝업 하나에만 가야 해서 순서가 필요하다.
    private readonly List<OpenPopup> _openPopups = new();

    // "전부 닫기"가 몇 번 지나갔는지. 로드 중이던 팝업이 그 정리를 놓치지 않게 하는 표식이다.
    private int _closeGeneration;

    /// <summary>현재 떠 있는(로드 중 포함) 팝업이 하나라도 있는지. 타이틀의 "터치하여 시작"이 팝업 위 클릭을 게임 시작으로 오인하지 않게 한다.</summary>
    public bool HasOpenPopup => _popup.Count + _light.Count > 0;

    /// <summary>전역 로딩 오버레이가 떠 있는지. 화면을 덮고 입력을 막는 동안 뒤로가기 등을 무시할 때 쓴다.</summary>
    public bool IsLoading => loadingOverlay != null && loadingOverlay.IsVisible;

    private void Awake() {
        _popup = new PopupLayerState(popupLayer);
        // Light 레이어는 비차단이라 연속 클리어처럼 비기 전에 계속 새로 뜰 수 있다. 정렬순서가 Popup 레이어를
        // 넘지 않도록 상한을 둔다. Popup 레이어는 입력을 막아 계속 쌓이지 않으므로 상한 없이 기존대로 둔다.
        _light = new PopupLayerState(lightPopupLayer, _popup.BaseOrder);
        // 항상 활성인 UIRoot 가 전역 툴팁을 등록한다(툴팁 오브젝트가 비활성이어도 확실히 등록됨).
        if (tooltip != null) tooltip.RegisterAsGlobal();
    }

    /// <summary>VContainer 가 자산 서비스를 메서드 주입(팝업 프리팹 로드용).</summary>
    [Inject]
    public void Construct(IAssetService asset) => _asset = asset;

    // ── 팝업 ────────────────────────────────────────────────
    // 인스턴스화 + 해제 콜백 연결까지만 하고, 내용 세팅/표시는 호출부가 한다.
    //   var popup = await UI.ShowPopupAsync<ConfirmPopup>(_popups.ConfirmPopup);
    //   await popup.ShowAsync("알림", "처리가 완료되었습니다.");
    public async UniTask<T> ShowPopupAsync<T>(AssetReference reference, CancellationToken ct = default, UIPopupLayer layer = UIPopupLayer.Popup) where T : UIView {
        // 카탈로그 필드가 인스펙터에서 비어 있으면 런타임 키가 빈 문자열이라, 어드레서블이
        // "Keys=" 만 찍힌 InvalidKeyException 을 던져 어느 팝업인지 추적이 어렵다. 어떤 팝업인지 먼저 알린다.
        if (reference == null || !reference.RuntimeKeyIsValid())
            throw new InvalidOperationException($"{typeof(T).Name} 프리팹 참조가 비어 있습니다. UIPopupCatalog 에셋에서 해당 항목을 할당하세요.");

        var target = layer == UIPopupLayer.Light ? _light : _popup;
        if (target.NeedsRenumber) RenumberLayer(target);

        // 부모가 꺼져 있으면 인스턴스화된 팝업도 함께 비활성이라 보이지 않는다. 인스턴스화 전에 켠다.
        // 프리팹 로드(await)는 한 프레임 이상 걸린다. 그 사이 다른 팝업이 닫혀 카운트가 0이 되면
        // 레이어가 꺼져, 로드된 이 팝업이 안 보이거나 닫힘 페이드(DOTween)가 멈춰 고아가 된다.
        // 따라서 "로드 중"부터 카운트에 미리 포함시켜(레이어가 꺼지지 않게) 경합을 막는다.
        int sortingOrder = target.Reserve();   // 나중에 연 팝업일수록 항상 더 큰 정렬순서(단조 증가). 로드 취소돼도 무해.
        int closeGeneration = _closeGeneration;
        try {
            var go = await _asset.InstantiateAsync(reference, target.Root, ct);

            // 로드가 끝나기 전에 "전부 닫기"가 지나갔다면 이 팝업은 이미 없어야 할 것이다.
            // 목록에 없었던 탓에 그 정리를 피해 갔을 뿐이므로 여기서 스스로 물러난다. 그러지 않으면
            // 씬 전환(전투 진입/복귀)에도 살아남아, 내용이 비어 있고 입력만 먹는 팝업이 화면에 남는다.
            if (closeGeneration != _closeGeneration) {
                _asset.ReleaseInstance(go);
                throw new OperationCanceledException();
            }

            var view = go.GetComponent<T>();
            if (view == null) {
                _asset.ReleaseInstance(go);
                throw new InvalidOperationException($"팝업 프리팹에 {typeof(T).Name} 컴포넌트가 없습니다.");
            }

            // 각 팝업을 독립된 정렬 그룹(overrideSorting Canvas)으로 만든다.
            // TMP_Dropdown 등은 열릴 때 정렬순서 30000 짜리 오버라이드 캔버스와 전체화면 Blocker 를
            // "루트 레이어 캔버스" 바로 아래에 주입한다. 팝업이 자체 정렬 그룹이 아니면 이 주입이
            // 레이어 캔버스의 배치를 쪼개 팝업 배경/딤의 그리기 순서가 흐트러지고, 뒤 UI 가 비쳐 보인다.
            // 팝업마다 자체 Canvas 로 원자 렌더시키면 이 문제가 사라지고, 스택 순서(나중에 연 팝업이 위)도 유지된다.
            ApplySortingIsland(go, sortingOrder);

            view.BindCloseHandler(() => CloseInstance(go));
            _openPopups.Add(new OpenPopup(go, view, target));
            // 팝업은 처음 열 때만 자산을 새로 로드한다. 이 지점의 증가량이 곧 그 팝업의 실제 비용이다.
            MemoryProbe.Mark($"팝업 {typeof(T).Name}");
            return view;
        } catch {
            // 로드 취소/실패 또는 컴포넌트 누락 시, 미리 올린 예약분을 되돌린다.
            target.Release();
            throw;
        }
    }

    // 팝업 하나 닫기(중복 호출 방지). 정상 닫힘 콜백과 일괄 닫힘이 모두 이 경로를 탄다.
    private void CloseInstance(GameObject go) {
        int index = IndexOf(go);
        if (index < 0) return;

        var entry = _openPopups[index];
        _openPopups.RemoveAt(index);
        _asset.ReleaseInstance(go);
        entry.Layer.Release();
    }

    private int IndexOf(GameObject go) {
        for (int i = 0; i < _openPopups.Count; i++)
            if (_openPopups[i].Go == go) return i;
        return -1;
    }

    /// <summary>
    /// 가장 위에 떠 있는 팝업에 뒤로가기를 넘긴다(넘겼으면 true, 떠 있는 팝업이 없으면 false).
    ///
    /// "가장 위"는 정렬순서 그대로 판단한다. Popup 레이어(100)가 Light 레이어(50)보다 항상 위이므로
    /// 일반 팝업이 하나라도 있으면 그중 마지막에 연 것이 대상이고, 없을 때만 Light 레이어를 본다.
    /// 실제로 무엇을 할지는 팝업이 정한다(UIView.OnBackKey). 여기서는 대상을 고르기만 한다.
    /// </summary>
    public bool TryHandleBackKey() {
        var target = TopMost();
        if (target == null) return false;

        target.OnBackKey();
        return true;
    }

    private UIView TopMost() {
        UIView light = null;
        for (int i = _openPopups.Count - 1; i >= 0; i--) {
            var entry = _openPopups[i];
            if (entry.View == null) continue;              // 파괴된 잔여(씬 전환 등)는 건너뛴다.
            if (entry.Layer == _popup) return entry.View;  // 일반 팝업이 항상 우선.
            light ??= entry.View;
        }
        return light;
    }

    // 떠 있는 팝업 한 건. 해제(레이어 반환)와 뒤로가기 전달에 필요한 것만 들고 있는다.
    private readonly struct OpenPopup {
        public readonly GameObject Go;
        public readonly UIView View;
        public readonly PopupLayerState Layer;

        public OpenPopup(GameObject go, UIView view, PopupLayerState layer) {
            Go = go;
            View = view;
            Layer = layer;
        }
    }

    // 팝업 GameObject 를 자체 정렬 그룹으로 만든다(overrideSorting Canvas + GraphicRaycaster).
    // overrideSorting 캔버스의 그래픽은 부모 레이어의 GraphicRaycaster 가 잡지 못하므로 자체 레이캐스터가 반드시 필요하다.
    // GraphicRaycaster 는 Canvas 를 요구하므로 Canvas 를 먼저 보장한다.
    private static void ApplySortingIsland(GameObject go, int sortingOrder) {
        var canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
    }

    // 레이어가 비지 않은 채 계속 열려 정렬순서가 상한에 닿았을 때, 떠 있는 팝업들의 순서를 연 순서대로 처음부터 다시 매긴다.
    // 나중에 연 팝업이 위라는 순서는 그대로이고 값만 작아진다. 로드 중인 팝업은 이미 받은 순서(상한 미만)를 그대로 쓴다.
    private void RenumberLayer(PopupLayerState layer) {
        layer.RestartOrder();
        foreach (var entry in _openPopups) {
            if (entry.Layer != layer || entry.Go == null) continue;
            var canvas = entry.Go.GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = layer.NextOrder();
        }
    }

    // 떠 있는 모든 팝업을 즉시 닫는다(연결 끊김 등). CloseInstance 가 목록을 변형하므로 스냅샷 위로 순회한다.
    //
    // 세대 값을 올리는 것이 핵심이다. 아직 로드 중인 팝업은 목록에 없어 여기서 닫히지 않는데,
    // 그대로 두면 로드가 끝난 뒤 화면에 나타나 씬 전환에도 살아남는다(ShowPopupAsync 가 세대로 걸러낸다).
    public void CloseAllPopups() {
        _closeGeneration++;
        if (_openPopups.Count == 0) return;

        var snapshot = new GameObject[_openPopups.Count];
        for (int i = 0; i < _openPopups.Count; i++) snapshot[i] = _openPopups[i].Go;
        foreach (var go in snapshot) CloseInstance(go);
    }

    // 레이어 컨테이너의 활성/참조카운트 상태. 컨테이너가 없으면(라이트 레이어 미할당 등) 안전하게 무시된다.
    private sealed class PopupLayerState {
        private readonly Transform _root;
        private readonly int _baseOrder;   // 레이어 컨테이너 캔버스의 정렬순서(예: Light=50, Popup=100). 팝업 정렬순서의 기준점.
        private int _count;   // 현재 떠 있는(로드 중 포함) 팝업 수. 0 이 되면 레이어를 끄고 오프셋을 리셋한다.
        private int _offset;  // 마지막으로 부여한 정렬 오프셋(레이어가 빌 때까지 단조 증가).
        // 이 레이어 팝업이 넘으면 안 되는 정렬순서(그 값 미만만 쓴다). 위 레이어의 기준값을 넣는다.
        // 지정하지 않거나 기준값 이하로 잘못 들어오면 상한 없음(기존 동작)으로 둔다.
        private readonly int _ceiling;

        public PopupLayerState(Transform root, int ceiling = int.MaxValue) {
            _root = root;
            var canvas = root != null ? root.GetComponent<Canvas>() : null;
            _baseOrder = canvas != null ? canvas.sortingOrder : 0;
            _ceiling = ceiling > _baseOrder + 1 ? ceiling : int.MaxValue;
        }

        public Transform Root => _root;
        public int Count => _count;
        public int BaseOrder => _baseOrder;

        /// <summary>다음에 부여할 순서가 상한에 닿는지. 닿으면 UIRoot 가 떠 있는 팝업 순서를 다시 매긴 뒤 예약한다.</summary>
        public bool NeedsRenumber => _ceiling != int.MaxValue && _baseOrder + _offset + 1 >= _ceiling;

        /// <summary>순서 다시 매기기의 시작. 이후 NextOrder 를 떠 있는 팝업 수만큼 부른다.</summary>
        public void RestartOrder() => _offset = 0;

        /// <summary>다음 정렬순서(단조 증가). 상한 안에서만 준다(가득 차면 상한 바로 아래를 준다).</summary>
        public int NextOrder() {
            if (_ceiling == int.MaxValue || _baseOrder + _offset + 1 < _ceiling) _offset++;
            return _baseOrder + _offset;
        }

        // 예약과 동시에 정렬순서를 부여한다. 오프셋을 단조 증가시키므로 나중에 연 팝업은 항상 더 큰 순서를 받는다.
        // '스택 깊이(동시 개수)'로 계산하던 이전 방식은, 중간 팝업(확인창 등)이 열렸다 닫히면 count 가 어긋나
        // 나중 팝업이 앞서 열린 팝업과 같은 순서를 받아(충돌) 렌더 순서가 뒤집히는 문제가 있었다. 단조 증가로 이를 없앤다.
        // 레이어가 완전히 비면(_count==0) 오프셋을 0 으로 리셋한다. 다만 비차단 레이어(Light)는 비기 전에 계속 새로 뜰 수 있어
        // 리셋만으로는 위 레이어를 넘을 수 있다. 그래서 상한(_ceiling)이 있는 레이어는 닿기 전에 UIRoot 가 순서를 다시 매긴다.
        public int Reserve() {
            SetActive(true);
            _count++;
            return NextOrder();
        }

        public void Release() {
            _count = Mathf.Max(0, _count - 1);
            if (_count == 0) {
                _offset = 0;
                SetActive(false);
            }
        }

        private void SetActive(bool on) {
            if (_root != null && _root.gameObject.activeSelf != on)
                _root.gameObject.SetActive(on);
        }
    }

    // ── UI VFX ──────────────────────────────────────────────
    public void PlayVfx(GameObject prefab, RectTransform at) {
        if (vfxLayer != null) vfxLayer.Play(prefab, at);
    }

    public void PlayVfx(GameObject prefab, Vector2 screenPoint) {
        if (vfxLayer != null) vfxLayer.Play(prefab, screenPoint);
    }

    // ── 토스트 ──────────────────────────────────────────────
    public void ShowToast(string message, float duration = 2f) => toast?.Show(message, duration);

    public void ShowCombatPowerGain(long before, long after, float duration = 2f) {
        // 미할당이면 아무 일도 일어나지 않아 "전투력이 안 올랐다"와 구분되지 않는다. 원인을 바로 알 수 있게 알린다
        // (시너지 토스트와 같은 이유. 조용히 지나가면 성장 계산을 의심하게 된다).
        if (combatPowerToast == null) {
            Debug.LogWarning("[UIRoot] 전투력 상승 토스트(combatPowerToast)가 할당되지 않아 " +
                             $"{before} -> {after} 상승을 표시하지 못했습니다. UIRoot 인스펙터에 CombatPowerToastView 를 연결하세요.", this);
            return;
        }
        combatPowerToast.Show(before, after, duration);
    }

    public void ShowSynergyActivated(string synergyName, string effectSummary, float duration = 2.5f) {
        // 미할당이면 아무 일도 일어나지 않아 "시너지가 안 걸린 것"과 구분되지 않는다. 원인을 바로 알 수 있게 알린다.
        if (synergyToast == null) {
            Debug.LogWarning($"[UIRoot] 시너지 토스트(synergyToast)가 할당되지 않아 '{synergyName}' 알림을 표시하지 못했습니다. " +
                             "UIRoot 인스펙터에 SynergyToastView 를 연결하세요.", this);
            return;
        }
        synergyToast.Show(synergyName, effectSummary, duration);
    }

    // ── 로딩 오버레이(참조 카운트) ──────────────────────────
    public IDisposable ShowLoading(string message = null) {
        loadingOverlay.Show(message);
        return new LoadingHandle(loadingOverlay);
    }

    private sealed class LoadingHandle : IDisposable {
        private LoadingOverlay _overlay;
        public LoadingHandle(LoadingOverlay overlay) => _overlay = overlay;
        public void Dispose() {
            if (_overlay == null) return;
            _overlay.Hide();
            _overlay = null;   // 중복 Dispose 방어
        }
    }

    // ── 씬 전환 로딩(진행도 + 팁) ───────────────────────────
    // 팝업들이 쓰는 위의 오버레이와 별개 화면이다. 참조 카운트를 쓰지 않는 이유는 씬 전환이
    // 한 번에 하나뿐이기 때문이며, 그래서 진행도라는 "하나의 값"을 가질 수 있다.
    public ISceneLoadingHandle ShowSceneLoading() {
        if (sceneLoadingScreen == null) {
            // 배선이 없으면 씬은 전환되지만 화면이 비어 보인다(원인이 드러나지 않으므로 알린다).
            Debug.LogWarning("[UIRoot] 씬 로딩 화면(sceneLoadingScreen)이 할당되지 않았습니다. " +
                             "UIRoot 인스펙터에 SceneLoadingScreen 을 연결하세요.", this);
            return new NullSceneLoadingHandle();
        }

        sceneLoadingScreen.Show();
        return new SceneLoadingHandle(sceneLoadingScreen);
    }

    private sealed class SceneLoadingHandle : ISceneLoadingHandle {
        private SceneLoadingScreen _screen;
        public SceneLoadingHandle(SceneLoadingScreen screen) => _screen = screen;

        public void Report(float progress01) => _screen?.SetProgress(progress01);

        public void Dispose() {
            if (_screen == null) return;
            _screen.Hide();
            _screen = null;   // 중복 Dispose 방어
        }
    }

    // 배선이 없을 때 쓰는 빈 핸들. 호출부가 null 검사를 하지 않아도 되도록 아무 일도 하지 않는다.
    private sealed class NullSceneLoadingHandle : ISceneLoadingHandle {
        public void Report(float progress01) { }
        public void Dispose() { }
    }
}
