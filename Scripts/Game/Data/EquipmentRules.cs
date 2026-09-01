using UnityEngine;

/// <summary>
/// 장비 성장 규칙(각성=별레벨 / 레벨업=레벨) + 전용무기 규약. 서버(Server.Data.EquipGrowth)와 수치가 반드시
/// 일치해야 미리보기와 실제 결과가 어긋나지 않는다. 치트 판정은 서버가 하며 여기는 표시/미리보기 미러다.
///
/// 두 성장 축:
///  - 각성(별레벨 1~15): 같은 장비를 재료로 소모해 별레벨을 올린다. 최대 레벨 상한을 열어준다.
///  - 레벨업(레벨 1~150): 골드로 확정 성장. 별레벨 x 10 이 상한이다.
///
/// 레벨업 성장 규칙(구간이 누적된다 - 43->44 는 주스탯+1옵션+2옵션+3옵션+4옵션이 전부 1 오름):
///   레벨 2~10   : 주스탯 +1
///   레벨 11~20  : 주스탯, 1옵션 +1
///   레벨 21~30  : 주스탯, 1옵션, 2옵션 +1
///   레벨 31~40  : 주스탯, 1옵션, 2옵션, 3옵션 +1
///   레벨 41~150 : 주스탯, 1옵션, 2옵션, 3옵션, 4옵션 +1
/// 즉 n옵션은 레벨 (10n+1) 부터 성장하기 시작한다. 실제 스탯 증가량은 EquipmentStatLayout 의 PerPoint 가 결정한다
/// (체력은 1포인트 = 10, 치명타확률은 1포인트 = 0.1).
/// </summary>
public static class EquipmentRules {
    /// <summary>장비 최대 별레벨. 캐릭터와 동일하게 15(별 5칸 x 3티어: 노랑->분홍->파랑).</summary>
    public const int MaxStarLevel = GrowthRules.MaxStarLevel;

    /// <summary>별레벨 1당 열리는 최대 레벨(4성 = 40레벨까지).</summary>
    public const int LevelsPerStar = 10;

    /// <summary>레벨업 골드 = 현재 레벨 x 이 값(3->4레벨 = 300골드).</summary>
    public const int GoldPerLevel = 100;

    /// <summary>각성 다이아 = 현재 별레벨 x 이 값(캐릭터 승급과 동일 규칙).</summary>
    public const int DiaPerStar = GrowthRules.StarDiaPerLevel;

    /// <summary>전용무기 id = 캐릭터 id + 이 값. 캐릭터와 1:1 대응이라 대응표 없이 id 에서 역산한다.</summary>
    public const int SignatureIdOffset = 50000;

    // ── 각성(별레벨) ──

    /// <summary>지정 별레벨에서 열리는 최대 레벨(별레벨 x 10). 별레벨은 최소 1로 간주한다.</summary>
    public static int MaxLevel(int starLevel) => Mathf.Clamp(starLevel, 1, MaxStarLevel) * LevelsPerStar;

    // 각성 재료 개수 구간. 별레벨만큼 요구하면 고별레벨에서 같은 장비를 열 개 넘게 모아야 해서
    // 사실상 각성이 막힌다. 계단식으로만 올려 상위 각성도 손이 닿게 한다.
    // 서버(EquipGrowth.AwakenMaterialCount)와 같은 값이어야 한다. 어긋나면 클라 표시로는 충분한데
    // 서버가 거절하는(또는 그 반대인) 상태가 된다.
    private const int MaterialTier2Star = 5;    // 이 별레벨부터 2개(4성->5성까지는 1개)
    private const int MaterialTier3Star = 10;   // 이 별레벨부터 3개(9성->10성까지는 2개)

    /// <summary>현재 별레벨에서 다음 별레벨로 각성할 때 필요한 "같은 장비" 개수
    /// (4성->5성 = 1개, 9성->10성 = 2개, 10성->11성부터 = 3개).</summary>
    public static int AwakenMaterialCount(int currentStar) {
        if (currentStar >= MaterialTier3Star) return 3;
        return currentStar >= MaterialTier2Star ? 2 : 1;
    }

    /// <summary>현재 별레벨에서 다음 별레벨로 각성할 때 필요한 다이아(1성->2성 = 100, 2성->3성 = 200 ...).</summary>
    public static int AwakenDiaCost(int currentStar) => DiaPerStar * Mathf.Max(1, currentStar);

    /// <summary>더 각성할 수 있는지(최대 별레벨 미달).</summary>
    public static bool CanAwaken(int currentStar) => currentStar < MaxStarLevel;

    // ── 레벨업 ──

    /// <summary>현재 레벨에서 다음 레벨로 올릴 때 필요한 골드(3->4레벨 = 300).</summary>
    public static int LevelUpGold(int currentLevel) => GoldPerLevel * Mathf.Max(1, currentLevel);

    /// <summary>더 레벨업할 수 있는지(별레벨이 열어준 상한 미달).</summary>
    public static bool CanLevelUp(int currentStar, int currentLevel) => currentLevel < MaxLevel(currentStar);

