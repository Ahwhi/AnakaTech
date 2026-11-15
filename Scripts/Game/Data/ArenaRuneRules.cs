using System;
using UnityEngine;

/// <summary>
/// 쟁탈전 룬(V1.12). 쟁탈전 전투에서만 양 팀에 걸리는 스탯 보정이다(예전 체력 10배 고정 보정을 대신한다).
///
/// 계열(Family)마다 1~5 레벨 중 하나만 낄 수 있고 레벨이 곧 코스트다. 총 코스트는 MaxCost 이하.
/// 저장/검사는 서버(ArenaRuneCatalog)가 하고, 효과 수치와 전투 적용은 여기서 한다(전투를 그리는 쪽이 클라라서).
/// 계열 순서와 수는 서버 ArenaRuneCatalog 와 반드시 같아야 한다(뒤에만 추가할 것).
/// </summary>
public static class ArenaRuneRules {
    /// <summary>룬 계열. 값이 저장 형식(CSV 자리)이므로 순서를 바꾸지 말 것.</summary>
    public enum Family {
        Attack = 0,
        AttackSpeed = 1,
        ArmorPen = 2,
        FinalDamage = 3,
        DamageTakenReduction = 4,
        Defense = 5,
        Health = 6,
    }

    public const int FamilyCount = 7;
    public const int MaxLevel = 5;
    public const int MaxCost = 7;

    // 계열 x 레벨(1~5) 수치. 배수 계열은 곱, 가산 계열(최종데미지/피격데미지감소)은 스탯에 더하는 값이다.
    // 행 순서는 Family 값과 같다. 계열별 밸런스 조정은 이 표만 고치면 표시와 전투 적용이 함께 바뀐다.
    private static readonly float[][] FamilyValues = {
        new[] { 1.5f, 2f, 2.5f, 3f, 5f },     // 공격력(x)
        new[] { 1.5f, 2f, 2.5f, 3f, 5f },     // 공격속도(x)
        new[] { 2f, 3f, 4f, 5f, 10f },        // 방어구관통력(x)
        new[] { 10f, 20f, 30f, 40f, 50f },    // 최종데미지(+%)
        new[] { 10f, 20f, 30f, 50f, 100f },   // 피격데미지감소(+%). 점근 감쇄(Resist01)라 100 을 더해도 무적이 되지 않는다.
        new[] { 2f, 3f, 4f, 5f, 10f },        // 방어력(x)
        new[] { 10f, 20f, 30f, 50f, 100f },   // 체력(x)
    };

    /// <summary>해당 계열의 레벨별 수치(배수 또는 가산 %). level 은 1~MaxLevel 로 보정된다.</summary>
    public static float ValueOf(Family f, int level) => FamilyValues[(int)f][Mathf.Clamp(level, 1, MaxLevel) - 1];

    private static bool IsAdditive(Family f) => f is Family.FinalDamage or Family.DamageTakenReduction;

    // 계열별 표시 색(룬 칸 / 효과 문구 공용).
    private static readonly string[] FamilyColors = {
        "#FF6B6B",   // 공격력
        "#FFB347",   // 공격속도
        "#C08BFF",   // 방어구관통력
        "#FF7FD0",   // 최종데미지
        "#6FD3FF",   // 피격데미지감소
        "#8FD694",   // 방어력
        "#7CFC9B",   // 체력
    };

    public static int CostOf(int level) => Mathf.Clamp(level, 0, MaxLevel);

    public static string FamilyName(Family f) => f switch {
        Family.Attack => "공격력",
        Family.AttackSpeed => "공격속도",
        Family.ArmorPen => "방어구관통력",
        Family.FinalDamage => "최종데미지",
        Family.DamageTakenReduction => "피격데미지감소",
        Family.Defense => "방어력",
        Family.Health => "체력",
        _ => ""
    };

    public static string ColorHex(Family f) => FamilyColors[(int)f];

    public static Color ColorOf(Family f) =>
        ColorUtility.TryParseHtmlString(ColorHex(f), out var c) ? c : Color.white;

    /// <summary>효과 값 문구(예: "x1.5", "+10%"). level 0 이면 빈 문자열.</summary>
    public static string ValueText(Family f, int level) {
        if (level <= 0) return "";
        float v = ValueOf(f, level);
        return IsAdditive(f) ? $"+{v:0}%" : $"x{v:0.#}";
    }

    /// <summary>룬 한 개의 이름(예: "공격력 x1.5").</summary>
    public static string RuneText(Family f, int level) => $"{FamilyName(f)} {ValueText(f, level)}";

