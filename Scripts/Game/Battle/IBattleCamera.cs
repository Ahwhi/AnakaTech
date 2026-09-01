using UnityEngine;

/// <summary>
/// 전투 카메라 추상화. 대표 캐릭터 추적 + 줌 + 타격 흔들림(PLANNING 1-10,20).
/// Cinemachine 설치 시 Cinemachine 기반 구현으로 교체할 수 있도록 인터페이스로 분리한다.
/// </summary>
public interface IBattleCamera {
    void SetTarget(Transform target);
    void Shake();

    /// <summary>
    /// 현재 목표 줌(오쏘 사이즈). 설정하면 카메라가 허용 범위로 잘라 적용한다.
    /// 저장/복원은 이 값 하나로 이뤄지므로, 구현체가 바뀌어도 보관 규약은 그대로다.
    /// </summary>
    float Zoom { get; set; }
}
