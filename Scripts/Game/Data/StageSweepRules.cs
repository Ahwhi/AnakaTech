using UnityEngine;

/// <summary>
/// 스테이지 소탕 규칙(클라 표시용 미러). 서버 GameConstants 와 값이 일치해야 한다(판정은 서버 권위).
/// 값이 어긋나면 고를 수는 있는데 서버가 거부하는 상태가 되므로, 서버 상수를 바꿀 때 여기도 함께 고친다.
/// </summary>
public static class StageSweepRules {
    /// <summary>1회 요청으로 쓸 수 있는 최대 장수. 서버 GameConstants.StageSweepMaxCount 와 일치.</summary>
    public const int MaxCount = 9999;

    /// <summary>보유 장수와 1회 상한을 함께 고려한 실제 선택 가능 최대치(최소 1).</summary>
    public static int SelectableMax(int owned) => Mathf.Clamp(owned, 1, MaxCount);
}