    /// <summary>지정 레벨에서 주스탯에 누적된 성장 포인트(레벨 1 = 0).</summary>
    public static int MainGrowthPoints(int level) => Mathf.Max(0, level - 1);

    /// <summary>
    /// 지정 레벨에서 n옵션(optionIndex 0-based)에 누적된 성장 포인트.
    /// 1옵션은 11레벨부터, 2옵션은 21레벨부터... 즉 (10 x (index+1)) 을 넘긴 레벨 수만큼 오른다.
    /// </summary>
    public static int OptionGrowthPoints(int level, int optionIndex) =>
        Mathf.Max(0, level - LevelsPerStar * (optionIndex + 1));

    // ── 최종 스탯 조립 ──

    /// <summary>
    /// 장비 1개의 최종 스탯. 드랍 시 굴린 기본 포인트(mainPoints/optionPoints)에 레벨 성장 포인트를 더한 뒤
    /// 부위별 스탯 종류/스케일(EquipmentStatLayout)로 환산한다. 등급이 개방한 옵션 개수까지만 반영한다.
    /// optionPoints 가 null 이거나 짧아도 안전하다(없는 칸은 0으로 간주).
    /// </summary>
    public static CombatStats ResolveStats(EquipmentSlot slot, Grade grade, int level,
                                           int mainPoints, System.Collections.Generic.IReadOnlyList<int> optionPoints) {
        var stats = new CombatStats();
        int lv = Mathf.Max(1, level);

        var main = EquipmentStatLayout.Main(slot);
        stats = stats.WithAdded(main.Stat, (mainPoints + MainGrowthPoints(lv)) * main.PerPoint);

        int count = EquipmentStatLayout.OptionCount(grade);
        for (int i = 0; i < count; i++) {
            int basePoints = optionPoints != null && i < optionPoints.Count ? optionPoints[i] : 0;
            var opt = EquipmentStatLayout.Option(slot, i);
            stats = stats.WithAdded(opt.Stat, (basePoints + OptionGrowthPoints(lv, i)) * opt.PerPoint);
        }
        return stats;
    }

    /// <summary>지정 레벨에서 주스탯의 총 포인트(기본 + 성장). 상세 패널의 옵션별 표시에 쓴다.</summary>
    public static int MainPointsAt(int basePoints, int level) => basePoints + MainGrowthPoints(Mathf.Max(1, level));

    /// <summary>지정 레벨에서 n옵션의 총 포인트(기본 + 성장).</summary>
    public static int OptionPointsAt(int basePoints, int level, int optionIndex) =>
        basePoints + OptionGrowthPoints(Mathf.Max(1, level), optionIndex);

    /// <summary>
    /// 장비 스탯 목록 문자열(주스탯 먼저, 그 다음 1~4옵션 순서). 예) "공격력 45\n방어구관통력 18".
    /// StatFormat.FullStats 와 달리 부위의 슬롯 순서를 그대로 유지해 유저의 "주스탯/1옵션..." 인식과 일치시킨다.
    /// 상세 패널과 가챠 연출 카드가 같은 문구를 쓰도록 여기 한 곳에 모아둔다.
    /// </summary>
    public static string DescribeStats(EquipmentSlot slot, Grade grade, int level,
                                       int mainPoints, System.Collections.Generic.IReadOnlyList<int> optionPoints) {
        var sb = new System.Text.StringBuilder();
        int lv = Mathf.Max(1, level);

        var main = EquipmentStatLayout.Main(slot);
        sb.Append(GrowthRules.StatName(main.Stat)).Append(' ')
          .Append(StatFormat.FormatValue(main.Stat, MainPointsAt(mainPoints, lv) * main.PerPoint));

        int count = EquipmentStatLayout.OptionCount(grade);
        for (int i = 0; i < count; i++) {
            int basePoints = optionPoints != null && i < optionPoints.Count ? optionPoints[i] : 0;
            var opt = EquipmentStatLayout.Option(slot, i);
            sb.Append('\n').Append(GrowthRules.StatName(opt.Stat)).Append(' ')
              .Append(StatFormat.FormatValue(opt.Stat, OptionPointsAt(basePoints, lv, i) * opt.PerPoint));
        }
        return sb.ToString();
    }

    /// <summary>장비 인스턴스의 스탯 목록 문자열(DescribeStats 편의 오버로드).</summary>
    public static string DescribeStats(EquipmentInstanceDto inst, EquipmentData data) {
        if (inst == null || data == null) return "";
        return DescribeStats(data.Slot, data.Grade, inst.Level, inst.MainPoints, inst.OptionPoints);
    }

