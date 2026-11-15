# Anaka: Girl Squad — Unity Client

**아나카: 소녀 스쿼드** — 2D 서브컬처 방치형 RPG의 클라이언트 C# 소스.
2026년 8월 출시 후 라이브 운영 중인 상용 빌드 코드이며, 기획·클라이언트·서버·인프라·운영을 1인 개발로 진행하고 있습니다.

> **기술 문서 (아키텍처 · 에셋 배포 · 장애 사례 · 운영)**
> https://claude.ai/artifact/4tpaNHPCdVmNiXZR3ECpoL

| 항목 | 값 |
|---|---|
| 코드 버전 | **V1.14.0** — Android 빌드 27 / iOS 빌드 29 |
| 엔진 | Unity 6000.4.5f1 · URP 17.4 · IL2CPP (arm64) |
| 플랫폼 | Android (AAB, API 31~36) · iOS · PC Google Play Games |
| 서버 | 자체 개발 .NET 8 TCP 서버 (비공개 저장소) |
| 통신 | 커스텀 바이너리 패킷 (PDL.xml 코드 생성) + TLS |
| 규모 | C# 728 파일 / 108,441 라인 — 자동 생성 패킷 코드를 빼면 약 87,100 라인 |
| 라이브 | 출시 V1.6 → V1.14, 정기 업데이트 8회 |

---

## 기술 스택

| 구분 | 사용 | 용도 |
|---|---|---|
| DI | **VContainer** | 루트 + 씬별 LifetimeScope 12개, 생성자 주입 |
| 비동기 | **UniTask** | 부트 시퀀스, 패킷 응답 대기, 에셋 로드, 씬 전환 |
| 반응형 | **UniRx** | 서버 상태 → UI 바인딩, 배지 합성 |
| 연출 | **DOTween** | 팝업 트랜지션, 수치 카운트업, 전투 연출 |
| 에셋 | **Addressables 2.9** | 원격 그룹 16종, 버전별 카탈로그 분리 배포 |
| 결제 | **Unity IAP 5.4** | 서버 영수증 검증 후 확정 |
| 입력 | **Input System 1.19** | 직접 조작 콘텐츠(가상 조이스틱) |
| 푸시 | **Firebase Messaging** (Android) / **APNs 직접** (iOS) | `IPushProvider` 로 격리 |
| UI | **TextMeshPro** · UI Particle · unity-webview | 전역 텍스트, uGUI 파티클, 약관 웹뷰 |

소셜 로그인은 SDK 없이 **브라우저 OAuth 2.0 + PKCE**로 구현했습니다 (Google · Naver · Apple).

---

## 아키텍처

```
          [ Bootstrap Scene ]  DontDestroyOnLoad
                   |
          GameLifetimeScope   (Composition Root)
                   |
     +-------------+--------------+
     |             |              |
  Managers      BootSteps      Services (interface)
  Network        (8 steps)     IAssetService / INetworkService /
  Login                        IUIService / IIapService / IPushProvider ...
  Sound                               |
  Addressable                         |
     +-------------+------------------+
                   |
       Child LifetimeScope x 11
       Title / Main / Commission / GuildRaid / PvpBattle / Story /
       Doom / Expedition / Tower / Frontline / Heroes
                   |
              Scene Views
```

**계층 규칙**

- View는 Service 인터페이스만 안다. 구현체와 패킷 타입을 모른다.
- Service는 패킷 ↔ DTO 변환만 한다. 판정은 전부 서버.
- State(`PlayerStateStore`)는 통신하지 않는다. 순수 상태 저장소.
- 정적 싱글톤 없음. 진단 도구(`MemoryProbe`)만 예외로 정적 접근을 허용하고, 빌드에서는 호출 자체가 사라진다.

---

## 폴더 구조

