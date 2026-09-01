# Anaka: Girl Squad — Unity Client

**아나카: 소녀 스쿼드**
모바일 서브컬쳐 방치형 RPG의 클라이언트 전체 소스. 
소셜 로그인, 인앱결제, 푸시, 실시간 길드 컨텐츠까지 포함한 상용 빌드 기준 코드입니다.

- **플랫폼**: Android / iOS (Unity 6000.4 LTS)
- **서버**: 자체 개발 .NET 8 TCP 서버 (별도 저장소)
- **통신**: 커스텀 바이너리 패킷 + TLS
- **규모**: 578 파일 / 79,884 라인 (자동 생성 코드 제외 시 약 64,000 라인)
- **코드버전**: iOS 1.5.0(빌드넘버 17) / Android 1.5.0(빌드넘버 15) 기준

---

## 기술 스택

| 구분 | 사용 | 용도 |
|---|---|---|
| DI | **VContainer** | 씬 단위 LifetimeScope, 생성자 주입 |
| 비동기 | **UniTask** | 부트 시퀀스, 패킷 응답 대기, 씬 로드 |
| 반응형 | **UniRx** | 서버 상태 → UI 바인딩, 배지 갱신 |
| 연출 | **DOTween** | 팝업 트랜지션, 전투 연출 |
| 자산 | **Addressables** | 원격 번들, 컨텐츠 버전 분리 배포 |
| UI | **TextMeshPro** | 전역 |

---

## 아키텍처

```
        [ Bootstrap Scene ]  DontDestroyOnLoad
                 |
        GameLifetimeScope  (Root Composition Root)
                 |
   +-------------+-------------+
   |             |             |
Managers     BootSteps      Services
(Network/     (7 steps)     (interface)
 Login/                          |
 Sound/                          |
 Addressable)                    |
   |                             |
   +-------------+---------------+
                 |
        Child LifetimeScope  (Title / Main / Commission / GuildRaid / PvpBattle / Story)
                 |
              Scene Views
```

**계층 규칙**

- View는 Service 인터페이스만 안다. 구현체와 패킷 타입을 모른다.
- Service는 패킷 <-> DTO 변환만 한다. 판정은 전부 서버.
- State(`PlayerStateStore`)는 통신하지 않는다. 순수 상태 저장소.
- 정적 싱글톤 없음. 전역 접근자를 전부 제거하고 주입으로 대체.

---

## 폴더 구조

```
Scripts/
├── App/            18f   1,408L   컴포지션 루트, 씬별 LifetimeScope, 생명주기
├── Boot/           16f     517L   부트 파이프라인 (IBootStep 7단계)
├── Network/         9f     688L   TCP 세션, 송수신 버퍼, 인증서 검증
├── Packet/          3f  15,864L   패킷 직렬화 (PDL.xml 기반 자동 생성)
├── Services/       10f     263L   서비스 인터페이스 정의
├── Auth/            4f     359L   소셜 로그인 (Google / Apple / Naver)
├── Terms/           2f     199L   약관 동의 플로우
├── Notice/         21f     969L   공지 (Supabase), 이벤트 페이지/배지
├── Diagnostics/     8f     757L   메모리 계측, 디바이스 로그 오버레이
├── Web/             2f     231L   인앱 웹뷰, 외부 링크 정책
├── UI/             42f   4,116L   공용 UI 베이스 및 위젯
└── Game/          413f  51,000L
    ├── UI/        196f  29,287L   기능별 화면 (33개 모듈)
    ├── Data/      108f   7,990L   ScriptableObject 데이터, 스킬/공격 정의
    ├── Net/        59f   6,658L   기능별 통신 서비스 (Service + Contracts 페어)
    ├── Battle/     32f   4,715L   전투 연출, 유닛, 풀링
    ├── Push/        8f     737L   FCM / APNs 추상화
    ├── State/       5f   1,228L   플레이어 상태 저장소
    └── Story/       5f     385L   스토리 진행
```

루트에는 영속 매니저(`NetworkManager`, `LoginManager`, `SoundManager`, `AddressableManager`)와 씬 매니저 6종이 있습니다.

---

## 핵심 설계

### 1. 부트 파이프라인 — `Boot/`

- `IBootStep` 인터페이스로 7단계 분리: 초기화 → Addressable → 컨텐츠 버전 → 버전 체크 → 연결 → 약관 → 로그인
- 등록 순서 = 실행 순서. `GameLifetimeScope`에서만 순서를 정의한다.
- `Bootstrapper`가 진행 단계를 `BootStatus`에 기록 → 로딩 UI가 구독.

> **왜**: 실패를 "서버에 연결할 수 없습니다" 하나로 뭉뚱그리면 실제 원인(자산 로드 실패, 특정 패킷 무응답)을 놓친다. 어느 단계에서 끊겼는지를 `BootFailReason`으로 분류해 남긴다.

> **왜**: 부트를 엔트리포인트가 아니라 타이틀 진입 시 호출하는 `IBootFlow`로 둔다. 재접속으로 타이틀에 돌아오면 부트가 처음부터 자연스럽게 다시 돈다.

### 2. DI 구성 — `App/`

