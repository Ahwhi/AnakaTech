/// <summary>
/// MainHudContext 로 초기화되는 팝업 계약. 어떤 팝업이든 이 인터페이스를 구현하면, 프리팹(AssetReference)만
/// 할당해 공통 방식으로 열 수 있다. 재화 +버튼(CurrencyView)이 이 계약을 이용해 상점/가챠 등 어느 팝업이든
/// 인스펙터 할당만으로 열도록 한다. 호출부는 ShowPopupAsync 후 Initialize 를 호출하고 ShowAsync 한다.
/// </summary>
public interface IHudPopup {
    void Initialize(MainHudContext ctx);
}
