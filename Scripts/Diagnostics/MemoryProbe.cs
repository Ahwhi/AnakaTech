using System.Diagnostics;

/// <summary>
/// 메모리 계측 호출구. 게임 코드는 이 파사드만 부르고, 실제 계측기는 에디터에서만 존재한다.
///
/// 모든 메서드에 Conditional("UNITY_EDITOR") 이 붙어 있어 빌드에서는 호출문 자체가 컴파일 단계에서
/// 사라진다. 따라서 진단 코드가 출시 빌드에 남지 않고, 게임 코드가 진단 서비스를 주입받을 필요도 없다.
///
/// 계측기 자체는 에디터에서도 기본으로 꺼져 있다. 켜고 끄는 것은 Tools > 아나카 > 메모리 진단 메뉴다.
/// </summary>
public static class MemoryProbe {
    /// <summary>이름표를 붙여 측정 지점을 남긴다. 직전 지점 대비 증가량이 함께 기록된다.</summary>
    [Conditional("UNITY_EDITOR")]
    public static void Mark(string label) {
#if UNITY_EDITOR
        MemoryDiagnostics.Active?.Mark(label);
#endif
    }
}
