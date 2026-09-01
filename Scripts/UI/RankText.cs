using TMPro;
using UnityEngine;

/// <summary>
/// 순위를 텍스트로 출력하는 공용 컴포넌트. 순위 문구와 구간별 색(RankColorCatalog)을 함께 다뤄,
/// 프로필 팝업이든 랭킹 목록이든 "3위"의 모양이 한 곳에서 정해진다(등급 표시의 GradeText 와 같은 역할).
///
/// 카탈로그는 앱 시작 시 SetDefaultCatalog 로 1회 등록되므로 개별 프리팹마다 SO 를 지정하지 않아도 된다
/// (특정 화면만 다른 팔레트를 쓰고 싶으면 catalog 를 직접 지정하면 그 값이 우선한다).
/// </summary>
public sealed class RankText : MonoBehaviour {
    [Tooltip("순위를 출력할 텍스트. 비우면 같은 오브젝트의 TMP_Text 를 사용.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("체크 시 순위 구간별 색(RankColorCatalog)을 적용. 미체크면 색을 건드리지 않는다.")]
    [SerializeField] private bool applyColor = true;
    [Tooltip("체크하면 '3위' 대신 숫자만 출력한다. 열 제목이 이미 순위인 목록(랭킹 슬롯)에서 쓴다.")]
    [SerializeField] private bool numberOnly;
    [Tooltip("색 소스(SO). 비우면 앱이 등록한 기본 카탈로그를 사용.")]
    [SerializeField] private RankColorCatalog catalog;

    // 앱 시작 시(LifetimeScope) 1회 등록되는 기본 카탈로그. 개별 프리팹에 SO 를 매번 할당하지 않아도 되게 한다.
    private static RankColorCatalog _default;
    public static void SetDefaultCatalog(RankColorCatalog c) => _default = c;

    private TMP_Text Label => label != null ? label : (label = GetComponent<TMP_Text>());

    /// <summary>순위를 표시하고, applyColor 시 구간 색을 적용한다. 0 이면 순위권 밖으로 표시된다.</summary>
    public void SetRank(int rank) {
        var t = Label;
        if (t == null) return;

        t.text = numberOnly ? RankingRules.RankNumberText(rank) : RankingRules.RankText(rank);
        if (!applyColor) return;

        var cat = catalog != null ? catalog : _default;
        if (cat != null && cat.TryGetColor(rank, out var col)) t.color = col;
    }

    /// <summary>
    /// 아직 순위를 모를 때(조회 전) 표시할 자리 문구. 순위가 아니라 대기 상태이므로 구간 색은 적용하지 않는다.
    /// </summary>
    public void SetPlaceholder(string text = "-") {
        var t = Label;
        if (t != null) t.text = text;
    }
}