- 루트 스코프(`GameLifetimeScope`)는 전용 Bootstrap 씬에 배치, `DontDestroyOnLoad`.
- 씬마다 자식 스코프를 두고 그 씬의 뷰를 등록.

> **왜 전용 씬인가**: 부트 오브젝트가 재로드되는 씬에 있으면 중복 생성과 자식 스코프의 부모 탐색 경합이 발생한다. 씬을 분리해 단일 인스턴스를 구조적으로 보장한다.

> **왜 자식 스코프인가**: 루트는 앱 시작 시 한 번만 빌드된다. 재접속으로 타이틀을 다시 로드하면 새로 생긴 뷰가 주입되지 않으므로, 씬 뷰는 자식 스코프가 등록한다.

### 3. 통신 계층 — `Game/Net/`

- 기능별 `XxxService` + `XxxContracts` 페어 (쟁탈전, 길드, 상점, 의뢰, 도감 등 29개 기능)
- 전 서비스 공통 패턴:

```csharp
var wait = _net.WaitPacketAsync<S_ArenaHome>(cts.Token);
_net.Send(new C_ArenaHome().Write());
var res = await wait;
```

- 응답 타임아웃 8초, `CancellationTokenSource.CreateLinkedTokenSource`로 상위 취소와 연결.

> **왜 Contracts를 분리하나**: 서버가 보낸 패킷 구조체가 UI까지 흘러가면 패킷이 바뀔 때마다 UI가 깨진다. Service 경계에서 DTO로 끊는다.

### 4. 상태 관리 — `Game/State/PlayerStateStore`

- 서버 권위 값의 로컬 캐시. 재화/기력/진행도를 `ReactiveProperty`로 노출.
- UI와 전투가 구독. 서버 응답이 오면 `Apply`로만 갱신.

> **왜 통신을 분리하나**: 저장소가 직접 서버를 부르면 "누가 이 값을 바꿨는가"를 추적할 수 없다. 호출은 Service, 반영은 Store로 나눈다.

### 5. UI 베이스 — `UI/UIView`

- 모든 팝업의 베이스. 페이드 인/아웃, 닫힘 대기(`UniTaskCompletionSource`), Addressable 인스턴스 해제를 공통 제공.
- 배경 탭과 안드로이드 뒤로가기를 `Dismiss` 한 경로로 수렴.
- 닫힘 연출 중 재진입 가드(뒤로가기 연타, 닫기 버튼 더블 탭).

> **왜**: 팝업마다 배경 버튼 필드를 선언하고 리스너를 다시 다는 중복을 베이스가 흡수한다. 프리팹에서 배경을 할당한 팝업만 배경 탭으로 닫힌다(할당 = 사용).

### 6. 진단 — `Diagnostics/`

- `MemoryProbe`: 부트 단계마다 계측 지점 기록. 릴리즈 빌드에서는 `Conditional`로 호출 자체가 컴파일 단계에서 제거된다.
- `DeviceLogOverlay`: 실기기에서 로그 확인 (에디터 없이 QA 대응).

> **왜**: 단계 목록이 바뀌어도 계측은 `Bootstrapper` 한 줄만 따라간다. 스텝 코드는 진단의 존재를 모른다.

### 7. 개발 도구 — `DevelopTool.cs`

- 서버 시계를 통째로 이동시켜 주간/일일 스케줄 컨텐츠(길드 대항전 토요일 매칭, 레이드 자정 초기화)를 즉시 검증.
- 판정이 서버에 있으므로 시간 이동도 서버에 요청한다. 클라만 옮기면 카운트다운만 바뀌고 매칭/정산은 그대로다.
- **서버가 Debug 빌드일 때만 동작.** 운영 빌드는 거절 응답을 반환.

---

## 플랫폼 네이티브 연동

| 파일 | 내용 |
|---|---|
| `Auth/AppleAuthNative.cs` | Sign in with Apple (Objective-C interop) |
| `Game/Push/AndroidFcmProvider.cs` | FCM 토큰 (`AndroidJavaObject`) |
| `Game/Push/IosApnsProvider.cs` | APNs 등록 |

- `IPushProvider` 인터페이스 + `PushProviderFactory`로 플랫폼 분기를 한 곳에 격리.

---

## 보안 관련

- 클라이언트는 판정하지 않는다. 재화 소모, 전투 결과, 확률 추첨, 결제 검증 전부 서버.
- `client_secret`은 앱에 포함하지 않는다. 네이버 OAuth는 서버가 코드 교환을 대행(`SocialAuthConfig` 주석 참조).
- 세션 토큰은 `SessionTokenStore`가 플랫폼 보안 저장소에 위임.

---

## 저장소 범위

- 이 저장소는 **클라이언트 C# 스크립트만** 포함합니다.
- 제외: Unity 씬/프리팹/아트 리소스, 서드파티 플러그인(DOTween/UniRx/UniTask/VContainer), 서버 소스, 설정 에셋(API 키 포함).
- `Packet/GenPackets.cs`는 서버의 `PDL.xml`에서 자동 생성된 코드입니다. 직접 작성한 코드는 약 64,000 라인입니다.
- 빌드 가능한 형태가 아닙니다. 코드 리뷰 목적입니다.
