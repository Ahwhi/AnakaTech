using UnityEngine;

/// <summary>
/// 직접 조작 이동 입력의 계약. 유닛은 이 값 하나만 읽고 입력이 어디서 왔는지(터치 조이스틱/키보드)는 모른다.
/// 크기는 0~1 이며 0 이면 멈춘다(조이스틱을 덜 밀면 1 보다 작아 천천히 걷는다).
/// </summary>
public interface IUnitMoveInput {
    Vector2 Direction { get; }
}
