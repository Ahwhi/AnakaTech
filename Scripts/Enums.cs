public class Enums {
    public enum PlatformType {
        Windows,
        Android,
        IOS
    }

    public enum BootState {
        None,
        Initializing,
        ConnectingServer,
        CheckingVersion,
        DownloadingAddressable,
        LoggingIn,
        AgreeingTerms,
        Ready,
        Failed
    }

    /// <summary>
    /// 부트 실패 사유. State 가 Failed 일 때 IBootStatus.FailReason 으로 읽어 UI 분기에 쓴다.
    /// 점검은 버전 체크 단계에서 서버가 알려주면 Maintenance 로 분류한다(로그인 단계의 LoginFailReason.Maintenance 와 별개의 더 앞단 차단).
    /// </summary>
    public enum BootFailReason {
        None,
        CannotConnect,   // 연결/버전/로그인 라운드트립 타임아웃 등 - "서버에 연결할 수 없습니다" + 재시도
        ForceUpdate,     // 강제 업데이트 필요 - 스토어 이동
        Maintenance      // 서버 점검 중 - 점검 안내 팝업
    }

    public enum SfxKey {
        ButtonClick,
        Purchase,
        // 전투 관련(사운드 에셋은 개발 막바지에 SoundLibrary 에 할당. 미할당 시 무음).
        Attack,
        // 피격음은 "맞은 쪽이 무엇인가"로 갈린다. 캐릭터는 내 팀이든 쟁탈전 상대 팀이든 같은 소리를 쓴다
        // (살점/금속 대 몬스터의 이질적인 소리처럼 재질이 다르기 때문이며, 팀은 기준이 아니다).
        HitCharacter,
        Skill,
        UnitDeath,
        Revive,
        DungeonClear,
        Miss,         // 회피 성공(피격 무효).
        // 편성 화면의 카드 드래그. 집을 때와 슬롯에 놓을 때를 나눠 조작감을 준다.
        CardPick,
        CardPlace,
        // 가챠 카드 공개음(등급별). 착지 VFX 와 같은 기준으로 등급마다 다른 소리를 낸다.
        // 낮은 등급은 비워 두면 그대로 무음이 되므로, SSR/XR 만 배정해 임팩트만 주는 것도 가능하다.
        GachaRevealN,
        GachaRevealR,
        GachaRevealSR,
        GachaRevealSSR,
        GachaRevealXR,
        // 수련 보상 수령(효과 획득). 구매음(Purchase)과 구분해 따로 배정한다.
        TrainingComplete,
        // 퀘스트 완료 알림(서버가 완료를 알려 온 순간)과, 보상 수령이 성사된 순간(퀘스트/업적/미션/우편 등 공용).
        QuestComplete,
        Claim,
        // 성장 연출음. 장비 강화/각성은 VFX 와 같은 순간에, 캐릭터 별 승급은 VFX 없이 소리만 난다.
        EquipLevelUp,
        EquipAwaken,
        StarUp,
        // 카운트다운 한 칸(쟁탈전 매칭 후 자동 시작 5,4,3,2,1). 숫자가 바뀔 때마다 한 번씩 난다.
        Countdown,
        // 몬스터 피격음. 배정하지 않으면 캐릭터 피격음(HitCharacter)으로 대체된다.
        HitMonster,
        // 회복 아이템을 집었을 때(영웅전). 배정하지 않으면 무음이다.
        HealPickup         // 값이 SoundLibrary 에 직렬화되므로 항상 끝에만 추가한다.
    }

    public enum BgmKey {
        Title,
        Battle,
        Story         // 값이 SoundLibrary 에 직렬화되므로 항상 끝에만 추가한다.
    }
}
