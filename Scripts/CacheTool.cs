using UnityEngine;

/// <summary>
/// 개발용 캐시 도구. 어드레서블이 원격(R2 등)에서 받아 로컬에 캐시한 에셋 번들을 삭제한다.
/// 캐시를 비우면 다음 실행 때 다운로드가 다시 강제되므로, 다운로드 흐름을 반복 테스트할 때 사용한다.
/// 인스펙터 버튼(CacheToolEditor)에서 실행하며, 플레이를 멈춘 상태에서 눌러야 안전하다.
/// </summary>
public class CacheTool : MonoBehaviour {
    // 현재 캐시가 차지한 용량(바이트). 에디터 인스펙터에서 표시용으로 읽는다.
    public long CachedBytes => Caching.currentCacheForWriting.spaceOccupied;

    // 캐시된 번들을 전부 삭제한다. 성공하면 다음 로드 때 재다운로드가 일어난다.
    public void ClearBundleCache() {
        long before = CachedBytes;
        bool ok = Caching.ClearCache();

        if (ok)
            Debug.Log($"[CacheTool] 캐시 삭제 완료. 약 {before / (1024f * 1024f):0.0} MB 정리됨. 다음 실행 때 재다운로드됩니다.");
        else
            Debug.LogWarning("[CacheTool] 캐시 삭제 실패. 사용 중인 번들이 있으면 플레이를 종료한 뒤 다시 시도하세요.");
    }
}