    /// <summary>
    /// 다음 레벨업으로 오르는 스탯 목록 문자열("공격력 +1" 형태, 주스탯 -> 옵션 순서). 오르지 않는 스탯은 생략한다.
    /// 성장 포인트의 차분(다음 레벨 - 현재 레벨)으로 만들기 때문에 실제 결과와 어긋날 수 없다.
    ///
    /// 구간 경계에서 자동으로 항목이 늘어난다. 예) 10->11 레벨에서 1옵션이 처음 오르기 시작하므로
    /// 그 시점부터 1옵션 줄이 추가되어, 유저가 "레벨을 더 올리면 옵션도 큰다"를 표시만 보고 알 수 있다.
    /// 실제 증가량은 부위별 스케일을 반영한다(체력은 +10, 치명타확률은 +0.1).
    /// </summary>
    public static string DescribeLevelUpGain(EquipmentSlot slot, Grade grade, int level) {
        int lv = Mathf.Max(1, level);
        var sb = new System.Text.StringBuilder();

        var main = EquipmentStatLayout.Main(slot);
        int mainGain = MainGrowthPoints(lv + 1) - MainGrowthPoints(lv);
        if (mainGain > 0)
            sb.Append(GrowthRules.StatName(main.Stat)).Append(" +")
              .Append(StatFormat.FormatValue(main.Stat, mainGain * main.PerPoint));

        int count = EquipmentStatLayout.OptionCount(grade);
        for (int i = 0; i < count; i++) {
            int gain = OptionGrowthPoints(lv + 1, i) - OptionGrowthPoints(lv, i);
            if (gain <= 0) continue;
            var opt = EquipmentStatLayout.Option(slot, i);
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(GrowthRules.StatName(opt.Stat)).Append(" +")
              .Append(StatFormat.FormatValue(opt.Stat, gain * opt.PerPoint));
        }
        return sb.ToString();
    }

    /// <summary>장비 인스턴스 기준 다음 레벨업 상승치(DescribeLevelUpGain 편의 오버로드).</summary>
    public static string DescribeLevelUpGain(EquipmentInstanceDto inst, EquipmentData data) {
        if (inst == null || data == null) return "";
        return DescribeLevelUpGain(data.Slot, data.Grade, inst.Level);
    }

    // ── 전용무기 ──

    /// <summary>이 장비 id 가 전용무기인지(캐릭터 id 대역에서 파생된 id 인지).</summary>
    public static bool IsSignature(int equipDataId) => equipDataId > SignatureIdOffset;

    /// <summary>전용무기 id 의 주인 캐릭터 id(전용무기가 아니면 0).</summary>
    public static int OwnerCharOf(int equipDataId) =>
        IsSignature(equipDataId) ? equipDataId - SignatureIdOffset : 0;

    /// <summary>캐릭터의 전용무기 id.</summary>
    public static int SignatureOf(int charDataId) => charDataId + SignatureIdOffset;

    /// <summary>이 장비가 지정 캐릭터의 "본인" 전용무기인지(장착 시 보너스 대상).</summary>
    public static bool IsOwnSignature(int equipDataId, int charDataId) =>
        charDataId != 0 && OwnerCharOf(equipDataId) == charDataId;

    // 전용무기 보너스 카탈로그(데이터 편집용). GradeText/BadgeCatalog 와 동일하게 앱 시작 시 1회 등록해
    // 프리팹마다 SO 를 지정하지 않아도 동작하게 한다. 미등록이면 아래 코드 폴백을 쓴다.
    private static SignatureBonusCatalog _bonusCatalog;

    /// <summary>전용무기 보너스 카탈로그를 기본값으로 등록한다(MainLifetimeScope 에서 1회).</summary>
    public static void SetSignatureBonusCatalog(SignatureBonusCatalog catalog) => _bonusCatalog = catalog;

    /// <summary>
    /// 전용무기 장착 보너스. 카탈로그에 규칙이 있으면 그것을(장비별 > 무기타입별 > 등급별 > 전체 우선순위),
    /// 없으면 등급별 최종데미지 폴백(N 10 / R 15 / SR 20 / SSR 25 / XR 30)을 쓴다.
    /// </summary>
    public static CombatStats SignatureBonus(Grade grade, WeaponType weaponType, int equipDataId) {
        if (_bonusCatalog != null && _bonusCatalog.TryResolve(equipDataId, grade, weaponType, out var rule))
            return rule.bonus;
        return new CombatStats { FinalDamage = FallbackFinalDamage(grade) };
    }

    /// <summary>전용무기 보너스 설명 문구(UI 출력용). 카탈로그 문구가 비어 있으면 스탯 델타로 자동 생성한다.</summary>
    public static string SignatureBonusDescription(Grade grade, WeaponType weaponType, int equipDataId) {
        if (_bonusCatalog != null && _bonusCatalog.TryResolve(equipDataId, grade, weaponType, out var rule)
            && !string.IsNullOrEmpty(rule.description))
            return rule.description;
        return StatFormat.DeltaSummary(SignatureBonus(grade, weaponType, equipDataId));
    }

    // 등급별 최종데미지 폴백: 10 + 5 x 등급(N 10 / R 15 / SR 20 / SSR 25 / XR 30).
    private static float FallbackFinalDamage(Grade grade) => 10f + 5f * Mathf.Clamp((int)grade, 0, 4);
}
