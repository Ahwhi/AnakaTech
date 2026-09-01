using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 씬 전환 추상화. 전환 중 전역 로딩 오버레이(IUIService.ShowLoading)로 입력을 막고 대기한다.
/// 빌트인 씬(Build Settings 에 포함)과 Addressable 씬을 모두 지원한다.
///
/// "로딩 표시 -> 로드 대기 -> 표시 해제" 라는 바깥 흐름은 둘이 완전히 동일하고,
/// 실제 로드 호출(SceneManager vs Addressables)만 다르다. 호출부는 이 차이를 알 필요가 없다.
/// 모두 Single 모드(현재 씬을 새 씬으로 교체)로 비동기 전환한다.
/// </summary>
public interface ISceneService {
    /// <summary>빌드 세팅에 포함된 일반 씬을 이름으로 전환.</summary>
    UniTask LoadAsync(string sceneName, bool showLoading = true, CancellationToken ct = default);

    /// <summary>Addressable 로 받은 씬을 주소(키)로 전환.</summary>
    UniTask LoadAddressableAsync(object key, bool showLoading = true, CancellationToken ct = default);
}
