using VContainer.Unity;
#if UNITY_IOS && !UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using UnityEngine;
#endif

/// <summary>
/// 광고 설치 전환 측정(엔트리포인트). iOS 에서만 Firebase(Core + Analytics)를 켠다.
///
/// Google Ads 의 iOS 앱 설치 전환은 Firebase Analytics 가 자동으로 남기는 first_open 이벤트를 가져와 쓴다.
/// 그래서 여기서는 Firebase 를 초기화해 수집을 시작시키기만 하고, 직접 남기는 이벤트는 없다.
///
/// 플랫폼별 범위
///   iOS     : Core + Analytics 만. 푸시는 APNs 직접 연동(IosApnsProvider)이라 Messaging 은 넣지 않는다.
///   Android : 건드리지 않는다. Firebase 는 FCM(AndroidFcmProvider)만 쓰고 수집은 매니페스트에서 꺼 두었다.
///             Android 설치 전환은 Google Play 연동으로 SDK 없이 잡힌다.
/// 추적 동의(ATT) 팝업은 띄우지 않는다. 동의 없이도 first_open 수집과 전환 보고는 된다.
/// </summary>
public sealed class InstallAttribution : IStartable {
    public void Start() {
#if UNITY_IOS && !UNITY_EDITOR
        InitializeAsync().Forget();
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    // 실패해도 게임은 그대로 돌아간다(전환 측정만 빠진다).
    private static async UniTaskVoid InitializeAsync() {
        try {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (status != DependencyStatus.Available) {
                Debug.LogWarning($"[Attribution] Firebase 를 사용할 수 없습니다: {status}");
                return;
            }
            // 기본 앱을 만들어야 네이티브 SDK 가 GoogleService-Info.plist 로 구성되고 수집이 시작된다.
            _ = FirebaseApp.DefaultInstance;
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            Debug.Log("[Attribution] Firebase Analytics 시작");
        } catch (Exception e) {
            Debug.LogWarning($"[Attribution] Firebase 초기화 실패: {e.GetType().Name}");
        }
    }
#endif
}
