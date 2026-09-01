/// <summary>
/// 메인 씬 -> 의뢰 씬으로 넘기는 "어느 의뢰를 시작할지" 요청. 씬이 바뀌면 DI 컨테이너(스코프)도 함께 파괴되므로
/// 씬 경계를 넘는 인자는 컨테이너로 전달할 수 없다. 그래서 이 최소한의 정적 전달 지점을 둔다.
///
/// 규약:
///  - 메인 씬이 Request 로 값을 채우고 의뢰 씬을 로드한다.
///  - 의뢰 씬이 Consume 으로 한 번만 읽어 가고 즉시 비운다(씬을 다시 열었을 때 옛 요청이 되살아나지 않게).
/// 입장권 소모 같은 권위 판정은 요청을 채우기 전에 서버에서 이미 끝나 있어야 한다.
/// </summary>
public static class CommissionSession {
    private static bool _hasRequest;
    private static CommissionType _type;
    private static int _stage;

    /// <summary>시작할 의뢰를 예약한다(메인 씬에서 호출).</summary>
    public static void Request(CommissionType type, int stage) {
        _type = type;
        _stage = stage;
        _hasRequest = true;
    }

    /// <summary>예약된 의뢰를 꺼내고 비운다. 예약이 없으면 false(의뢰 씬을 직접 열어 본 경우 등).</summary>
    public static bool Consume(out CommissionType type, out int stage) {
        type = _type;
        stage = _stage;
        bool had = _hasRequest;
        _hasRequest = false;
        return had;
    }

    /// <summary>남은 예약을 버린다(진입 실패/취소 시).</summary>
    public static void Clear() => _hasRequest = false;
}
