using System;
using UnityEngine;

/// <summary>
/// 메인 퀘스트 목표별 "어디를 차례로 눌러야 하는가"를 담는 경로표(SO).
///
/// 경로는 코드가 아니라 데이터다. 퀘스트가 늘거나 화면 동선이 바뀌어도 이 에셋만 고치면 되고,
/// 클라 빌드 없이 유도 문구를 다듬을 수 있다(퀘스트 정의 자체는 서버가 갖는다는 기존 구조와 같은 결).
///
/// 단계(Step)는 "순서대로 강제 진행되는 절차"가 아니라 "이 경로에 놓인 지점들의 나열"이다.
/// 가이드는 지금 화면에 보이는 지점 중 가장 뒤 단계를 가리키므로,
///   - 유저가 스스로 팝업을 먼저 열어도 그 다음 지점으로 자연스럽게 넘어가고,
///   - 뒤로 나가면 앞 단계로 저절로 되돌아온다.
/// 덕분에 "메인 -> 팝업 -> 탭 -> 버튼" 처럼 깊이가 제각각인 퀘스트를 분기 코드 없이 같은 방식으로 다룬다.
///
/// 단 탭 선택과 실행 버튼이 한 팝업에 같이 있으면 둘 다 동시에 보여 순서가 무너진다. 그런 단계는
/// 대상 오브젝트의 TutorialAnchor 에서 통과 조건(satisfiedWhenActive)을 배선해 관문으로 만든다.
/// 경로표는 순서만 적고, 순서를 강제할지는 앵커 쪽이 정한다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Tutorial Guide Routes", fileName = "TutorialGuideRoutes")]
public sealed class TutorialGuideRoutes : ScriptableObject {
    /// <summary>경로 위의 한 지점.</summary>
    [Serializable]
    public sealed class Step {
        [Tooltip("가리킬 지점. 대상 오브젝트에 TutorialAnchor 를 붙이고 같은 Id 를 고른다.")]
        public TutorialAnchorId anchor;

        [Tooltip("체크하면 이 단계에서 안내 문구를 띄운다. 기본은 끔이며, 화면이 문구로 덮이지 않도록 " +
                 "정말 설명이 필요한 지점에서만 켠다(프레임과 손만으로 알 수 있는 곳이 대부분이다).")]
        public bool showHint;

        [Tooltip("보여 줄 안내 문구(선택). showHint 를 켰을 때만 쓰인다. " +
                 "비워 두면 퀘스트 목표 문구를 그대로 쓴다.")]
        [TextArea(1, 3)]
        public string hint;

        [Tooltip("체크하면 가리키는 곳 외에는 눌리지 않게 막는다. 확실히 따라오게 하지만 " +
                 "유저가 다른 것을 볼 자유를 뺏으므로, 첫 몇 퀘스트에만 쓰는 것을 권한다.")]
        public bool blockOutside;
    }

    /// <summary>한 목표의 경로.</summary>
    [Serializable]
    public sealed class Route {
        [Tooltip("이 경로가 담당하는 퀘스트 목표.")]
        public MainQuestObjective objective;

        [Tooltip("목표의 인자가 이 값일 때만 쓰는 경로. -1 이면 인자와 무관하게 쓴다. " +
                 "같은 목표라도 대상이 다르면(스테이지 등) 갈라 쓰기 위한 자리.")]
        public int param = AnyParam;

        [Tooltip("앞에서부터 순서대로 놓인 지점들. 마지막 지점이 목표를 실제로 달성시키는 버튼이 되도록 만든다.")]
        public Step[] steps;

        [Tooltip("목표를 달성한 뒤 보상 수령으로 돌아오기까지 거칠 지점들(선택). 고르는 규칙은 steps 와 같다" +
                 "(보이는 것 중 가장 뒤). 여기 지점이 하나도 보이지 않으면 보상 수령 지점을 가리킨다.\n" +
                 "예: 모집을 마친 뒤 [가챠 메뉴 닫기, 결과 팝업 닫기] 를 넣으면 결과를 닫고 메뉴를 닫을 때까지 안내한다. " +
                 "비워 두면 달성 즉시 보상 수령을 가리킨다(기존 동작).")]
        public Step[] completedSteps;
    }

    /// <summary>Route.param 에 넣으면 인자를 따지지 않는다.</summary>
    public const int AnyParam = -1;

    [Tooltip("목표별 경로 목록. 여기에 없는 목표는 유도하지 않는다(그 퀘스트는 안내 문구만 뜬다).")]
    [SerializeField] private Route[] routes;

    [Tooltip("퀘스트를 완료했을 때 가리킬 지점(보통 메인 퀘스트의 보상 수령 버튼). " +
             "Anchor 를 None 으로 두면 완료 상태에서는 아무것도 가리키지 않는다.")]
    [SerializeField] private Step completedStep = new() { anchor = TutorialAnchorId.MainQuestClaim };

    [Tooltip("이 레벨까지만 유도한다(0 이면 제한 없음). 초반에는 손을 잡아 주고 그 뒤에는 " +
             "화면을 깨끗하게 두기 위한 값이다.")]
    [SerializeField] private int guideMaxLevel;

    /// <summary>완료 상태에서 가리킬 지점(설정하지 않았으면 null).</summary>
    public Step CompletedStep => completedStep != null && completedStep.anchor != TutorialAnchorId.None ? completedStep : null;

    /// <summary>이 레벨의 퀘스트를 유도 대상으로 볼지.</summary>
    public bool ShouldGuide(int level) => guideMaxLevel <= 0 || level <= guideMaxLevel;

    /// <summary>
    /// 목표에 맞는 경로(없으면 null). 인자까지 일치하는 경로를 우선하고, 없으면 인자를 따지지 않는 경로를 쓴다.
    /// </summary>
    public Route Find(MainQuestObjective objective, int param) {
        if (routes == null) return null;

        Route fallback = null;
        foreach (var route in routes) {
            if (route == null || route.objective != objective || route.steps == null || route.steps.Length == 0) continue;
            if (route.param == param) return route;
            if (route.param == AnyParam) fallback ??= route;
        }
        return fallback;
    }
}
