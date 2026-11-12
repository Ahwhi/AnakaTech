using UnityEngine;

/// <summary>
/// 전투 전역 튜닝값 집약 SO. 매직넘버를 코드에 흩뿌리지 않고 한 곳에서 조정한다.
/// MainLifetimeScope 인스펙터에 할당해 DI 로 주입한다.
/// </summary>
[CreateAssetMenu(fileName = "BattleSettings", menuName = "Anaka/Battle Settings")]
public sealed class BattleSettings : ScriptableObject {
    [Header("Combat")]
    [Tooltip("방어 감산 계수. 유효방어 = max(0, 방어 - 관통), 감산 = 유효방어 * 이 계수.")]
    public float DefenseFactor = 0.5f;
    [Tooltip("적(몬스터) 공통 피격 VFX 프리팹(PLANNING 1-9).")]
    public GameObject CommonEnemyHitVfxPrefab;

    [Header("Training Effects (수련 발동형 효과)")]
    [Tooltip("2회 공격이 발동했을 때 두 번째 타격이 나가기까지의 간격(초). 기본공격 연출(VFX/사운드)이 이 간격 뒤에 한 번 더 재생된다.")]
    public float DoubleAttackDelaySeconds = 0.1f;
    [Tooltip("회피에 성공했을 때 대상 위에 띄우는 문구. 데미지 팝업과 같은 연출을 탄다.")]
    public string MissText = "MISS";

    [Header("Damage Jitter")]
    [Tooltip("전역 데미지 지터 폭(%). 계산딜 100, 지터 30 이면 기본 70~130 사이에서 랜덤. 캐릭터의 최소/최대데미지비율이 이 폭을 재조형한다.")]
    public float DamageJitterPercent = 30f;

    [Header("Defensive Resist (점근 감쇄)")]
    [Tooltip("방어 스탯 3종(치명타저항/상태이상저항/피격데미지감소) 공통 점근 상수 K. 감쇄율 = 스탯/(스탯+K). "
           + "스탯이 K 와 같을 때 정확히 50% 감쇄가 되며, 클수록 저항 효율이 낮아진다. 100% 감쇄는 원천 불가.")]
    public float ResistSoftK = 100f;

    [Header("Attack Speed (기준 대비 %가속)")]
    [Tooltip("모든 캐릭터/몬스터 공통 기준 공격속도(초당 공격 횟수). 실제 공격간격 = (1/이 값) / (1 + 공속스탯%).")]
    public float BaseAttacksPerSecond = 0.8f;
    [Tooltip("공속 감쇠(소프트캡) 사용 여부. 개발 중에는 꺼두고, 라이브에서 상한이 필요할 때 켠다.")]
    public bool EnableAttackSpeedSoftCap = false;
    [Tooltip("감쇠 시작 지점(공속 스탯%). 이 값 초과분부터 점근 감쇠된다.")]
    public float AttackSpeedSoftCapStart = 200f;
    [Tooltip("감쇠 강도 상수 K(클수록 감쇠 약함). 유효초과 = K*초과/(K+초과).")]
    public float AttackSpeedSoftK = 300f;

    [Header("Move Speed (기준 대비 %가속)")]
    [Tooltip("모든 유닛 공통 기준 이동속도(월드 units/초). 실제 이동속도 = 이 값 * (1 + 이속스탯%).")]
    public float BaseMoveSpeed = 2f;

    [Header("Weapon Range (무기타입 고정 사거리, 투자/성장 불가)")]
    [Tooltip("검/너클(근접) 기본 사거리.")]
    public float SwordKnuckleRange = 1.5f;
    [Tooltip("스태프(마법) 기본 사거리.")]
    public float StaffRange = 6f;
    [Tooltip("총(총잡이) 기본 사거리.")]
    public float GunRange = 8f;
    [Tooltip("활(궁수) 기본 사거리.")]
    public float BowRange = 10f;

