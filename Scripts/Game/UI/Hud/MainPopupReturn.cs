using UnityEngine.AddressableAssets;

/// <summary>
/// 다른 씬을 다녀온 뒤 메인 화면에서 다시 열 팝업 예약(CommissionSession 과 같은 성격의 정적 전달 지점).
///
/// 왜 필요한가: UIRoot 는 부트 오브젝트 밑에 있어 씬이 바뀌어도 살아남는다. 그래서 전투 씬으로 넘어가기 전에
/// 열려 있던 팝업을 닫지 않으면 전투 화면을 가린다. 그런데 그냥 닫아 버리면 돌아왔을 때 유저가 보던 자리를
/// 잃으므로, 닫기 직전에 "돌아오면 여기를 다시 열어 달라"를 남겨 둔다.
///
/// 씬이 바뀌면 DI 컨테이너도 함께 파괴되어 인자를 넘길 수 없으므로 정적으로 둔다.
/// 특정 기능에 묶지 않고 팝업 참조만 받으므로 다른 컨텐츠도 그대로 쓸 수 있다.
///
/// 규약:
///  - 씬을 떠나는 쪽이 Request 로 예약하고 팝업을 닫는다.
///  - 메인 HUD 가 Consume 으로 한 번만 읽어 가고 즉시 비운다(다음 진입에 옛 예약이 되살아나지 않게).
/// </summary>
public static class MainPopupReturn {
    /// <summary>탭 지정 없음.</summary>
    public const int NoTab = -1;

    private static AssetReference _popup;
    private static int _tabIndex = NoTab;

    /// <param name="popup">다시 열 팝업(UIPopupCatalog 의 참조를 그대로 넘긴다).</param>
    /// <param name="tabIndex">탭이 있는 팝업이면 복귀 시 열 탭. 없으면 NoTab.</param>
    public static void Request(AssetReference popup, int tabIndex = NoTab) {
        _popup = popup;
        _tabIndex = tabIndex;
    }

    /// <summary>예약을 꺼내고 비운다. 예약이 없으면 false.</summary>
    public static bool Consume(out AssetReference popup, out int tabIndex) {
        popup = _popup;
        tabIndex = _tabIndex;
        bool had = _popup != null && _popup.RuntimeKeyIsValid();
        _popup = null;
        _tabIndex = NoTab;
        return had;
    }

    /// <summary>남은 예약을 버린다(로그아웃/연결 끊김 등 메인으로 정상 복귀하지 않는 경우).</summary>
    public static void Clear() {
        _popup = null;
        _tabIndex = NoTab;
    }
}

/// <summary>
/// 탭이 있는 팝업이 특정 탭으로 열릴 수 있게 하는 선택 구현.
/// 전투 씬을 다녀온 뒤 보던 탭으로 되돌리는 데 쓴다(MainPopupReturn).
/// </summary>
public interface IHudPopupTab {
    /// <summary>지정한 탭을 연다. 범위를 벗어난 값은 구현이 알아서 무시한다.</summary>
    void SelectTab(int index);
}