```
Scripts/
├── App/            28f   2,301L   컴포지션 루트, 씬별 LifetimeScope 12개, 생명주기, 재접속 가드
├── Boot/           17f     623L   부트 파이프라인 (IBootStep 8단계)
├── Network/        11f     769L   TCP 세션, 송수신 버퍼, 인증서 검증, 서버 경로표
├── Packet/          3f  21,277L   패킷 직렬화 (PDL.xml 기반 자동 생성)
├── Services/       11f     271L   서비스 인터페이스 정의
├── Auth/            5f     486L   OAuth + PKCE, Apple / iOS 인증 시트 네이티브 연동
├── Terms/           2f     199L   약관 동의 플로우
├── Notice/         23f   1,034L   공지 (Supabase), 이벤트 페이지/배지
├── Diagnostics/     8f     757L   메모리 계측, 디바이스 로그 오버레이, 환경 보고
├── Web/             2f     231L   인앱 웹뷰, 외부 링크 정책
├── UI/             51f   5,255L   UIRoot, 팝업 베이스, 공용 위젯
└── Game/          532f  70,235L
    ├── UI/        257f  39,904L   기능별 화면 (40개 모듈)
    ├── Data/      131f  11,419L   ScriptableObject 데이터, 스킬 14종, 전투력 공식
    ├── Net/        75f   8,899L   기능별 통신 서비스 (Service + Contracts 페어 38개)
    ├── Battle/     49f   7,325L   공용 전투 무대, 유닛, 풀링, 콘텐츠별 Director
    ├── Push/        8f     737L   FCM / APNs 추상화
    ├── State/       5f   1,383L   플레이어 상태 저장소
    └── Story/       7f     568L   스토리 진행
```

루트에는 영속 매니저(`NetworkManager`, `LoginManager`, `SoundManager`, `AddressableManager`)와 씬 매니저 11종이 있습니다.

---

## 핵심 설계

### 1. 부트 파이프라인 — `Boot/`

- `IBootStep` 8단계: 초기화 → 서버 경로 → 접속 → 버전 체크 → Addressable → 콘텐츠 버전 → 로그인 → 약관
- 등록 순서 = 실행 순서. `GameLifetimeScope` 한 곳에서만 순서를 정한다. 서버 경로 단계는 운영 빌드에서만 등록.
- 각 단계는 UI를 모르고 `IProgress` 와 `BootStatus` 로만 진행을 알린다.

> **왜**: 실패를 "서버에 연결할 수 없습니다" 하나로 뭉뚱그리면 실제 원인을 놓친다. 실제로 라이브에서 이 문구의 원인이 네트워크가 아니라 앱과 원격 카탈로그의 짝 불일치였던 적이 있다. 어느 단계에서 끊겼는지를 `BootFailReason` 으로 분류해 남긴다.

> **왜**: 부트를 엔트리포인트가 아니라 `IBootFlow` 로 둔다. 재접속으로 타이틀에 돌아오면 같은 경로로 처음부터 다시 돈다.

### 2. 에셋 배포 — `AddressableManager`, `Boot/AddressableStep`

- 기동 시 `CheckForCatalogUpdates` → `UpdateCatalogs`, `preload_boot` 라벨 합산 용량 → 용량/예상시간 동의 → 바이트 단위 진행률.
- 원격 그룹은 요청 단위 재시도가 없어(RetryCount 0) 번들 하나의 일시 실패가 곧 부트 실패가 된다. 부트 단계에서 1초 / 3초 백오프로 재시도하고, 이미 받은 번들은 캐시에서 이어 받는다.
- 카탈로그는 `catalog_[앱 버전]` 으로 분리하고 번들은 해시 이름으로 공존시켜, 스토어 심사·전파 기간에도 구버전 유저가 깨지지 않게 한다.

> **왜 인스턴스별 핸들인가**: `AssetReference.InstantiateAsync` 는 생성 핸들을 AssetReference 객체에 저장한다. 그 객체가 앱 전체가 공유하는 팝업 카탈로그(SO) 필드라서, 같은 팝업을 두 번 만들면 핸들이 덮이고 해제가 조용히 실패했다. 결과는 내용이 비고 입력만 먹는 "유령 팝업". 인스턴스 → 핸들 맵으로 직접 관리하고, 해제에 실패해도 오브젝트는 반드시 파괴한다.

