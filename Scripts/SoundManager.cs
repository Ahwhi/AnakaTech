using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;
using static Enums;

public class SoundManager : MonoBehaviour, ISoundService {
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] SoundLibrary soundLibrary;

    private IAssetService _asset;

    // 로드 완료된 클립을 키로 즉시 재생하기 위한 캐시. 동시 중복 로드는 _loading 집합으로 차단해 핸들 누수를 막는다.
    private readonly Dictionary<SfxKey, AudioClip> _sfxCache = new();
    private readonly Dictionary<BgmKey, AudioClip> _bgmCache = new();
    private readonly HashSet<SfxKey> _sfxLoading = new();
    private readonly HashSet<BgmKey> _bgmLoading = new();

    // 어드레서블 참조로 직접 트는 BGM(던전 그룹별 전투곡 등). 카탈로그 키가 없으므로 주소 문자열로 캐시한다.
    private readonly Dictionary<string, AudioClip> _bgmRefCache = new();
    private readonly HashSet<string> _bgmRefLoading = new();

    // 같은 효과음의 중복 재생을 걸러내는 간격(초)과 키별 마지막 재생 시각.
    private const float DuplicateSfxSeconds = 0.05f;
    private readonly Dictionary<SfxKey, float> _lastSfxTime = new();

    private CancellationToken _lifetime;

    // 절전 모드 동안은 소리를 내지 않는다. 볼륨 설정(세이브)은 건드리지 않고 재생만 멈춘다.
    private bool _suspended;
    // BGM 을 틀어 둔 상태인지(StopBgm 전까지). 멈춘 동안 곡이 바뀌어도 풀릴 때 지금 곡으로 다시 튼다.
    private bool _bgmWanted;

    [Inject]
    public void Construct(IAssetService asset) {
        _asset = asset;
    }

    private void Awake() {
        _lifetime = this.GetCancellationTokenOnDestroy();
        PowerSaveMode.Active.Subscribe(SetSuspended).AddTo(this);
    }

    // 멈출 때 BGM 은 일시정지(풀리면 이어서), 효과음은 즉시 끊는다. 멈춘 동안 들어온 효과음 요청은 버린다
    // (쌓아 두면 풀리는 순간 한꺼번에 터진다). 멈춘 동안은 오디오 디코딩도 하지 않아 그만큼 CPU 를 덜 쓴다.
    private void SetSuspended(bool suspended) {
        if (_suspended == suspended) return;
        _suspended = suspended;

        if (suspended) {
            if (bgmSource != null) bgmSource.Pause();
            if (sfxSource != null) sfxSource.Stop();
            return;
        }

        if (bgmSource == null || !_bgmWanted || bgmSource.clip == null || bgmSource.isPlaying) return;
        // 멈춘 사이 곡이 바뀌었으면 UnPause 로는 재생되지 않으므로 처음부터 튼다.
        bgmSource.UnPause();
        if (!bgmSource.isPlaying) bgmSource.Play();
    }

    // 볼륨의 단일 진실 원천은 세이브 값이고, AudioSource 볼륨은 거기서 파생한다(마스터 x 개별).
    // 세이브 로드 전에는 조절할 대상이 없으므로 읽기는 무음 아닌 기본값, 쓰기는 무시한다.
    private static GameData Data => UserDataManager.Data;

    public float MasterVolume {
        get => Data != null ? Data.MasterVolume : 1f;
        set => SetVolume(v => Data.MasterVolume = v, value);
    }

    public float BgmVolume {
        get => Data != null ? Data.BgmVolume : 1f;
        set => SetVolume(v => Data.BgmVolume = v, value);
    }

    public float SfxVolume {
        get => Data != null ? Data.SfxVolume : 1f;
        set => SetVolume(v => Data.SfxVolume = v, value);
    }

    public void Initialize() {
        ApplyVolumes();
        soundLibrary?.Initialize();
    }

    private void SetVolume(Action<float> store, float value) {
        if (Data == null) return;
        store(Mathf.Clamp01(value));
        ApplyVolumes();
        UserDataManager.RequestSave();
    }

    private void ApplyVolumes() {
        if (Data == null) return;
        if (bgmSource != null) bgmSource.volume = Data.MasterVolume * Data.BgmVolume;
        if (sfxSource != null) sfxSource.volume = Data.MasterVolume * Data.SfxVolume;
    }

    // BGM ---------------------------------------------------------------------
    public void PlayBgm(AudioClip clip) {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && (bgmSource.isPlaying || (_suspended && _bgmWanted))) return;
        _bgmWanted = true;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        // 멈춘 동안에는 곡만 바꿔 두고, 재생은 풀릴 때(SetSuspended) 한다.
        if (!_suspended) bgmSource.Play();
    }

    public void PlayBgm(BgmKey key) {
        if (_bgmCache.TryGetValue(key, out var clip)) { PlayBgm(clip); return; }

        // 직접 배정된 곡은 어드레서블을 거치지 않으므로 부트가 끝나기 전에도 바로 재생된다.
        var direct = soundLibrary != null ? soundLibrary.GetBgmClip(key) : null;
        if (direct != null) { _bgmCache[key] = direct; PlayBgm(direct); return; }

        LoadAndPlayBgmAsync(key).Forget();
    }

    // 어드레서블 참조로 트는 BGM. 키 기반과 캐시/중복로드 방어 규칙은 같고, 카탈로그에 키가 없으므로
    // 참조의 런타임 키(주소)를 사전 키로 쓴다.
    public void PlayBgm(AssetReferenceAudioClip reference) {
        if (reference == null || !reference.RuntimeKeyIsValid()) return;

        string address = reference.RuntimeKey.ToString();
        if (_bgmRefCache.TryGetValue(address, out var cached)) { PlayBgm(cached); return; }

        LoadAndPlayBgmAsync(reference, address).Forget();
    }

    public void StopBgm() {
        _bgmWanted = false;
        if (bgmSource != null) bgmSource.Stop();
    }

    private async UniTaskVoid LoadAndPlayBgmAsync(AssetReferenceAudioClip reference, string address) {
        if (_asset == null) return;
        if (!_bgmRefLoading.Add(address)) return;   // 이미 로딩 중인 요청은 무시(핸들 누수 방지)
        try {
            var clip = await _asset.LoadAssetAsync<AudioClip>(reference, _lifetime);
            if (clip == null) return;
            _bgmRefCache[address] = clip;
            PlayBgm(clip);
        } catch (OperationCanceledException) {
            // 씬 종료/앱 종료. 조용히 끝낸다.
        } catch (Exception e) {
            // 곡 하나 때문에 전투가 멈추면 안 되므로 무음으로 넘기고 원인을 남긴다.
            Debug.LogWarning($"[SoundManager] BGM 로드 실패: {address} - {e.Message}");
        } finally {
            _bgmRefLoading.Remove(address);
        }
    }

    private async UniTaskVoid LoadAndPlayBgmAsync(BgmKey key) {
        var clip = await LoadBgmAsync(key);
        if (clip != null) PlayBgm(clip);
    }

    private async UniTask<AudioClip> LoadBgmAsync(BgmKey key) {
        if (_bgmCache.TryGetValue(key, out var cached)) return cached;
        var reference = soundLibrary?.GetBgmRef(key);
        if (_asset == null || reference == null) return null;   // 미할당 키는 무음
        if (!_bgmLoading.Add(key)) return null;                 // 이미 로딩 중인 요청은 무시
        try {
            var clip = await _asset.LoadAssetAsync<AudioClip>(reference, _lifetime);
            if (clip != null) _bgmCache[key] = clip;
            return clip;
        } catch (OperationCanceledException) {
            return null;
        } catch (Exception e) {
            // 클립이 어드레서블로 등록되지 않았거나 번들이 없는 경우. 소리 하나 때문에 화면이 멈추면 안 되므로
            // 무음으로 넘기고 원인을 남긴다(에셋을 Addressable 로 표시했는지 확인할 것).
            Debug.LogWarning($"[SoundManager] BGM 로드 실패: {key} - {e.Message}");
            return null;
        } finally {
            _bgmLoading.Remove(key);
        }
    }

    // SFX ---------------------------------------------------------------------
    public void PlaySfx(AudioClip clip) {
        if (_suspended || sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySfx(SfxKey key) {
        if (_suspended) return;   // 멈춘 동안에는 클립 로드도 하지 않는다.
        key = Resolve(key);
        if (!TryStartSfx(key)) return;
        if (_sfxCache.TryGetValue(key, out var clip)) { PlaySfx(clip); return; }

        // 직접 배정된 클립은 어드레서블을 거치지 않으므로 부트가 끝나기 전에도 그 자리에서 난다.
        var direct = soundLibrary != null ? soundLibrary.GetSfxClip(key) : null;
        if (direct != null) { _sfxCache[key] = direct; PlaySfx(direct); return; }

        LoadAndPlaySfxAsync(key).Forget();
    }

    // 클립이 배정되지 않은 키를 대체음으로 돌린다. "아무 소리도 안 나는 것"보다 최소한의 반응이 나은
    // 조작 계열 사운드만 대상으로 하며, 전투/연출음처럼 없으면 조용해야 하는 소리는 그대로 무음이다.
    // 대체음도 배정돼 있지 않으면 원래 키로 두고 무음 처리에 맡긴다.
    private SfxKey Resolve(SfxKey key) {
        if (soundLibrary == null) return key;

        // 대체음이 또 비어 있을 수 있으므로(수련 완료 -> 수령 -> 클릭) 배정된 키를 만날 때까지 따라간다.
        // 규칙을 잘못 적어 서로를 가리켜도 멈추도록 횟수를 제한한다.
        var current = key;
        for (int i = 0; i < 4 && !soundLibrary.HasSfx(current); i++) {
            var fallback = FallbackFor(current);
            if (!fallback.HasValue) return key;
            current = fallback.Value;
        }
        return soundLibrary.HasSfx(current) ? current : key;
    }

    // 키별 대체음 규칙. 새 조작 사운드가 늘어나면 여기에 한 줄만 추가한다.
    private static SfxKey? FallbackFor(SfxKey key) => key switch {
        SfxKey.Claim => SfxKey.ButtonClick,          // 보상 수령(퀘스트/업적/미션/우편 등)
        SfxKey.TrainingComplete => SfxKey.Claim,     // 수련 완료도 수령의 일종이라 수령음 -> 클릭음 순으로 내려간다
        SfxKey.CardPick => SfxKey.ButtonClick,
        SfxKey.CardPlace => SfxKey.ButtonClick,
        // 별 승급은 VFX 가 없어 소리마저 없으면 성공했는지 알기 어렵다(장비 강화/각성은 VFX 가 있어 대체음을 두지 않는다).
        SfxKey.StarUp => SfxKey.ButtonClick,
        // 몬스터 전용 피격음을 따로 넣기 전까지는 캐릭터 피격음을 함께 쓴다(둘로 나누기 전과 같은 소리).
        SfxKey.HitMonster => SfxKey.HitCharacter,
        _ => null
    };

    // 같은 효과음이 아주 짧은 간격 안에 겹쳐 요청되면 한 번만 낸다.
    // 한 번의 조작을 두 곳이 각각 알리는 경우(전역 클릭음 창구 + 화면이 직접 내는 클릭음)가 대표적인데,
    // 두 요청이 같은 프레임에 들어오므로 여기서 걸러야 소리가 두 겹으로 울리지 않는다.
    // 사람이 연타하는 간격(0.05초 이상)은 그대로 통과한다.
    private bool TryStartSfx(SfxKey key) {
        float now = Time.unscaledTime;   // 일시정지(timeScale 0) 중에도 UI 는 동작한다
        if (_lastSfxTime.TryGetValue(key, out float last) && now - last < DuplicateSfxSeconds) return false;
        _lastSfxTime[key] = now;
        return true;
    }

    private async UniTaskVoid LoadAndPlaySfxAsync(SfxKey key) {
        var clip = await LoadSfxAsync(key);
        if (clip != null) PlaySfx(clip);
    }

    private async UniTask<AudioClip> LoadSfxAsync(SfxKey key) {
        if (_sfxCache.TryGetValue(key, out var cached)) return cached;
        var reference = soundLibrary?.GetSfxRef(key);
        if (_asset == null || reference == null) return null;   // 미할당 키는 무음
        if (!_sfxLoading.Add(key)) return null;                 // 이미 로딩 중인 요청은 무시
        try {
            var clip = await _asset.LoadAssetAsync<AudioClip>(reference, _lifetime);
            if (clip != null) _sfxCache[key] = clip;
            return clip;
        } catch (OperationCanceledException) {
            return null;
        } catch (Exception e) {
            // 클립이 어드레서블로 등록되지 않았거나 번들이 없는 경우. 클릭음 하나 때문에 조작이 막히면 안 되므로
            // 무음으로 넘기고 원인을 남긴다(에셋을 Addressable 로 표시했는지 확인할 것).
            Debug.LogWarning($"[SoundManager] SFX 로드 실패: {key} - {e.Message}");
            return null;
        } finally {
            _sfxLoading.Remove(key);
        }
    }
}
