using UnityEngine;

/// <summary>
/// 재화 패널(PLANNING 2-2). 자신이 품은 재화 위젯(CurrencyView)들을 일괄 초기화하는 얇은 컨테이너다.
/// 어떤 재화를 몇 개 보여줄지는 자식으로 배치한 CurrencyView 의 개수/타입으로 정해지므로(골드/다이아/기력 등),
/// HUD 든 상점이든 어느 팝업에서든 필요한 재화만 골라 담아 재사용할 수 있다. 순수 뷰(스토어만 구독).
/// </summary>
public sealed class CurrencyPanel : MonoBehaviour {
    [Tooltip("표시할 재화 위젯. 비우면 자식에서 자동 수집(비활성 포함).")]
    [SerializeField] private CurrencyView[] views;

    public void Initialize(MainHudContext ctx) {
        if (views == null || views.Length == 0)
            views = GetComponentsInChildren<CurrencyView>(true);

        foreach (var v in views)
            if (v != null) v.Initialize(ctx);
    }
}