    [Header("Revive (PLANNING 1-12)")]
    [Tooltip("캐릭터 사망 후 부활까지 대기 시간(초). 현재 던전/의뢰 모두 부활이 없어 어느 컨텐츠도 이 값을 쓰지 않는다. " +
             "부활이 있는 컨텐츠를 새로 만들 때 그 무대가 ReviveDelaySeconds 로 이 값을 돌려주면 된다.")]
    public float ReviveDelaySeconds = 10f;
    [Tooltip("부활 대기 표시 형식. {0} 에 남은 초가 들어간다. 전투 캐릭터 슬롯의 쓰러짐 오버레이가 쓴다.")]
    public string ReviveTextFormat = "부활중 {0}초";
    [Tooltip("부활이 없는 컨텐츠(의뢰)에서 쓰러진 캐릭터에 표시할 문구. 남은 시간이 없으므로 형식 인자를 쓰지 않는다.")]
    public string DeadText = "전투불능";

    [Header("Knockback (PLANNING 1-13)")]
    public float KnockbackDistance = 0.4f;
    public float KnockbackDurationSeconds = 0.12f;
    [Tooltip("연속 넉백 방지 쿨타임(초).")]
    public float KnockbackCooldownSeconds = 0.5f;

    [Header("Hit Blink (PLANNING 1-13)")]
    [Tooltip("피격 시 캐릭터 스프라이트가 낮아지는 알파 배수(원래 알파 * 이 값).")]
    public float HitBlinkAlphaFactor = 0.3f;
    [Tooltip("깜빡임 1회(어두워졌다 밝아지는) 지속 시간(초).")]
    public float HitBlinkDuration = 0.06f;
    [Tooltip("깜빡임 왕복 횟수.")]
    public int HitBlinkLoops = 2;

    [Header("VFX")]
    [Tooltip("VFX 인스턴스가 풀로 반환되기까지의 수명(초).")]
    public float VfxLifetime = 1f;

    [Header("Status Effect (상태이상 표시)")]
    [Tooltip("상태이상이 걸려 있는 동안 유닛에 붙여 둘 VFX 프리팹. 종류마다 하나씩 배선한다. " +
             "여기 두는 이유는 어느 전장(방치/의뢰/레이드/대전)에서든 같은 상태이상이 같은 모습으로 보이게 하기 위함이다. " +
             "비워 두면 그 종류는 표시 없이 효과만 동작한다.")]
    public StatusEffectVfx[] StatusVfx = System.Array.Empty<StatusEffectVfx>();

    /// <summary>상태이상 종류에 대응하는 지속 표시 VFX 프리팹(미배선이면 null).</summary>
    public GameObject StatusVfxPrefab(StatusEffectType type) {
        if (StatusVfx == null) return null;
        for (int i = 0; i < StatusVfx.Length; i++)
            if (StatusVfx[i].Type == type) return StatusVfx[i].Prefab;
        return null;
    }

    [Tooltip("무적 상태 동안 유닛에 붙여 둘 공용 VFX 프리팹. 무적을 거는 쪽(스킬)이 따로 지정하지 않으면 이것을 쓴다. " +
             "비워 두면 표시 없이 무적만 동작한다.")]
    public GameObject InvulnerableVfxPrefab;

