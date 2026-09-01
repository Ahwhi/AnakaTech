using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 우편함 팝업(요청 9, 상용 모바일게임식). 미수령 우편 목록 + 개별 받기 + 모두 받기.
/// 보상은 서버에서 지급되며(권위), 수령 결과의 재화/보상 종류에 따라 관련 스토어를 재동기화한다.
/// HudMenuButton 으로 열리며(IHudPopup), 미수령 배지는 BadgeCenter.MailBadge(우편함 버튼은 BadgeView Id=Mail,
/// 메인 햄버거 버튼은 Id=HamburgerMenu)가 담당한다.
/// </summary>
public sealed class MailboxPopup : UIView, IHudPopup {
    [Header("List")]
    [SerializeField] private Transform content;
    [SerializeField] private MailItemView itemPrefab;
    [Tooltip("우편이 없을 때 표시할 오브젝트(선택).")]
    [SerializeField] private GameObject emptyLabel;
    [Tooltip("우편을 불러오고 채우는 동안 표시할 로딩 그룹(문구/스피너). 비우면 표시 없이 처리.")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Buttons")]
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Button closeButton;

    private const int BuildPerFrame = 8;   // 한 프레임에 생성할 우편 아이템 수.

    private MainHudContext _hud;
    private CancellationToken _ct;
    private CancellationTokenSource _buildCts;   // 목록 재구성 겹칠 때 이전 생성 취소용.
    private readonly List<MailItemView> _items = new();
    private bool _busy;
    private bool _wired;

    public void Initialize(MainHudContext hud) {
        _hud = hud;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (claimAllButton != null) claimAllButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OnClaimAll(); });
            if (closeButton != null) closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            _wired = true;
        }

        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        // 전역 오버레이 대신 팝업 자체 로딩 그룹으로, 조회~목록 채우기 동안만 표시한다.
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            var mails = await _hud.Mail.RequestMailAsync(_ct);
            await BuildListAsync(mails);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("우편을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    // 우편 목록을 청크 단위로 채운다. 재구성이 겹치면 이전 생성을 취소한다.
    private async UniTask BuildListAsync(List<MailDto> mails) {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _buildCts.Token;

        foreach (var it in _items) if (it != null) Destroy(it.gameObject);
        _items.Clear();

        if (itemPrefab != null && content != null && mails != null)
            await AsyncBuild.SpawnChunkedAsync(mails, BuildPerFrame, m => {
                var item = Instantiate(itemPrefab, content);
                item.Bind(m, OnClaimOne, ResolveReward);
                _items.Add(item);
            }, ct);

        RefreshEmptyState();
    }

    private void RefreshEmptyState() {
        bool empty = _items.Count == 0;
        if (emptyLabel != null) emptyLabel.SetActive(empty);
        if (claimAllButton != null) claimAllButton.interactable = !empty;
    }

    private void OnClaimOne(MailItemView item) {
        if (_busy || item == null || item.Mail == null) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        ClaimAsync(item.Mail.MailId, false).Forget();
    }

    private void OnClaimAll() {
        if (_busy || _items.Count == 0) return;
        ClaimAsync(0, true).Forget();
    }

    private async UniTaskVoid ClaimAsync(long mailId, bool all) {
        _busy = true;
        try {
            var res = all ? await _hud.Mail.ClaimAllAsync(_ct) : await _hud.Mail.ClaimMailAsync(mailId, _ct);
            if (!res.IsSuccess) { _hud.Ui.ShowToast("수령할 수 없습니다."); return; }

            _hud.Store.Gold.Value = res.NewGold;
            _hud.Store.Diamond.Value = res.NewDiamond;
            _hud.Store.Stamina.Value = res.NewStamina;
            _hud.Sound.PlaySfx(SfxKey.Claim);

            await ResyncRewardsAsync(res.Granted);

            // 수령한 우편은 서버에서 폐기되므로 목록을 다시 받아 갱신한다.
            if (loadingGroup != null) loadingGroup.SetActive(true);
            try {
                var mails = await _hud.Mail.RequestMailAsync(_ct);
                await BuildListAsync(mails);
            } finally {
                if (loadingGroup != null) loadingGroup.SetActive(false);
            }

            _hud.Ui.ShowToast("보상을 받았습니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("수령에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 보상 종류에 따라 관련 스토어를 재동기화(캐릭터=메인 스냅샷 / 장비/조각=인벤토리 / 티켓=상점 재화 / 소모품=아이템).
    private async UniTask ResyncRewardsAsync(List<MailRewardDto> granted) {
        if (granted == null) return;
        bool needMain = false, needInventory = false, needCurrency = false, needItem = false, needEngraving = false;
        bool needCostume = false;
        foreach (var r in granted) {
            switch (r.Kind) {
                case MailRewardKind.Character:
                case MailRewardKind.CharacterShard: needMain = true; break;   // 조각도 캐릭터 스냅샷에 들어 있다
                case MailRewardKind.Equip:
                case MailRewardKind.UniversalShard: needInventory = true; break;
                case MailRewardKind.Currency: needCurrency = true; break;
                case MailRewardKind.Item: needItem = true; break;
                case MailRewardKind.Engraving: needEngraving = true; break;
                case MailRewardKind.Costume: needCostume = true; break;
            }
        }
        if (needMain) {
            var data = await _hud.MainData.EnterMainAsync(_ct);
            _hud.Store.ApplyMainData(data);
        }
        if (needInventory) {
            var inv = await _hud.Inventory.RequestInventoryAsync(_ct);
            _hud.Store.ApplyInventory(inv);
        }
        if (needCurrency) {
            var catalog = await _hud.Shop.RequestShopAsync(_ct);
            _hud.Store.ApplyShopCurrencies(catalog.Currencies);
        }
        if (needItem) {
            var items = await _hud.Item.RequestInfoAsync(_ct);
            _hud.Store.ApplyItems(items);
        }
        if (needEngraving) {
            var engravings = await _hud.Engraving.RequestInfoAsync(_ct);
            _hud.Store.ApplyEngravings(engravings);
        }
        if (needCostume) {
            var costumes = await _hud.Costume.RequestInfoAsync(_ct);
            _hud.Store.ApplyCostumes(costumes);
        }
    }

    // 보상 1개를 아이콘 + 수량 + 툴팁 이름으로 해석한다(카탈로그 조회). 품목이 늘면 이 switch 만 확장하면 된다.
    // 재화/기력 = 수량 표시, 캐릭터/장비/조각 = 개수 표시. 아이콘 소스: 재화=CurrencyCatalog,
    // 캐릭터=CharacterData.Illustration, 장비=EquipmentData.Icon, 만능조각=ShopIconCatalog.
    private MailItemView.RewardDisplay ResolveReward(MailRewardDto r) {
        var cc = _hud != null ? _hud.Currencies : null;
        switch (r.Kind) {
            case MailRewardKind.Gold:
                return new MailItemView.RewardDisplay(cc != null ? cc.Icon(CurrencyType.Gold) : null, r.Value.ToString("N0"),
                                                      cc != null ? cc.DisplayName(CurrencyType.Gold) : "골드");
            case MailRewardKind.Diamond:
                return new MailItemView.RewardDisplay(cc != null ? cc.Icon(CurrencyType.Diamond) : null, r.Value.ToString("N0"),
                                                      cc != null ? cc.DisplayName(CurrencyType.Diamond) : "다이아");
            case MailRewardKind.Stamina:
                return new MailItemView.RewardDisplay(cc != null ? cc.Icon(CurrencyType.Stamina) : null, r.Value.ToString("N0"),
                                                      cc != null ? cc.DisplayName(CurrencyType.Stamina) : "기력");
            case MailRewardKind.Currency:
                return new MailItemView.RewardDisplay(CurrencyKeyIcon(r.CurrencyKey), r.Value.ToString("N0"),
                                                      CurrencyKeyName(r.CurrencyKey));
            case MailRewardKind.Character: {
                var d = _hud != null && _hud.CharDb != null ? _hud.CharDb.Get(r.Value) : null;
                return new MailItemView.RewardDisplay(d != null ? d.Illustration : null, Qty(r),
                                                      d != null ? d.DisplayName : "캐릭터");
            }
            case MailRewardKind.CharacterShard: {
                // 조각도 캐릭터 일러스트로 보여준다(전용 아이콘이 없어도 어떤 캐릭터의 조각인지 바로 읽힌다).
                var d = _hud != null && _hud.CharDb != null ? _hud.CharDb.Get(r.Value) : null;
                return new MailItemView.RewardDisplay(d != null ? d.Illustration : null, Qty(r),
                                                      d != null ? $"{d.DisplayName} 조각" : "캐릭터 조각");
            }
            case MailRewardKind.Item: {
                var d = _hud != null && _hud.ItemDb != null ? _hud.ItemDb.Get(r.Value) : null;
                return new MailItemView.RewardDisplay(d != null ? d.Icon : null, Qty(r),
                                                      d != null ? d.DisplayName : "아이템");
            }
            case MailRewardKind.Engraving: {
                var d = _hud != null && _hud.EngravingDb != null ? _hud.EngravingDb.Get(r.Value) : null;
                return new MailItemView.RewardDisplay(d != null ? d.Icon : null, "1",
                                                      d != null ? $"{d.DisplayName} 각인 도면" : "각인 도면");
            }
            case MailRewardKind.Costume: {
                // 코스튬은 그 자체가 일러스트라 별도 아이콘 없이 코스튬 일러스트를 그대로 보여 준다.
                var d = _hud != null && _hud.CostumeDb != null ? _hud.CostumeDb.Get(r.Value) : null;
                return new MailItemView.RewardDisplay(d != null ? d.Illustration : null, "1",
                                                      d != null ? $"{d.DisplayName} 코스튬" : "코스튬");
            }
            case MailRewardKind.Equip: {
                var d = _hud != null && _hud.EquipDb != null ? _hud.EquipDb.Get(r.Value) : null;
                return new MailItemView.RewardDisplay(d != null ? d.Icon : null, Qty(r),
                                                      d != null ? d.DisplayName : "장비");
            }
            case MailRewardKind.ProfileFrame: {
                var sprite = _hud != null && _hud.ProfileFrames != null ? _hud.ProfileFrames.Icon(r.Value) : null;
                string frameName = _hud != null && _hud.ProfileFrames != null ? _hud.ProfileFrames.DisplayName(r.Value) : "프로필 테두리";
                return new MailItemView.RewardDisplay(sprite, Qty(r), frameName);
            }
            case MailRewardKind.UniversalShard:
                return new MailItemView.RewardDisplay(ShardIcon(r.Value), Qty(r),
                                                      $"{EncyclopediaRewardRules.GradeName(r.Value)} 만능조각");
            default:
                return new MailItemView.RewardDisplay(null, r.Value.ToString());
        }
    }

    // 개수형 보상의 수량 문구. 서버가 Qty 를 안 채운 옛 우편도 1로 보이도록 하한을 둔다.
    private static string Qty(MailRewardDto r) => (r.Qty < 1 ? 1 : r.Qty).ToString("N0");

    // 재화키 -> 아이콘/이름. 키 목록을 여기서 다시 나열하지 않고 CurrencyKeys 매핑을 쓴다
    // (재화가 늘 때 이 파일을 같이 고쳐야 하는 상황을 만들지 않기 위함).
    private Sprite CurrencyKeyIcon(string key) {
        var cc = _hud != null ? _hud.Currencies : null;
        if (cc == null) return null;
        var type = CurrencyKeys.ToType(key);
        return type.HasValue ? cc.Icon(type.Value) : null;
    }

    private string CurrencyKeyName(string key) {
        var cc = _hud != null ? _hud.Currencies : null;
        if (cc == null) return key ?? "";
        var type = CurrencyKeys.ToType(key);
        return type.HasValue ? cc.DisplayName(type.Value) : (key ?? "");
    }

    // 만능조각 아이콘은 보상 슬롯과 같은 소스(등급별 카탈로그)를 쓴다.
    private Sprite ShardIcon(int grade) =>
        _hud != null && _hud.GradeShardPrefabs != null ? _hud.GradeShardPrefabs.Icon((Grade)grade) : null;

    protected override void OnClosed() {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = null;
        foreach (var it in _items) if (it != null) Destroy(it.gameObject);
        _items.Clear();
    }
}
