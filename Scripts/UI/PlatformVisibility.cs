using System;
using UnityEngine;
using static Enums;

/// <summary>
/// 지정한 플랫폼에서는 이 오브젝트를 아예 꺼 두는 범용 컴포넌트. 버튼, 메뉴, 안내 문구 등 어디에나 붙인다.
///
/// 스토어 정책상 특정 플랫폼에서 제공하지 않는 기능(예: iOS 의 쿠폰 입력)을 그 플랫폼 빌드에서만 빼기 위해 쓴다.
/// 판정은 빌드 플랫폼으로만 하며 원격 설정이나 서버 값으로 바뀌지 않는다. 그 플랫폼에서는 언제나 없는 기능이다.
/// </summary>
public sealed class PlatformVisibility : MonoBehaviour {
    [Tooltip("이 플랫폼들에서는 오브젝트를 비활성화한다.")]
    [SerializeField] private PlatformType[] hiddenOn = Array.Empty<PlatformType>();

    private void Awake() {
        if (Array.IndexOf(hiddenOn, CurrentPlatform.Value) >= 0)
            gameObject.SetActive(false);
    }
}
