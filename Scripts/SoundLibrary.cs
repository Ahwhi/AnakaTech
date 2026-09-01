using System.Collections.Generic;
using UnityEngine;
using static Enums;

/// <summary>
/// 사운드 카탈로그. 키(enum) -> AudioClip 어드레서블 참조 매핑만 보관하는 순수 데이터 SO 이다.
/// 실제 로드/캐싱/재생은 SoundManager 가 담당한다(데이터와 재생 로직 분리).
/// 참조 기반이므로 무거운 오디오는 원격 번들에 두고 필요 시 온디맨드로 로드하며, SO 자체는 가볍게 로컬에 동봉된다.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Anaka/Sound Library")]
public class SoundLibrary : ScriptableObject {
    [System.Serializable] public class BgmEntry {
        public BgmKey Key;
        public AssetReferenceAudioClip Clip;
        [Tooltip("어드레서블을 거치지 않고 바로 쓰는 클립(선택). 넣으면 위 참조보다 우선한다. " +
                 "타이틀 BGM 처럼 부트가 끝나기 전에 들려야 하는 소리에만 쓴다.")]
        public AudioClip DirectClip;
    }

    [System.Serializable] public class SfxEntry {
        public SfxKey Key;
        public AssetReferenceAudioClip Clip;
        [Tooltip("어드레서블을 거치지 않고 바로 쓰는 클립(선택). 넣으면 위 참조보다 우선한다. " +
                 "버튼 클릭음처럼 부트 중에도 들려야 하는 소리에만 쓴다.")]
        public AudioClip DirectClip;
    }

    [SerializeField] List<SfxEntry> sfxEntries = new();
    [SerializeField] List<BgmEntry> bgmEntries = new();

    private Dictionary<SfxKey, AssetReferenceAudioClip> _sfxMap;
    private Dictionary<BgmKey, AssetReferenceAudioClip> _bgmMap;

    private Dictionary<SfxKey, AudioClip> _sfxDirect;
    private Dictionary<BgmKey, AudioClip> _bgmDirect;

    public void Initialize() {
        _sfxMap = new Dictionary<SfxKey, AssetReferenceAudioClip>();
        _sfxDirect = new Dictionary<SfxKey, AudioClip>();
        foreach (var e in sfxEntries) {
            if (e.DirectClip != null) _sfxDirect[e.Key] = e.DirectClip;
            if (IsAssigned(e.Clip)) _sfxMap[e.Key] = e.Clip;
        }

        _bgmMap = new Dictionary<BgmKey, AssetReferenceAudioClip>();
        _bgmDirect = new Dictionary<BgmKey, AudioClip>();
        foreach (var e in bgmEntries) {
            if (e.DirectClip != null) _bgmDirect[e.Key] = e.DirectClip;
            if (IsAssigned(e.Clip)) _bgmMap[e.Key] = e.Clip;
        }
    }

    /// <summary>어드레서블을 거치지 않고 바로 쓸 수 있는 클립(없으면 null). 부트 중에도 즉시 재생된다.</summary>
    public AudioClip GetSfxClip(SfxKey key) =>
        _sfxDirect != null && _sfxDirect.TryGetValue(key, out var c) ? c : null;

    /// <summary>어드레서블을 거치지 않고 바로 쓸 수 있는 곡(없으면 null).</summary>
    public AudioClip GetBgmClip(BgmKey key) =>
        _bgmDirect != null && _bgmDirect.TryGetValue(key, out var c) ? c : null;

    public AssetReferenceAudioClip GetSfxRef(SfxKey key) =>
        _sfxMap != null && _sfxMap.TryGetValue(key, out var r) ? r : null;

    /// <summary>
    /// 키에 클립이 배정돼 있는지. 대체음 판단(배정 안 된 키를 다른 키로 돌려 재생)에 쓴다.
    /// 초기화 전에는 판단할 근거가 없으므로 배정된 것으로 본다(엉뚱한 대체음이 나가지 않도록).
    /// </summary>
    public bool HasSfx(SfxKey key) =>
        _sfxMap == null || _sfxMap.ContainsKey(key) || (_sfxDirect != null && _sfxDirect.ContainsKey(key));

    public AssetReferenceAudioClip GetBgmRef(BgmKey key) =>
        _bgmMap != null && _bgmMap.TryGetValue(key, out var r) ? r : null;

    // 참조가 실제로 할당되었는지(빈 슬롯 제외). 미할당 키는 재생 시 무음 처리된다.
    private static bool IsAssigned(AssetReferenceAudioClip clip) =>
        clip != null && clip.RuntimeKeyIsValid();
}
