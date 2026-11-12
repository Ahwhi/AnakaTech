using UnityEngine;

/// <summary>
/// 원격 경로표(ServerRouteTable) 설정(ScriptableObject). 경로표 주소와 조회 조건을 인스펙터에서 할당한다.
///
/// 경로표 조회가 실패해도 부트는 멈추지 않고 인스펙터 기본 접속 값(라이브)으로 진행한다.
/// CDN 이 잠깐 흔들렸다고 라이브 이용자 전원이 접속하지 못하는 일은 없어야 하기 때문이다.
///
/// 생성: 프로젝트 창에서 Create > Anaka > Server Route Config.
/// </summary>
[CreateAssetMenu(fileName = "ServerRouteConfig", menuName = "Anaka/Server Route Config")]
public sealed class ServerRouteConfig : ScriptableObject {
    [Tooltip("경로표 JSON 주소. 예: https://cdn.ahwhistudio.com/config/server_routes.json")]
    [SerializeField] private string tableUrl;

    [Tooltip("경로표 조회 제한 시간(초). 넘기면 기본 접속 값으로 진행한다.")]
    [SerializeField] private int timeoutSeconds = 3;

    [Tooltip("에디터에서도 경로표를 따를지. 끄면 에디터는 항상 NetworkManager 인스펙터 값(개발 서버 프로필 등)으로 접속한다.")]
    [SerializeField] private bool applyInEditor;

    public string TableUrl => tableUrl?.Trim();
    public int TimeoutSeconds => Mathf.Max(1, timeoutSeconds);

    /// <summary>이번 실행에서 경로표를 조회해야 하는지.</summary>
    public bool IsActive {
        get {
            if (string.IsNullOrEmpty(TableUrl)) return false;
#if UNITY_EDITOR
            return applyInEditor;
#else
            return true;
#endif
        }
    }
}
