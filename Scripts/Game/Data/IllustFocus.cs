using System;
using UnityEngine;

/// <summary>
/// 일러스트 프레이밍 파라미터(관심점 + 크기 배율). "이 그림의 어디를, 얼마나 크게 잡을 것인가" 를
/// 데이터(ScriptableObject) 쪽에 두기 위한 값 묶음이다.
///
/// 역할 분담:
///  - 프리팹(BattleUnitIllustration) : 슬롯 규격(프레이밍 창의 크기/중심/기준 배율/앵커). 여러 개체가 공유한다.
///  - 데이터(이 구조체)              : 개체별 차이(관심점, 창 대비 크기 배율).
/// 덕분에 하나의 본체 프리팹을 여러 개체가 공유하면서도 개체마다 다른 크기/구도를 낼 수 있고,
/// 반대로 창 규격을 바꿔도 개체 데이터는 건드릴 필요가 없다.
///
/// 정규화 좌표와 배율만 다루므로 원본 픽셀수/PPU 에 무관하다. 스프라이트를 교체해도 같은 부위가 잡힌다.
/// </summary>
[Serializable]
public struct IllustFocus {
    [Tooltip("일러스트 내 관심점(보통 얼굴 중심)의 정규화 좌표. (0,0)=좌하단, (1,1)=우상단. 이 점이 프레이밍 창의 앵커 위치에 놓인다.")]
    public Vector2 Point;

    [Tooltip("프레이밍 창 대비 크기 배율. 프리팹의 기준 배율에 곱해진다. 1=프리팹 기준 그대로, 키우면 크게(클로즈업), 줄이면 작게.")]
    public float CoverScale;

    /// <summary>새 에셋의 기본값(중앙 관심점, 프리팹 기준 배율 그대로).</summary>
    public static IllustFocus Default => new() { Point = new Vector2(0.5f, 0.5f), CoverScale = 1f };

    /// <summary>
    /// 실제 계산에 쓰는 크기 배율. 이 필드가 추가되기 전에 만들어진 에셋은 CoverScale 이 0 으로 역직렬화되어
    /// 스케일 0(= 화면에서 사라짐)이 되므로, 미설정 값은 기준 배율 1 로 본다.
    /// </summary>
    public float SafeCoverScale => CoverScale > 0f ? CoverScale : 1f;
}
