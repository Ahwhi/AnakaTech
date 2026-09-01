using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 별레벨(0~MaxStarLevel)을 별 5칸으로 표시하는 공용 컴포넌트.
/// 별은 항상 5칸이 채워지며(빈 칸은 회색별), 5레벨마다 색상 티어가 한 단계 올라간다.
///   1~5 : 노란별을 왼쪽부터 레벨 수만큼 채우고 나머지는 회색별.
///   6~10: 왼쪽부터 노란별 하나씩 분홍별로 승격(6=분홍1, 10=분홍5).
///   11~15: 왼쪽부터 분홍별 하나씩 파란별로 승격(11=파랑1, 15=파랑5).
/// 색상이 높은(승격된) 별이 항상 왼쪽에 온다. 예) 1레벨=노-회-회-회-회, 12레벨=파-파-핑-핑-핑.
/// 덱 카드/상세 패널 등 별 표시가 필요한 곳에서 공통으로 사용한다.
/// </summary>
public sealed class StarLevelView : MonoBehaviour {
    // 별 슬롯 수(한 색상 티어가 가득 차는 단위). MaxStarLevel = Slots x 색상 티어 수(3) = 15 와 맞물린다.
    private const int Slots = 5;

    [Tooltip("별을 채울 그리드 컨테이너(GridLayoutGroup 권장). 별 5칸이 들어갈 크기로 구성한다.")]
    [SerializeField] private Transform starGrid;
    [Tooltip("별 1개 프리팹(Image). 스프라이트와 색상은 이 뷰가 티어별로 지정한다.")]
    [SerializeField] private Image starPrefab;
    [Tooltip("별 스타일 모음(StarSpriteCatalog). 아트 교체를 한 곳에서 하기 위해 프리팹마다 두지 않고 에셋으로 뺐다.")]
    [SerializeField] private StarSpriteCatalog sprites;

    private readonly List<Image> _stars = new();

    /// <summary>별레벨(0~MaxStarLevel)을 5칸의 색상으로 반영한다. 별을 재사용해 할당을 최소화한다.</summary>
    public void SetStarLevel(int starLevel) {
        if (starGrid == null || starPrefab == null) return;

        int level = Mathf.Clamp(starLevel, 0, GrowthRules.MaxStarLevel);

        for (int i = 0; i < Slots; i++) {
            Image star = i < _stars.Count ? _stars[i] : CreateStar();
            // 왼쪽 슬롯(i=0)일수록 먼저(낮은 레벨에) 채워지고 상위 색상으로 승격된다.
            // 슬롯 i 는 레벨이 (i+1) 이상이면 1티어, 한 칸(Slots)씩 더 오를 때마다 티어가 하나씩 올라간다.
            // 티어 수를 상수로 박지 않아 색상 티어가 늘어도 이 계산은 그대로 쓴다.
            int tier = 0;
            while (level >= i + 1 + tier * Slots) tier++;

            var style = sprites != null ? sprites.Get(tier) : null;
            if (style != null) {
                // 스프라이트가 비어 있으면 프리팹에 지정된 이미지를 그대로 두고 색상만 반영한다.
                if (style.Sprite != null) star.sprite = style.Sprite;
                star.color = style.Color;
            }
            star.gameObject.SetActive(true);
        }
        // 과거 구현이 5칸보다 많이 만들었을 수 있으므로 남는 별은 숨긴다(재사용).
        for (int i = Slots; i < _stars.Count; i++)
            _stars[i].gameObject.SetActive(false);
    }

    private Image CreateStar() {
        var star = Instantiate(starPrefab, starGrid);
        _stars.Add(star);
        return star;
    }
}
