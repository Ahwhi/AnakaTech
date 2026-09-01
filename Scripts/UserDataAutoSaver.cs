using System;
using UniRx;
using UnityEngine;

/// <summary>
/// SaveFlag 가 설정된 경우 주기적으로 유저 데이터를 디스크에 저장한다.
/// 부트 오브젝트(GameLifetimeScope 가 올라간 오브젝트)에 부착해 사용.
/// </summary>
public sealed class UserDataAutoSaver : MonoBehaviour {
    [SerializeField] private float intervalSeconds = 5f;

    private void Start() {
        Observable.Interval(TimeSpan.FromSeconds(intervalSeconds))
            .Where(_ => UserDataManager.SaveFlag)
            .Subscribe(_ => UserDataManager.Save())
            .AddTo(this);
    }
}
