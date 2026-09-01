using UniRx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 타이틀 배경음악 on/off 버튼. 타이틀 BGM 재생의 주인이며, 켜짐/꺼짐을 로컬 세이브(GameData.TitleBgmOn)에 기억한다.
///
/// 설정 팝업의 BGM 볼륨과는 별개의 스위치다. 볼륨 값은 건드리지 않고 타이틀 BGM 재생만 끄므로,
/// 여기서 꺼도 게임 안 BGM 볼륨은 그대로다(반대로 설정에서 볼륨을 바꿔도 이 스위치는 유지된다).
/// 기본값은 켜짐이고, 앱을 다시 켜도 마지막 선택이 남는다.
///
/// 재생은 세이브를 읽은 뒤(켜짐/꺼짐을 알게 된 뒤) 바로 시작한다. 예전에는 다운로드 단계가 끝나기를 기다렸는데,
/// 곡이 원격 번들에 있던 시절의 제약이었다. 지금 타이틀 곡은 앱에 함께 들어가 있으므로(SoundLibrary 의
/// 직접 클립 슬롯) 기다릴 이유가 없고, 다운로드 내내 조용한 타이틀이 되는 문제만 남는다.
///
/// 타이틀을 벗어날 때(씬 언로드) 자기가 켠 BGM 을 스스로 끈다. 다음 씬은 자기 BGM 을 직접 시작하므로
/// 타이틀 곡이 다른 씬으로 새어 나가지 않는다.
///
/// 루트 스코프에서 스스로 주입받으므로(RootInjectedBehaviour) 씬 스코프에 등록하지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class TitleBgmToggle : RootInjectedBehaviour {
    [SerializeField] private Button button;

    [Header("Icon (선택)")]
    [Tooltip("on/off 를 스프라이트로 표시할 이미지. 비우면 스프라이트 교체를 생략한다.")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    [Header("Label (선택)")]
    [Tooltip("on/off 를 글자로 표시할 텍스트. 비우면 표시만 생략한다.")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private string onLabel = "BGM ON";
    [SerializeField] private string offLabel = "BGM OFF";

    private ISoundService _sound;
    private IBootStatus _boot;

    // 재생 여부를 정할 수 있는 시점을 지났는지(세이브 로드 이후). 그전의 on/off 변경은 표시만 하고 재생은 미룬다.
    private bool _loadable;
    // 이 버튼이 켠 BGM 이 울리고 있는지. 내가 켜지 않은 BGM 을 뺏어 끄지 않기 위한 표시.
    private bool _playing;

    [Inject]
    public void Construct(ISoundService sound, IBootStatus boot) {
        _sound = sound;
        _boot = boot;
    }

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Toggle);
        RefreshView(IsOn);
    }

    // 저장값은 부트 1단계(세이브 로드) 후에야 읽을 수 있고, 곡 로드는 다운로드 단계 후에야 안전하다.
    // 두 시점 모두 부트 상태로 알 수 있으므로 상태 변화에 맞춰 표시와 재생을 갱신한다.
    protected override void OnInjected() {
        _boot.State.Subscribe(OnBootState).AddTo(this);
    }

    // 세이브 로드 전에는 저장값을 알 수 없으므로 기본값(켜짐)으로 본다.
    private static bool IsOn => UserDataManager.Data == null || UserDataManager.Data.TitleBgmOn;

    private void OnBootState(BootState state) {
        RefreshView(IsOn);
        // 세이브를 읽기 전에는 켜짐/꺼짐을 알 수 없어 재생 여부를 정할 수 없다. 그것만 지나면 바로 튼다.
        if (_loadable || UserDataManager.Data == null) return;
        _loadable = true;
        Apply(IsOn);
    }

    private void Toggle() {
        var data = UserDataManager.Data;
        if (data == null) return;   // 세이브 로드 전에는 기억할 곳이 없으므로 무시한다

        _sound?.PlaySfx(SfxKey.ButtonClick);
        data.TitleBgmOn = !data.TitleBgmOn;
        UserDataManager.RequestSave();
        Apply(data.TitleBgmOn);
    }

    private void Apply(bool on) {
        RefreshView(on);
        if (_sound == null || !_loadable) return;

        if (on) {
            _sound.PlayBgm(BgmKey.Title);
            _playing = true;
        } else if (_playing) {
            _sound.StopBgm();
            _playing = false;
        }
    }

    private void RefreshView(bool on) {
        if (iconImage != null) {
            var sprite = on ? onSprite : offSprite;
            if (sprite != null) iconImage.sprite = sprite;
        }
        if (label != null) label.text = on ? onLabel : offLabel;
    }

    private void OnDestroy() {
        if (button != null) button.onClick.RemoveListener(Toggle);
        // 타이틀 곡은 타이틀에서만 울린다. 내가 켠 경우에만 끈다.
        if (_playing) _sound?.StopBgm();
    }
}
