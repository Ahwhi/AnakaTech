using UnityEngine;

/// <summary>
/// 유닛 머리 위에 떠다니는 체력바(+스킬 게이지) 오버레이가 폴링하는 최소 계약.
/// 유닛 프리팹마다 Canvas/Slider 를 붙이지 않고, MainScene 의 공유 캔버스에 있는
/// 풀링된 바가 이 계약만 읽어 위치와 수치를 매 프레임 갱신한다(드로우콜/캔버스 리빌드 절감).
/// </summary>
public interface IUnitBar {
    /// <summary>바가 따라다닐 월드 기준 트랜스폼(이 위로 오프셋만큼 떠 있음).</summary>
    Transform BarAnchor { get; }

    /// <summary>체력 비율(0~1).</summary>
    float HpRatio { get; }

    /// <summary>스킬 게이지 바 표시 여부(스킬이 할당된 유닛만 true. 캐릭터/몬스터 무관).</summary>
    bool HasGauge { get; }

    /// <summary>스킬 게이지 비율(0~1). HasGauge 가 false 면 무시된다.</summary>
    float GaugeRatio { get; }

    /// <summary>바 표시 여부(사망/부활 중이면 false 로 숨긴다).</summary>
    bool BarActive { get; }
}
