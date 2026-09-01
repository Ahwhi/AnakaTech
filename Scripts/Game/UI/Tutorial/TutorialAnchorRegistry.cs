using System;
using System.Collections.Generic;
using UniRx;

/// <summary>
/// 지금 화면에 떠 있는 튜토리얼 앵커들의 등록소.
///
/// 정적인 이유: 앵커는 대부분 런타임에 인스턴스화되는 팝업 프리팹 안에 들어 있어 DI 컨테이너가 닿지 않는다
/// (BadgeCatalog.Default 와 같은 이유). 앵커는 켜질 때 등록하고 꺼질 때 해제하므로, 이 목록은 언제나
/// "지금 눈에 보이는 지점"만 담는다. 가이드는 이 사실만으로 어디를 가리킬지 정할 수 있다.
///
/// 같은 Id 가 여러 개 등록될 수 있다(목록의 카드마다 붙는 경우 등). 이때는 먼저 등록된 것을 쓴다.
/// 목록은 위에서부터 만들어지므로 먼저 등록된 것이 곧 목록 앞쪽 항목이고, 처음부터 화면에 있던 것이
/// 나중에 덧붙은 사본(편성 슬롯에 옮겨 담긴 카드 등)보다 유도 대상으로 자연스럽다.
/// "어느 것을 고를지"를 등록 순서에 기대고 싶지 않으면 앵커 쪽에서 제외 조건(skipWhenActive)을 배선한다.
/// </summary>
public static class TutorialAnchorRegistry {
    private static readonly Dictionary<TutorialAnchorId, List<TutorialAnchor>> Anchors = new();
    private static readonly Subject<Unit> ChangedSubject = new();

    /// <summary>등록/해제가 일어났다는 신호. 가이드가 다시 판단하는 계기가 된다.</summary>
    public static IObservable<Unit> Changed => ChangedSubject;

    public static void Register(TutorialAnchor anchor) {
        if (anchor == null || anchor.Id == TutorialAnchorId.None) return;

        if (!Anchors.TryGetValue(anchor.Id, out var list)) {
            list = new List<TutorialAnchor>();
            Anchors[anchor.Id] = list;
        }
        if (list.Contains(anchor)) return;

        WarnIfGateMismatch(list, anchor);
        list.Add(anchor);
        ChangedSubject.OnNext(Unit.Default);
    }

    // 같은 Id 를 쓰는 앵커끼리 관문 배선이 엇갈리면 알린다.
    //
    // 같은 Id 가 여럿인 것 자체는 정상이다(목록의 카드마다 붙는 경우). 다만 그때는 같은 프리팹이라
    // 배선이 서로 같다. 한쪽만 통과 조건이 배선돼 있다면 대개 지우다 만 앵커가 남은 것이고,
    // 그런 앵커 하나가 순서 판정을 통째로 무너뜨리는데 화면에는 아무 단서도 남지 않는다.
    private static void WarnIfGateMismatch(List<TutorialAnchor> list, TutorialAnchor added) {
        if (WarnedIds.Contains(added.Id)) return;

        for (int i = 0; i < list.Count; i++) {
            var other = list[i];
            if (other == null || other.IsGate == added.IsGate) continue;

            WarnedIds.Add(added.Id);
            var gate = added.IsGate ? added : other;
            var plain = added.IsGate ? other : added;
            UnityEngine.Debug.LogWarning(
                $"[TutorialAnchor] '{added.Id}' 앵커가 서로 다르게 배선되어 있습니다. " +
                $"'{gate.name}' 는 통과 조건이 있고 '{plain.name}' 는 없습니다. " +
                "쓰지 않는 앵커가 남아 있으면 유도 순서가 그 앵커 때문에 건너뛰어집니다.", plain);
            return;
        }
    }

    private static readonly HashSet<TutorialAnchorId> WarnedIds = new();

    public static void Unregister(TutorialAnchor anchor) {
        if (anchor == null || !Anchors.TryGetValue(anchor.Id, out var list)) return;
        if (!list.Remove(anchor)) return;
        ChangedSubject.OnNext(Unit.Default);
    }

    /// <summary>
    /// 지금 가리킬 수 있는 앵커 1개(없으면 null). 먼저 등록된 것부터 본다.
    /// 꺼져 있거나, 화면 밖으로 밀려났거나, 제외 조건에 걸린 앵커는 건너뛴다.
    /// </summary>
    public static TutorialAnchor Find(TutorialAnchorId id) {
        if (id == TutorialAnchorId.None || !Anchors.TryGetValue(id, out var list)) return null;

        for (int i = 0; i < list.Count; i++) {
            var anchor = list[i];
            if (anchor != null && anchor.IsPointable) return anchor;
        }
        return null;
    }

    /// <summary>
    /// 관문 판정에 쓸 앵커 1개(없으면 null). 관문으로 배선된 앵커만 후보로 삼고, 그중 가리킬 수 있는 것을
    /// 우선하되 없으면 등록되어 있기만 해도 돌려준다.
    ///
    /// 표시 조건(가리킬 수 있는가)으로 거르지 않는 이유: 관문은 "이 단계를 통과했는가"를 묻는 것이라,
    /// 잠깐 흐려졌거나 눌 수 없는 상태라고 해서 없는 것으로 치면 관문이 조용히 사라져 뒤 단계로 건너뛴다.
    ///
    /// 관문이 아닌 앵커를 건너뛰는 이유: 같은 Id 를 쓰는 앵커가 실수로 하나 더 붙어 있을 때
    /// (조건을 배선하지 않은 채 남은 것 등) 그쪽이 먼저 잡히면 "관문이 없다"로 판정되어 순서가 통째로 무너진다.
    /// 하나라도 관문으로 배선돼 있으면 그 의도를 따르는 편이 안전하다.
    /// </summary>
    public static TutorialAnchor FindGate(TutorialAnchorId id) {
        if (id == TutorialAnchorId.None || !Anchors.TryGetValue(id, out var list)) return null;

        TutorialAnchor fallback = null;
        for (int i = 0; i < list.Count; i++) {
            var anchor = list[i];
            if (anchor == null || !anchor.IsGate) continue;
            if (anchor.IsPointable) return anchor;
            fallback ??= anchor;
        }
        return fallback;
    }

    /// <summary>등록을 전부 비운다(로그아웃/타이틀 복귀 등 화면이 통째로 사라지는 경우의 안전장치).</summary>
    public static void Clear() {
        if (Anchors.Count == 0) return;
        Anchors.Clear();
        ChangedSubject.OnNext(Unit.Default);
    }
}
