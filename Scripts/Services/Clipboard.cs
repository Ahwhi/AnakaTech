using UnityEngine;

/// <summary>
/// 시스템 클립보드 복사(플랫폼 공용). UID/쿠폰코드처럼 사용자가 밖으로 가져가야 하는 문자열을 복사할 때 쓴다.
///
/// GUIUtility.systemCopyBuffer 는 에디터/Windows/Android/iOS 에서 모두 OS 클립보드에 연결된다.
/// 플랫폼 분기가 필요해지면(예: WebGL 지원 추가) 호출부를 고치지 않도록 여기 한 곳만 바꾸면 된다.
/// </summary>
public static class Clipboard {
    /// <summary>클립보드에 문자열을 넣는다. 비어 있으면 아무 것도 하지 않고 false 를 돌려준다.</summary>
    public static bool Copy(string text) {
        if (string.IsNullOrEmpty(text)) return false;
        GUIUtility.systemCopyBuffer = text;
        return true;
    }
}
