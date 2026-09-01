using UnityEngine;

/// <summary>
/// 원격 앱 설정. 어드레서블 Remote_Config 그룹(preload_boot)에 넣어 콘텐츠와 함께 빌드/업로드된다.
/// 따라서 여기 담긴 값은 콘텐츠와 절대 어긋나지 않는다(서버/CDN 별도 관리 불필요).
///
/// contentVersion: 어드레서블 콘텐츠 패치 번호. 콘텐츠 패치 배포 때 이 값을 올린 뒤
/// Groups 창 Build > Update a Previous Build 로 함께 빌드/업로드하면 된다.
/// 클라 표시 버전 v{Application.version}.{contentVersion} 의 뒷자리가 된다.
/// (이후 원격으로 껐다 켜는 플래그 등 다른 원격 튜닝값도 여기에 확장할 수 있다.)
/// </summary>
[CreateAssetMenu(fileName = "RemoteAppConfig", menuName = "Anaka/Remote App Config")]
public sealed class RemoteAppConfig : ScriptableObject {
    [Tooltip("어드레서블 콘텐츠 패치 번호. R2 콘텐츠 배포 때마다 +1. 표시 버전 뒷자리.")]
    [SerializeField] private int contentVersion = 0;

    public int ContentVersion => contentVersion;
}