### 3. DI 구성 — `App/`

- 루트 스코프(`GameLifetimeScope`)는 전용 Bootstrap 씬에 배치, `DontDestroyOnLoad`.
- 씬마다 자식 스코프를 두고 그 씬의 뷰를 등록.

> **왜 전용 씬인가**: 부트 오브젝트가 재로드되는 씬에 있으면 중복 생성과 자식 스코프의 부모 탐색 경합이 생긴다. 씬을 분리해 단일 인스턴스를 구조로 보장한다.

> **왜 자식 스코프인가**: 루트는 앱 시작 시 한 번만 빌드된다. 재접속으로 타이틀을 다시 로드하면 새로 생긴 뷰가 주입되지 않으므로, 씬 뷰는 자식 스코프가 등록한다.

### 4. 통신 계층 — `Network/`, `Game/Net/`

- 기능별 `XxxService` + `XxxContracts` 페어 38개 (쟁탈전, 길드, 상점, 의뢰, 멸망전, 원정대, 전선, 영웅전 등)
- 전 서비스 공통 패턴:

```csharp
var wait = _net.WaitPacketAsync<S_ArenaHome>(cts.Token);
_net.Send(new C_ArenaHome().Write());
var res = await wait;
```

- 응답 타임아웃 8초, `CancellationTokenSource.CreateLinkedTokenSource` 로 상위 취소와 연결.
- `Session` 은 소켓이 아니라 스트림 위에서 동작한다. 개발은 `NetworkStream`, 운영은 `SslStream` 이 들어와도 송수신 코드는 하나.
- 연결이 끊기면 `ConnectionGuard` 가 먼저 화면 그대로 조용히 다시 붙이고, 실패할 때만 안내 후 타이틀로 보낸다.
- 재연결은 새 서버 세션이므로 버전 핸드셰이크(`C_Version`)를 매번 다시 거친다. V1.14 이전에는 로그인 재연결 경로가 이를 건너뛰어, 가입 플랫폼이 비고 강제 업데이트·점검 판정도 빠지는 문제가 있었다.

> **왜 Contracts를 분리하나**: 패킷 구조체가 UI까지 흘러가면 패킷이 바뀔 때마다 UI가 깨진다. Service 경계에서 DTO로 끊는다.

> **목록 응답 페이지 분할 (V1.14)**: 한정 상품이 많이 쌓인 계정은 상점 카탈로그가 패킷 크기 한계(64KB)를 넘어, 접속 직후 끊기고 재접속을 반복했다. 상품 목록을 `S_ShopOfferPage` 여러 개로 나눠 먼저 보내고, 헤더(`S_ShopCatalog`)가 마지막에 와서 끝을 알린다. 모든 응답에 요청 번호가 실려 와서, 요청이 겹치거나 앞선 요청이 시간 초과로 버려져도 페이지가 섞이지 않는다.

> **하위 호환 규칙**: 패킷 필드는 기존 필드 뒤에만 추가한다. 구버전 클라는 모르는 뒤쪽 필드를 읽지 않고 넘어간다. 패킷 번호가 바뀌는 배포만 서버 핸드셰이크의 최소 버전으로 차단한다.

### 5. 상태 관리 — `Game/State/PlayerStateStore`

- 서버 권위 값의 로컬 캐시. 재화/기력/진행도를 `ReactiveProperty` 로 노출.
- UI와 전투가 구독. 서버 응답이 오면 `Apply` 로만 갱신.

> **왜 통신을 분리하나**: 저장소가 직접 서버를 부르면 "누가 이 값을 바꿨는가"를 추적할 수 없다. 호출은 Service, 반영은 Store로 나눈다.

### 6. 전투 — `Game/Battle/`, `Game/Data/Skills/`

