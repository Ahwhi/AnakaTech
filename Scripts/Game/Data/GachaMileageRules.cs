using UnityEngine;

/// <summary>
/// 모집 마일리지 규칙(표시/미리보기 미러). 서버 Server.Data.ShopCatalog 의 같은 값과 반드시 일치해야 한다.
/// 실제 적립과 차감은 전부 서버가 확정하며, 여기는 게이지와 버튼 상태를 그리기 위한 값이다.
///
/// 마일리지는 모집을 돌리면 소녀든 장비든 가리지 않고 쌓인다. 다 모으면 로테이션에 걸리지 않은 SSR 소녀까지
/// 원하는 대상을 직접 받을 수 있어, 픽업 차례를 기다릴 수밖에 없던 구조에 바닥을 만들어 준다.
/// </summary>
public static class GachaMileageRules {
    /// <summary>픽업 1회에 드는 마일리지. 서버 ShopCatalog.MileagePickCost 와 같아야 한다.</summary>
    public const int PickCost = 20000;

    /// <summary>일반 모집 1회 적립량(서버 ShopCatalog.MileagePerNormalPull).</summary>
    public const int PerNormalPull = 1;

    /// <summary>특수 모집 1회 적립량(서버 ShopCatalog.MileagePerSpecialPull).</summary>
    public const int PerSpecialPull = 100;

    /// <summary>픽업으로 고를 수 있는 소녀의 등급. 로테이션 픽업 대상과 같은 등급이다.</summary>
    public const Grade PickGrade = Grade.SSR;

    /// <summary>지금 픽업할 수 있는지.</summary>
    public static bool CanPick(int mileage) => mileage >= PickCost;

    /// <summary>
    /// 게이지 채움 비율(0~1). 한 번 쓸 분량을 가득 찬 것으로 본다.
    /// 여러 번 쓸 만큼 모은 유저도 게이지는 가득 찬 채로 멈추고, 정확한 양은 숫자가 알려 준다
    /// (게이지를 2회분까지 늘리면 "다음 한 번까지 얼마 남았나"를 읽기 어려워진다).
    /// </summary>
    public static float Fill(int mileage) => Mathf.Clamp01((float)mileage / PickCost);

    /// <summary>게이지 옆에 붙는 문구. 보유량은 있는 그대로 보여 준다(상한으로 잘라 표시하지 않는다).</summary>
    public static string Describe(int mileage) => $"{mileage:N0} / {PickCost:N0}";
}
