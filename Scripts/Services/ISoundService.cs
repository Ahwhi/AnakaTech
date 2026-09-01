using UnityEngine;
using static Enums;

/// <summary>
/// 사운드 재생/볼륨 제어 추상화. 키(enum) 기반 재생과 직접 클립 재생을 모두 제공한다.
/// 소비자는 SoundManager 구현체가 아니라 이 계약에만 의존한다.
/// </summary>
public interface ISoundService {
    /// <summary>전체 볼륨(0~1). BGM/SFX 볼륨에 곱해져 최종 출력이 되므로, 개별 값을 건드리지 않고 전체만 줄일 수 있다.</summary>
    float MasterVolume { get; set; }
    float BgmVolume { get; set; }
    float SfxVolume { get; set; }

    void Initialize();

    void PlayBgm(BgmKey key);
    void PlayBgm(AudioClip clip);
    /// <summary>
    /// 어드레서블 참조로 BGM 을 튼다. 카탈로그(SoundLibrary)에 키를 늘리지 않고 데이터 SO 가 곡을 직접 들고 있을 때 쓴다
    /// (던전 그룹별 전투 BGM 처럼 곡 수가 콘텐츠와 함께 늘어나는 경우). 미할당 참조는 무음이다.
    /// </summary>
    void PlayBgm(AssetReferenceAudioClip reference);
    void StopBgm();

    void PlaySfx(SfxKey key);
    void PlaySfx(AudioClip clip);
}
