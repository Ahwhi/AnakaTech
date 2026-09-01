using System;
using UnityEngine;

/// <summary>
/// 별 한 칸의 표시 정보(스프라이트 + 색상). 색상은 알파를 포함하므로 반투명 별도 표현할 수 있다.
/// 스프라이트와 색상을 한 묶음으로 두어 티어를 추가할 때 배열 두 개의 순서가 어긋나는 일을 막는다.
/// </summary>
[Serializable]
public sealed class StarStyle {
    [Tooltip("별 스프라이트. 비우면 상위(카탈로그)에서 빈 칸 스프라이트로 대체한다.")]
    [SerializeField] private Sprite sprite;
    [Tooltip("Image에 곱해질 색상. 알파로 투명도를 조절한다.")]
    [SerializeField] private Color color = Color.white;

    public Sprite Sprite => sprite;
    public Color Color => color;
}

/// <summary>
/// 별 표시용 스타일 모음(StarLevelView 공용). 별 이미지가 프리팹마다 흩어져 있으면
/// 아트를 바꿀 때 별을 쓰는 모든 프리팹을 찾아다녀야 하므로, 스프라이트와 색상을 이 에셋 하나로 모은다.
///
/// 색상 티어는 배열로 두어 티어가 늘어도 코드를 고치지 않는다
/// (티어를 추가할 때는 이 배열에 스타일을 넣고 GrowthRules.MaxStarLevel 만 맞추면 된다).
/// </summary>
[CreateAssetMenu(fileName = "StarSpriteCatalog", menuName = "Anaka/Star Sprite Catalog")]
public sealed class StarSpriteCatalog : ScriptableObject {
    [Tooltip("빈 칸(회색) 스타일. 아직 올리지 않은 별에 쓴다.")]
    [SerializeField] private StarStyle empty = new();
    [Tooltip("색상 티어 스타일을 낮은 티어부터 순서대로(노랑 -> 분홍 -> 파랑).")]
    [SerializeField] private StarStyle[] tiers;

    /// <summary>정의된 색상 티어 수(빈 칸은 포함하지 않는다).</summary>
    public int TierCount => tiers != null ? tiers.Length : 0;

    /// <param name="tier">0 = 빈 칸, 1 이상 = 색상 티어. 정의된 티어보다 크면 가장 높은 티어로 맞춘다.</param>
    /// <returns>표시에 쓸 스타일. 티어 스프라이트가 비어 있으면 빈 칸 스타일로 대체한다.</returns>
    public StarStyle Get(int tier) {
        if (tier <= 0 || tiers == null || tiers.Length == 0) return empty;
        int index = Mathf.Min(tier, tiers.Length) - 1;
        var style = tiers[index];
        return style != null && style.Sprite != null ? style : empty;
    }
}
