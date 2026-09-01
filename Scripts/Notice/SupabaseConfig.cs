using UnityEngine;

/// <summary>
/// Supabase 접속 설정(ScriptableObject). 프로젝트 URL 과 공개용 anon 키를 인스펙터에서 할당한다.
/// 코드에 키를 하드코딩하지 않고 에셋으로 분리해, 환경(개발/운영) 교체나 키 회전이 쉽도록 한다.
///
/// 주의: 여기에는 반드시 'anon public' 키만 넣는다. 'service_role'(secret) 키는 모든 보안 규칙을
/// 무시하므로 클라이언트에 절대 포함하면 안 된다(추출 시 DB 전체 조작 위험).
///
/// 생성: 프로젝트 창에서 Create > Anaka > Supabase Config.
/// </summary>
[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "Anaka/Supabase Config")]
public sealed class SupabaseConfig : ScriptableObject {
    [Tooltip("프로젝트 URL. 예: https://xxxxxxxx.supabase.co")]
    [SerializeField] private string projectUrl;

    [Tooltip("anon public 키(공개용). service_role 키는 절대 넣지 말 것.")]
    [TextArea(2, 4)]
    [SerializeField] private string anonKey;

    /// <summary>끝의 슬래시를 제거한 프로젝트 URL.</summary>
    public string ProjectUrl => string.IsNullOrEmpty(projectUrl) ? projectUrl : projectUrl.TrimEnd('/');

    public string AnonKey => anonKey;
}