    [Header("Damage Popup (PLANNING 1-14)")]
    [Tooltip("팝인 시간(초). 작게 시작해 오버슈트 크기까지 튀어 오르는 구간. 짧을수록 탁 튄다.")]
    public float DamagePopupPopInDuration = 0.12f;
    [Tooltip("홀드 시간(초). 오버슈트에서 기준 크기로 되돌아오며 숫자를 또렷하게 보여주는 구간(알파 유지).")]
    public float DamagePopupHoldDuration = 0.1f;
    [Tooltip("소멸 시간(초). 위로 떠오르며 페이드아웃되는 구간. 이 동안에만 투명해진다.")]
    public float DamagePopupFadeDuration = 0.35f;
    [Tooltip("팝인 시작 크기(작게). 여기서 오버슈트 크기까지 확 커진다.")]
    public float DamagePopupPopInStartScale = 0.4f;
    [Tooltip("오버슈트 크기. 기준 크기보다 크게 튀었다가 되돌아온다(팝 느낌의 핵심).")]
    public float DamagePopupOvershootScale = 1.25f;
    [Tooltip("정착(기준) 크기. 홀드 이후 이 크기로 세틀되어 페이드된다.")]
    public float DamagePopupSettleScale = 1f;
    [Tooltip("팝인 순간 살짝 튀어 오르는 높이(월드 단위). 0이면 제자리에서 팝.")]
    public float DamagePopupPopHop = 0.12f;
    [Tooltip("소멸 구간에서 위로 떠오르는 거리(월드 단위).")]
    public float DamagePopupRiseDistance = 0.6f;
    [Tooltip("겹침 방지: 최근 이 시간(초) 안에 아래 반경 안에서 뜬 팝업 수만큼 좌우로 벌린다. 대략 팝업 수명과 맞춘다.")]
    public float DamagePopupSpreadWindow = 0.5f;
    [Tooltip("겹침 판정 반경(월드 단위). 이 반경 안의 최근 팝업들과 겹치지 않게 벌린다.")]
    public float DamagePopupSpreadRadius = 0.6f;
    [Tooltip("겹칠 때 한 칸당 좌우로 벌리는 거리(월드 단위). 0이면 벌리지 않음(기존처럼 겹침).")]
    public float DamagePopupSpreadStep = 0.35f;
    [Tooltip("상승하며 벌어진 방향(바깥)으로 추가로 흘리는 거리(월드 단위). 0이면 수직 상승.")]
    public float DamagePopupSpreadDrift = 0.25f;
    [Tooltip("팝업 렌더러 정렬순서. 모든 유닛(캐릭터/몬스터 마스크 대역) 위에 그려지도록 충분히 크게 둔다.")]
    public int DamagePopupSortingOrder = 10000;

    [Header("Skill Cooldown (시간 기반, PLANNING 1-18)")]
    [Tooltip("스킬 발동 주기의 하한(초). 아무리 쿨감/스킬가속을 쌓아도 이 아래로는 내려가지 않는다(무제한 투자 안전장치).")]
    public float SkillCooldownFloorSeconds = 1.5f;
    [Tooltip("스킬가속 점근 감소 상수 K(클수록 감쇠 약함=가속 효율 유지). 발동주기 = floor + (조정쿨-floor)*K/(K+가속).")]
    public float SkillAccelSoftK = 100f;

    [Header("Combat Power (표시용 전투력 산출)")]
    [Tooltip("전투력 표시 계산 튜닝값. 전투 밸런스에는 영향이 없고 화면에 보이는 숫자만 바뀐다(CombatPowerFormula).")]
    public CombatPowerSettings CombatPower = new CombatPowerSettings();

    [Header("Growth Formula")]
    [Tooltip("레벨 1 상승당 더해지는 스탯.")]
    public CombatStats LevelGrowthPerLevel;
    [Tooltip("별레벨 성장표(승급별 스탯 델타). 미할당이면 별 성장 없음. Anaka/Data/Star Growth Table 로 생성해 할당한다.")]
    public StarGrowthTable StarGrowthTable;

    [Header("Stat Point Value (투자 1포인트당 상승량)")]
    [Tooltip("공격력 1포인트당 상승량.")]
    public float PointAttack = 1f;
    [Tooltip("방어력 1포인트당 상승량.")]
    public float PointDefense = 1f;
    [Tooltip("체력 1포인트당 상승량(스케일이 커서 크게 준다).")]
    public float PointHealth = 10f;
    [Tooltip("공격속도 1포인트당 상승량(%p).")]
    public float PointAttackSpeed = 1f;
    [Tooltip("이동속도 1포인트당 상승량(%p).")]
    public float PointMoveSpeed = 1f;
    [Tooltip("스킬가속 1포인트당 상승량(%p).")]
    public float PointSkillAccel = 1f;

    [Header("Battle Formation (원형 배치 공용 기본값)")]
    [Tooltip("모든 던전에 적용되는 기본 배치. 특정 던전만 다르게 하려면 그 DungeonData 에서 " +
             "Override Formation 을 체크하고 값을 채운다(체크하지 않은 던전은 여기 값을 쓴다).")]
    public BattleFormation Formation = BattleFormation.Default;