- `BattleStage` 가 생성·배치·연출 풀·카메라·겹침 해소·스킬 런타임을 맡고, 콘텐츠는 Director가 규칙만 얹는다 (스테이지 · 의뢰 · 멸망전 · 길드 레이드 · PvP · 원정대 · 타워 · 전선 · 영웅전).
- 스킬은 ScriptableObject 에셋 14종 타입. 각 스킬이 `SkillPowerProfile` 로 전투력 기여를 스스로 신고한다.
- 자기 회복, 아군 전체 회복 같은 공통 부가 효과는 `UnitSkill` 베이스 필드로 두어 스킬 타입과 상관없이 조합한다. 기본값 0 = 효과 없음.
- 투사체 · VFX · 데미지 팝업 · 스킬 말풍선 풀링.

> **왜 주입식 확장인가**: 직접 조작 콘텐츠(영웅전)를 넣으면서 `IUnitMoveInput` 을 null 기본값으로 주입하게 했다. 주입하지 않은 기존 전투는 코드 경로가 그대로라, 라이브 중인 전투의 회귀를 구조로 막는다. 전체 공격의 대상별 타격 간격(V1.13, 프레임 분산)도 같은 원칙으로 기본값 0 = 기존 동작.

> **전투력 정밀도**: float 합산은 21억대에서 128 단위로만 구분된다. 합산은 double, 표시는 long, 카운트업 연출은 값이 아니라 0~1 진행률만 트윈한다.

### 7. UI — `UI/`

- `UIRoot` (`IUIService` 구현): 레이어 캔버스 Light 50 < Popup 100 < Overlay 200 < System 300. 호출부는 의도(팝업·토스트·로딩)만 말한다.
- `UIView` 는 모든 팝업의 베이스. 페이드 인/아웃, 닫힘 대기(`UniTaskCompletionSource`), Addressable 인스턴스 해제를 공통 제공.
- 배경 탭과 안드로이드 뒤로가기를 `Dismiss` 한 경로로 수렴. 뒤로가기는 연 순서 스택의 최상단 하나에만 간다.
- 로딩 오버레이는 참조 카운트. 동시에 여러 작업이 요청해도 모두 끝나야 닫힌다.
- 배지는 `BadgeCenter` 한 곳에서 합성하고, 메뉴 버튼은 그 하나만 구독한다.

> **왜**: 팝업마다 배경 버튼과 리스너를 다는 중복을 베이스가 흡수한다. 프리팹에서 배경을 할당한 팝업만 배경 탭으로 닫힌다(할당 = 사용).

### 8. 진단 — `Diagnostics/`

- `MemoryProbe`: 계측 지점 기록. `[Conditional("UNITY_EDITOR")]` 로 빌드에서는 호출 자체가 컴파일 단계에서 사라진다.
- `DeviceLogOverlay`: 케이블 없이 실기기 화면에서 로그 확인. 개발 빌드에서만 존재하고, 씬을 수정하지 않고 스스로 캔버스를 만든다.
- `SystemReport`: 화면/그래픽 환경 요약. Display 와 Screen 해상도가 어긋나면 렌더 타겟 재할당이 일어나는 신호라 함께 기록한다.

### 9. 개발 도구 — `DevelopTool.cs`, `ServerTool.cs`

- 서버 시계를 통째로 이동시켜 주간/일일 스케줄 콘텐츠(길드 대항전 매칭, 자정 초기화, 원정대 주간 정산)를 즉시 검증.
- 판정이 서버에 있으므로 시간 이동도 서버에 요청한다. 클라만 옮기면 카운트다운만 바뀌고 매칭/정산은 그대로다.
- **서버가 Debug 빌드일 때만 동작.** 운영 빌드는 거절 응답을 반환.

---

## 플랫폼 네이티브 연동

