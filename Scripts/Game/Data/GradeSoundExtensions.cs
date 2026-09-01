using static Enums;

/// <summary>
/// 등급(Grade) -> 사운드 키 매핑. 가챠 카드 공개음처럼 "등급마다 다른 소리"가 필요한 화면이 공통으로 쓴다.
/// 캐릭터 가챠와 장비 가챠가 같은 규칙을 보게 해 두면, 등급이 늘거나 소리 정책이 바뀌어도 고칠 곳이 한 곳이다.
///
/// 실제 클립은 SoundLibrary 에서 키마다 배정한다. 배정하지 않은 등급은 무음이므로,
/// 상위 등급에만 클립을 넣어 임팩트만 주는 운용도 그대로 가능하다.
/// </summary>
public static class GradeSoundExtensions {
    public static SfxKey GachaRevealSfx(this Grade grade) => grade switch {
        Grade.XR => SfxKey.GachaRevealXR,
        Grade.SSR => SfxKey.GachaRevealSSR,
        Grade.SR => SfxKey.GachaRevealSR,
        Grade.R => SfxKey.GachaRevealR,
        _ => SfxKey.GachaRevealN
    };
}
