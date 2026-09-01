using System;
using UnityEngine;

/// <summary>
/// 순위 구간별 텍스트 색을 매칭하는 카탈로그(SO). 순위를 출력하는 모든 곳(RankText)이 이 색을 참조한다.
/// 구간을 몇 개로 나눌지도 이 에셋에서 정하므로, 등급 구간을 바꾸거나 늘려도 코드는 손대지 않는다
/// (등급 색을 다루는 GradeColorCatalog 와 같은 방식이다).
///
/// 항목은 "이 순위까지"를 뜻하는 상한으로 적고 높은 순위부터 차례로 둔다. 예를 들어 1등 / 2~10등 / 11등 이하는
///   maxRank = 1   (1등)
///   maxRank = 10  (2~10등)
///   maxRank = 0   (나머지 전부. 0 이하는 상한 없음을 뜻한다)
/// 순서대로 훑어 처음 걸리는 구간의 색을 쓰므로, 마지막에 상한 없는 항목을 하나 두면 빈 구간이 생기지 않는다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Rank Color Catalog", fileName = "RankColorCatalog")]
public sealed class RankColorCatalog : ScriptableObject {
    [Serializable]
    public struct Tier {
        [Tooltip("이 구간의 마지막 순위. 0 이하면 상한 없음(그 아래 전부)을 뜻하며 보통 마지막 항목에 쓴다.")]
        public int maxRank;
        public Color color;
    }

    [Tooltip("순위 구간. 높은 순위부터 차례로 둔다(예: 1 / 10 / 0). 위에서부터 처음 걸리는 구간의 색을 쓴다.")]
    [SerializeField] private Tier[] tiers = {
        new() { maxRank = 1, color = new Color(1f, 0.84f, 0.25f) },      // 1등
        new() { maxRank = 10, color = new Color(0.75f, 0.82f, 1f) },     // 2~10등
        new() { maxRank = 0, color = Color.white },                       // 11등 이하
    };

    [Tooltip("순위권 밖(순위 없음)일 때의 색.")]
    [SerializeField] private Color noRankColor = new(0.6f, 0.6f, 0.6f);

    /// <summary>
    /// 순위에 맞는 색. 어느 구간에도 걸리지 않으면 false 를 돌려주고, 그때 표시부는 색을 건드리지 않는다
    /// (카탈로그가 비어 있어도 프리팹에 지정된 색이 그대로 남는다).
    /// </summary>
    public bool TryGetColor(int rank, out Color color) {
        if (rank <= 0) {
            color = noRankColor;
            return true;
        }
        if (tiers != null) {
            foreach (var t in tiers) {
                if (t.maxRank > 0 && rank > t.maxRank) continue;
                color = t.color;
                return true;
            }
        }
        color = default;
        return false;
    }
}