| 파일 | 내용 |
|---|---|
| `Auth/OAuthBrowserFlow.cs` | PKCE verifier / state 생성, 브라우저 OAuth 흐름 |
| `Auth/WebAuthNative.cs` | iOS `ASWebAuthenticationSession` — 앱 밖 브라우저로 나가지 않는 인증 시트 |
| `Auth/AppleAuthNative.cs` | Sign in with Apple — identityToken만 받아 서버가 검증 |
| `Game/Push/AndroidFcmProvider.cs` | FCM 토큰 |
| `Game/Push/IosApnsProvider.cs` | APNs 직접 등록 (Firebase Messaging 미사용) |
| `App/AppLifecycle.cs` | iOS 종료 시 엔진 셧다운 크래시(Unity UUM-143137) 우회 — 저장 후 네이티브 `_exit` 호출 |

- `IPushProvider` + `PushProviderFactory` 로 플랫폼 분기를 한 곳에 격리. 에디터에서는 아무것도 하지 않는 구현이 들어온다.
- Objective-C++ / Android 네이티브 소스(`.mm`, `.androidlib`)는 `Plugins/` 아래에 있어 이 저장소에는 포함되지 않습니다.

---

## 보안

- 클라이언트는 판정하지 않는다. 재화 소모, 전투 결과, 확률 추첨, 결제 검증, 입장 비용 계산 전부 서버.
- 접속 시간 보상은 하트비트 신호 간격으로 서버가 판정한다. 클라가 경과 시간을 숫자로 보내지 않는다.
- `client_secret` 은 앱에 포함하지 않는다. 네이버 OAuth는 서버가 코드 교환을 대행한다(`SocialAuthConfig` 주석 참조).
- 자동 로그인 토큰(`SessionTokenStore`)은 소셜 토큰이 아니라 우리 서버에서만 통하는 토큰이다. 서버는 해시만 보관하고, 한 번 쓰면 새 토큰으로 교체되므로 예전 값이 유출돼도 재사용되지 않는다.
- TLS 인증서는 표준 체인·도메인·기간 검증을 생략할 수 없고, 지문 핀은 그 위에 추가 조건으로만 붙는다.

---

## V1.5 이후 주요 변경

| 버전 | 클라이언트 변경 |
|---|---|
| V1.6~1.8 | 정식 출시, 키보드 높이 추적 입력창, 멸망전, 모집 마일리지 |
| V1.9 | 서버 경로표(심사 빌드 → 테스트 서버), iOS 전량 내장 Addressables 프로필, 웹 쿠폰 |
| V1.10 | 백그라운드 복귀 시 세션 재접속(씬 유지), 전투력 공식 교체(단조 증가), 패스 |
| V1.11 | 원정대 · 무한 타워 씬, 장비 한계 돌파 |
| V1.12 | 전선 · 영웅전(직접 조작, `IUnitMoveInput`), 아웃핏, 쟁탈전 룬, 등급 시너지 |
| V1.13 | 튜토리얼 UI, 길드 레이드 연속 처치, 랭킹 내 순위 칸, 전체 공격 프레임 분산, 전투력 21억 대응 |
| V1.14 | 스토리 리워크(11편), 상점 카탈로그 페이지 분할, 재연결 핸드셰이크 보강, 배지 합성 정리, 아군 전체 회복 스킬 옵션, 장비 한계돌파 채팅 알림 |

---

## 저장소 범위

- 이 저장소는 **클라이언트 C# 스크립트(`Assets/Scripts`)만** 포함합니다.
- 제외: Unity 씬/프리팹/아트 리소스, 에디터 도구(`Editor/`), 서드파티 플러그인(DOTween/UniRx/UniTask/VContainer), 네이티브 플러그인, 서버 소스, 설정 에셋(API 키·클라이언트 ID 포함).
- `Packet/GenPackets.cs` 는 서버의 `PDL.xml` 에서 자동 생성된 코드입니다. 직접 작성한 코드는 약 87,100 라인입니다.
- 빌드 가능한 형태가 아닙니다. 코드 리뷰 목적입니다.
