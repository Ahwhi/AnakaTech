using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 입장/도전 버튼의 비용 표시(공용). 무료면 무료 표시를, 유료면 재화 아이콘 + 비용 묶음을 켠다.
/// 비용과 재화 종류는 호출측이 넘기므로 컨텐츠를 가리지 않는다(멸망전 시작/재도전 등).
///
/// 재화 아이콘 스프라이트는 재화 카탈로그에서 가져오므로 인스펙터에 따로 넣지 않는다(CurrencyIcon 규칙).
/// </summary>
public sealed class EntryCostView : MonoBehaviour {
    [Tooltip("무료일 때 켜는 표시(예: '무료' 라벨).")]
    [SerializeField] private GameObject freeLabel;
    [Tooltip("유료일 때 켜는 묶음. 아래 아이콘과 비용 문구를 이 안에 둔다.")]
    [SerializeField] private GameObject paidGroup;
    [Tooltip("비용 재화 아이콘. 스프라이트는 재화 카탈로그에서 가져오므로 비워 둔다.")]
    [SerializeField] private Image costIcon;
    [Tooltip("비용 숫자.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("재화가 모자랄 때 비용 문구에 적용할 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    private Color? _costBaseColor;

    /// <param name="cost">이번 비용. 0 이하면 무료로 표시한다.</param>
    /// <param name="affordable">보유 재화로 낼 수 있는지(모자라면 비용 문구 색을 바꾼다).</param>
    public void Bind(CurrencyCatalog currencies, CurrencyType currency, int cost, bool affordable) {
        bool paid = cost > 0;
        if (freeLabel != null) freeLabel.SetActive(!paid);
        if (paidGroup != null) paidGroup.SetActive(paid);
        if (!paid) return;

        CurrencyIcon.Apply(costIcon, currencies, currency);
        if (costText != null) {
            if (_costBaseColor == null) _costBaseColor = costText.color;
            costText.text = cost.ToString("N0");
            costText.color = affordable ? _costBaseColor.Value : insufficientColor;
        }
    }

    /// <summary>비용을 아직 모를 때(응답 대기 중) 무료/유료 표시를 모두 끈다.</summary>
    public void Clear() {
        if (freeLabel != null) freeLabel.SetActive(false);
        if (paidGroup != null) paidGroup.SetActive(false);
    }
}
