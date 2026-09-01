using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 조각/만능조각 팝업(PLANNING 5-12, 10). 선택 캐릭터의 보유 조각 + 같은 등급 만능조각 수량을 보여주고,
/// 슬라이더(+/- 버튼)로 전환 수량을 정해 만능조각을 해당 캐릭터 조각으로 전환한다(1:1, 등급 일치).
/// </summary>
public sealed class ShardPopup : UIView {
    [FormerlySerializedAs("portraitView")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("전환 안내 문구. '<등급> 캐릭터 만능 조각을 몇개를 사용하여 <이름> 조각으로 변환할까요?' 형태로 출력.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("등급별 만능조각 아이콘을 표시할 Image. 비우면 표시하지 않는다.")]
    [SerializeField] private Image shardIcon;
    [SerializeField] private TMP_Text charShardText;
    [SerializeField] private TMP_Text universalShardText;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [Tooltip("선택. 최대 수량 버튼(보유 만능조각 전부로 설정). 상점 구매 확인 팝업과 같은 역할이다.")]
    [SerializeField] private Button maxButton;
    [SerializeField] private Button convertButton;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private int _charId;
    private int _grade;
    private CancellationToken _ct;
    private bool _busy;

    public void Initialize(MainHudContext hud, int charDataId) {
        _hud = hud;
        _charId = charDataId;
        _ct = this.GetCancellationTokenOnDestroy();

        var data = hud.CharDb.Get(charDataId);
        if (data != null) {
            // 내 캐릭터 화면이므로 장착 코스튬이 있으면 그 일러스트로 보여 준다(덱/육성 화면과 같은 모습).
            if (illustrationView != null) illustrationView.Bind(hud.Appearance.Mine(data));
            _grade = (int)data.Grade;
            ApplyConversionInfo(data);
        }

        if (amountSlider != null) {
            amountSlider.wholeNumbers = true;
            amountSlider.minValue = 0;
            amountSlider.onValueChanged.AddListener(_ => RefreshAmount());
        }
        if (minusButton != null) minusButton.onClick.AddListener(() => ChangeAmount(-1));
        if (plusButton != null) plusButton.onClick.AddListener(() => ChangeAmount(1));
        if (maxButton != null) maxButton.onClick.AddListener(SetMaxAmount);
        if (convertButton != null) convertButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OnConvert().Forget(); });
        if (closeButton != null) closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

        Refresh();
    }

    // 안내 문구(등급 + 캐릭터명)와 등급별 만능조각 아이콘을 구성한다.
    private void ApplyConversionInfo(CharacterData data) {
        if (descriptionText != null)
            descriptionText.text = $"{data.Grade} 캐릭터 만능 조각을 몇개를 사용하여 {data.DisplayName} 조각으로 변환할까요?";

        if (shardIcon == null) return;
        var sprite = _hud.GradeShardPrefabs != null ? _hud.GradeShardPrefabs.Icon(data.Grade) : null;
        shardIcon.sprite = sprite;
        shardIcon.enabled = sprite != null;
    }

    private int UniversalQty => _hud.Store.UniversalShards.TryGetValue(_grade, out var q) ? q : 0;

    private void Refresh() {
        var prog = _hud.Store.GetProgress(_charId);
        if (charShardText != null) charShardText.text = $"보유: {(prog != null ? prog.Shards : 0)}";

        int uni = UniversalQty;
        if (universalShardText != null) universalShardText.text = $"만능: {uni}";
        if (amountSlider != null) {
            amountSlider.maxValue = uni;
            if (amountSlider.value > uni) amountSlider.value = uni;
        }
        if (convertButton != null) convertButton.interactable = uni > 0 && !_busy;
        // 만능조각이 없으면 수량 조절(+/-) 버튼도 비활성화.
        if (minusButton != null) minusButton.interactable = uni > 0 && !_busy;
        if (plusButton != null) plusButton.interactable = uni > 0 && !_busy;
        if (maxButton != null) maxButton.interactable = uni > 0 && !_busy;
        RefreshAmount();
    }

    private void ChangeAmount(int delta) {
        if (amountSlider == null) return;
        amountSlider.value = Mathf.Clamp(amountSlider.value + delta, 0, amountSlider.maxValue);
    }

    // 최대 수량. 상한은 Refresh 가 보유 만능조각 수로 맞춰 둔 슬라이더 maxValue 라, 그 값을 그대로 쓴다
    // (여기서 보유량을 다시 읽으면 상한이 두 곳에서 정해져 어긋날 수 있다).
    private void SetMaxAmount() {
        if (amountSlider == null) return;
        amountSlider.value = amountSlider.maxValue;
    }

    private void RefreshAmount() {
        if (amountText != null && amountSlider != null) amountText.text = ((int)amountSlider.value).ToString();
    }

    private async UniTaskVoid OnConvert() {
        if (_busy) return;
        int amount = amountSlider != null ? (int)amountSlider.value : 0;
        if (amount < 1) { _hud.Ui.ShowToast("전환할 수량을 선택하세요."); return; }

        _busy = true;
        if (convertButton != null) convertButton.interactable = false;
        try {
            var res = await _hud.Inventory.ConvertUniversalShardAsync(_charId, amount, _ct);
            if (res.IsSuccess) {
                _hud.Store.ApplyShardConvert(res);
                _hud.Sound.PlaySfx(SfxKey.Purchase);
                _hud.Ui.ShowToast($"{amount}개를 전환했습니다.");
                CloseAsync().Forget();   // 전환 성공 시 팝업 자동 닫기
                return;
            } else {
                _hud.Ui.ShowToast(res.FailReason switch {
                    1 => "전환할 수 없는 캐릭터입니다.",
                    2 => "만능조각이 부족합니다.",
                    _ => "전환할 수 없습니다."
                });
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("전환에 실패했습니다.");
        } finally {
            _busy = false;
            Refresh();
        }
    }
}