    /// <summary>
    /// 스탯에 룬을 적용한다(쟁탈전 전투 전용, 양 팀 모두). 모든 보정(장비/각인/본부 등)이 끝난 최종 스탯 위에 얹는다.
    ///   배수 계열: 공격력/방어구관통력/방어력/체력은 값에 곱한다.
    ///   공격속도: 실제 공격 속도(1 + 공속%/100)에 곱한다(공속% 에 곱하면 0% 인 소녀는 효과가 없으므로).
    ///   가산 계열: 최종데미지/피격데미지감소 스탯에 더한다.
    /// </summary>
    public static CombatStats Apply(CombatStats s, ArenaRuneLoadout runes) {
        if (runes == null) return s;
        for (int f = 0; f < FamilyCount; f++) {
            int level = runes.LevelOf((Family)f);
            if (level <= 0) continue;
            float v = ValueOf((Family)f, level);
            switch ((Family)f) {
                case Family.Attack: s.Attack *= v; break;
                case Family.AttackSpeed: s.AttackSpeed = ((1f + s.AttackSpeed / 100f) * v - 1f) * 100f; break;
                case Family.ArmorPen: s.ArmorPen *= v; break;
                case Family.FinalDamage: s.FinalDamage += v; break;
                case Family.DamageTakenReduction: s.DamageTakenReduction += v; break;
                case Family.Defense: s.Defense *= v; break;
                case Family.Health: s.Health *= v; break;
            }
        }
        return s;
    }

    /// <summary>"5 / 7" 형태의 코스트 문구.</summary>
    public static string CostText(int used) => $"{used} / {MaxCost}";

    /// <summary>서버 ArenaRuneCatalog.Fail 값 -> 안내 문구.</summary>
    public static string FailMessage(int fail) => fail switch {
        1 => "로그인이 필요합니다.",
        2 => "룬 구성이 올바르지 않습니다.",
        3 => $"코스트는 최대 {MaxCost}까지 장착할 수 있습니다.",
        _ => "룬을 저장하지 못했습니다."
    };
}

/// <summary>
/// 룬 장착 구성 1벌(계열마다 레벨 0~5, 0 = 미장착). 서버와 주고받는 형식은 계열 순서 CSV 다.
/// 비었거나 깨진 값은 기본 구성(전 계열 1레벨)으로 읽는다(서버 ArenaRuneCatalog.Effective 와 같은 규칙).
/// </summary>
public sealed class ArenaRuneLoadout {
    private readonly int[] _levels = new int[ArenaRuneRules.FamilyCount];

    public static ArenaRuneLoadout Default() {
        var l = new ArenaRuneLoadout();
        for (int i = 0; i < l._levels.Length; i++) l._levels[i] = 1;
        return l;
    }

    public static ArenaRuneLoadout Parse(string csv) {
        if (string.IsNullOrWhiteSpace(csv)) return Default();
        var parts = csv.Split(',');
        if (parts.Length != ArenaRuneRules.FamilyCount) return Default();

        var l = new ArenaRuneLoadout();
        for (int i = 0; i < parts.Length; i++) {
            if (!int.TryParse(parts[i].Trim(), out int lv) || lv < 0 || lv > ArenaRuneRules.MaxLevel) return Default();
            l._levels[i] = lv;
        }
        return l.TotalCost <= ArenaRuneRules.MaxCost ? l : Default();
    }

    public string ToCsv() => string.Join(",", _levels);

    public ArenaRuneLoadout Clone() {
        var l = new ArenaRuneLoadout();
        Array.Copy(_levels, l._levels, _levels.Length);
        return l;
    }

    public int LevelOf(ArenaRuneRules.Family f) => _levels[(int)f];

    public int TotalCost {
        get {
            int sum = 0;
            foreach (int lv in _levels) sum += ArenaRuneRules.CostOf(lv);
            return sum;
        }
    }

    /// <summary>이 계열을 level 로 바꿨을 때의 총 코스트(같은 계열은 하나만 끼므로 기존 것을 빼고 센다).</summary>
    public int CostIfSet(ArenaRuneRules.Family f, int level) =>
        TotalCost - ArenaRuneRules.CostOf(_levels[(int)f]) + ArenaRuneRules.CostOf(level);

    public void Set(ArenaRuneRules.Family f, int level) => _levels[(int)f] = Mathf.Clamp(level, 0, ArenaRuneRules.MaxLevel);

    public bool SameAs(ArenaRuneLoadout other) {
        if (other == null) return false;
        for (int i = 0; i < _levels.Length; i++) if (_levels[i] != other._levels[i]) return false;
        return true;
    }
}