    [Header("Character AI Movement (몬스터 탐지 후 접근)")]
    [Tooltip("캐릭터가 적을 탐지해 접근을 시작하는 거리. 이 안의 가장 가까운 적에게 이동한다. 0 이하면 항상 탐지(제한 없음).")]
    public float CharacterDetectRange = 0f;
    [Tooltip("캐릭터가 스폰(대형) 위치에서 최대로 전진할 수 있는 거리(리쉬). 이 범위 끝에서는 더 쫓지 않고 대기한다. 0 이하면 무제한.")]
    public float CharacterMaxAdvance = 3f;
    [Tooltip("탐지된 적이 없을 때 캐릭터가 스폰(대형) 위치로 걸어서 복귀하는지 여부.")]
    public bool CharacterReturnToSpawn = true;

    [Header("Unit Separation (겹침 방지)")]
    [Tooltip("유닛(캐릭터/몬스터) 겹침 방지 최소 중심 간격(월드). 0 이하면 몸통 반경만으로 판정한다. " +
             "근접 사거리(Range)보다 작게 둘 것. 크게 잡으면 서로 밀어내 사거리 밖으로 벗어나 공격을 못 한다. " +
             "몸통 반경(BodyRadius)이 설정된 유닛끼리는 반경 합이 더 크면 그쪽이 최소 간격이 된다.")]
    public float UnitSeparationDistance = 0.55f;
    [Tooltip("겹침 해소로 유닛이 한 프레임에 밀리는 최대 거리(월드). 여러 유닛이 겹쳐도 순간이동 없이 서서히 벌어지게 한다. 0 이하면 무제한.")]
    public float UnitSeparationMaxStep = 0.2f;

    [Header("Monster Spawn (PLANNING 1-21)")]
    [Tooltip("웨이브 스폰 간격(초).")]
    public float SpawnIntervalSeconds = 1.2f;
    [Tooltip("동시에 필드에 존재 가능한 최대 몬스터 수.")]
    public int MaxConcurrentMonsters = 12;

    [Header("Party Formation")]
    [Tooltip("휴식/대기 시 캐릭터 일렬 배치 간격.")]
    public float PartyFormationSpacing = 1.2f;
    [Tooltip("휴식 상태 배경(던전 밖 기본 스프라이트, PLANNING 3-4).")]
    public Sprite DefaultBackground;
    [Tooltip("휴식 상태 안내 문구.")]
    public string RestingText = "휴식중...";

    [Header("Camera (PLANNING 1-10,20)")]
    public float CameraDefaultZoom = 6f;
    public float CameraMinZoom = 3f;
    public float CameraMaxZoom = 10f;
    public float CameraFollowLerp = 5f;
    public float CameraShakeAmplitude = 0.25f;
    public float CameraShakeDuration = 0.18f;

    /// <summary>무기타입 고정 사거리(투자/장비/성장 무관). 캐릭터 최종 스탯 조립 시 Range 를 이 값으로 덮어쓴다.</summary>
    public float RangeFor(WeaponType weapon) => weapon switch {
        WeaponType.Sword   => SwordKnuckleRange,
        WeaponType.Knuckle => SwordKnuckleRange,
        WeaponType.Staff   => StaffRange,
        WeaponType.Gun     => GunRange,
        WeaponType.Bow     => BowRange,
        _ => SwordKnuckleRange
    };

    /// <summary>투자 포인트 1당 해당 스탯 상승량(투자 불가 스탯은 0).</summary>
    public float PointValue(StatType stat) => stat switch {
        StatType.Attack      => PointAttack,
        StatType.Defense     => PointDefense,
        StatType.Health      => PointHealth,
        StatType.AttackSpeed => PointAttackSpeed,
        StatType.MoveSpeed   => PointMoveSpeed,
        StatType.SkillAccel  => PointSkillAccel,
        _ => 0f
    };
}
