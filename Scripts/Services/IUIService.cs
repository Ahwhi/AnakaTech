using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 공용 팝업이 올라갈 Canvas 레이어 구분.
///   Popup : 일반 상호작용 팝업(SortOrder 100).
///   Light : 다른 팝업 뒤에 가볍게 띄우는 비차단 연출용(SortOrder 50). 예: 던전 클리어 획득 표시.
/// </summary>
public enum UIPopupLayer { Popup, Light }

/// <summary>
/// 전역 UI 표시 계약. "어떤 프리팹을 어느 Canvas 레이어에 띄우는지"를 캡슐화해
/// 호출부는 배치 세부(프리팹/하이어라키/레이어)를 모른 채 의도만 표현하면 된다.
///   - ShowPopupAsync : 공용 팝업(인스펙터 할당 AssetReference 프리팹)을 띄우고 뷰를 반환
///   - ShowToast      : 짧은 안내 메시지(자동 소멸)
///   - ShowLoading    : 전역 로딩 오버레이(참조 카운트). 반환된 핸들을 Dispose 하면 닫힌다.
/// 컴포지션 루트(GameLifetimeScope)가 구현체(UIRoot)를 IUIService 로 등록한다. 소비자는 IUIService 를 주입받아 사용.
/// </summary>
public interface IUIService {
    /// <summary>현재 떠 있는(로드 중 포함) 공용 팝업이 하나라도 있는지. 팝업 위 입력을 게임 입력과 구분할 때 참고한다.</summary>
    bool HasOpenPopup { get; }

    /// <summary>전역 로딩 오버레이가 떠 있는지. 화면을 덮고 입력을 막는 동안 전역 입력을 무시할 때 참고한다.</summary>
    bool IsLoading { get; }

    /// <summary>
    /// 가장 위에 떠 있는 팝업에 안드로이드 뒤로가기를 전달한다(전달했으면 true).
    /// 무엇을 할지는 팝업이 정하며(UIView.OnBackKey) 보통은 자신을 닫는다.
    /// false 면 닫을 팝업이 없다는 뜻이므로, 호출부가 그 상황에 맞는 처리(게임 종료 확인 등)를 한다.
    /// </summary>
    bool TryHandleBackKey();

    /// <summary>
    /// 팝업이 올라갈 Canvas 레이어. Popup(기본, SortOrder 100)은 일반 상호작용 팝업이고,
    /// Light(SortOrder 50)는 던전 클리어 획득 연출처럼 다른 팝업을 가리지 않고 뒤에 가볍게 띄우는 용도다.
    /// </summary>
    UniTask<T> ShowPopupAsync<T>(AssetReference reference, CancellationToken ct = default, UIPopupLayer layer = UIPopupLayer.Popup) where T : UIView;
    /// <summary>떠 있는 모든 공용 팝업을 즉시 닫는다(예: 연결 끊김 시 정리).</summary>
    void CloseAllPopups();
    void ShowToast(string message, float duration = 2f);

    /// <summary>총 전투력 상승 토스트(상승분 + 바뀐 값, 숫자 카운트업). before 이하면 표시하지 않는다.</summary>
    void ShowCombatPowerGain(int before, int after, float duration = 2f);

    /// <summary>
    /// 편성 시너지 활성화 토스트(일반 안내 토스트와 별개 자리/연출). 새로 켜진 시너지에만 쓴다.
    /// 시너지 조건은 유저가 직접 찾아내야 하는 정보라, 실패 안내와 같은 토스트에 섞이면 발견의 신호가 묻힌다.
    /// 여러 개가 한꺼번에 켜지면 차례대로 표시된다.
    /// </summary>
    void ShowSynergyActivated(string synergyName, string effectSummary, float duration = 2.5f);
    IDisposable ShowLoading(string message = null);

    /// <summary>
    /// 씬 전환 전용 전체 화면 로딩(진행도 + 팁). 팝업들이 쓰는 ShowLoading 과 별개 화면이라
    /// 어느 한쪽을 고쳐도 다른 쪽 표시가 바뀌지 않는다.
    /// 반환된 핸들에 진행도를 알려 주고, 다 끝나면 Dispose 한다.
    /// </summary>
    ISceneLoadingHandle ShowSceneLoading();

    /// <summary>대상 UI 요소 위치에 UI 파티클 VFX 프리팹을 재생한다(전역 VFX 레이어에 풀링). prefab 이 null 이면 무시.</summary>
    void PlayVfx(GameObject prefab, RectTransform at);

    /// <summary>지정 화면 좌표에 UI 파티클 VFX 프리팹을 재생한다(움직이는 대상의 특정 지점 등). prefab 이 null 이면 무시.</summary>
    void PlayVfx(GameObject prefab, Vector2 screenPoint);
}

/// <summary>씬 전환 로딩 화면의 조작 창구. Dispose 하면 화면이 닫힌다.</summary>
public interface ISceneLoadingHandle : IDisposable {
    /// <summary>진행도(0~1)를 알린다. 로딩이 알려 주는 값을 그대로 넘기면 된다.</summary>
    void Report(float progress01);
}
