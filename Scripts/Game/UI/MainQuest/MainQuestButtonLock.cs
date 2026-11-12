using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 퀘스트를 일정 레벨까지 마치기(보상 수령까지) 전에는 지정한 버튼들을 누를 수 없게(회색) 둔다.
/// 처음 시작한 유저가 첫 스테이지를 깨고 그 보상을 받기 전에 다른 메뉴로 새어 나가지 않게 하는 용도다(기본: 퀘스트 1 = 1-1 클리어).
///
/// 버튼 자체의 스크립트는 건드리지 않고 interactable 만 바꾼다. 잠글 때 원래 값을 기억해 두었다가
/// 풀 때 그대로 되돌리므로, 원래 꺼져 있던 버튼이 잠금 해제로 켜지는 일은 없다.
///
/// 잠겨 있는 동안에는 안내(이미지 + 타입라이터 문구, 선택)를 띄워 지금 무엇을 해야 하는지 알려 준다.
/// 문구는 퀘스트를 진행 중일 때와 완료해 보상만 남았을 때 따로 쓰며, 상태가 바뀔 때만 다시 타이핑한다.
///
/// 퀘스트 현황을 아직 받지 못했을 때(null)는 잠그지 않는다. 받기 전에 잠가 버리면
/// 받는 데 실패했을 때 기존 유저까지 메뉴가 막히기 때문이다(잠금보다 진행이 우선).
/// </summary>
public sealed class MainQuestButtonLock : HudInjectedBehaviour {
    [Tooltip("이 퀘스트 레벨의 보상을 받으면(다음 퀘스트로 넘어가면) 잠금이 풀린다. 1 = 첫 퀘스트(1-1 클리어).")]
    [SerializeField] private int unlockQuestLevel = 1;

    [Tooltip("잠글 버튼들. 회색으로 보이려면 각 버튼의 Transition 이 Color Tint(Disabled Color)여야 한다.")]
    [SerializeField] private Button[] buttons;

    [Header("Guide (선택. 잠겨 있는 동안만 보인다)")]
    [Tooltip("안내 묶음(이미지 + 문구). 이 스크립트가 붙은 오브젝트나 그 부모이면 안 된다(꺼지면 구독도 끊긴다).")]
    [SerializeField] private GameObject guideRoot;
    [Tooltip("안내 문구. TMP 텍스트에 TmpTypewriter 를 붙여 연결한다.")]
    [SerializeField] private TmpTypewriter guideText;
    [Tooltip("퀘스트를 진행 중일 때의 문구.")]
    [SerializeField, TextArea] private string inProgressMessage = "첫 번째 메인 퀘스트를 완료해 보세요!";
    [Tooltip("퀘스트를 완료해 보상 수령만 남았을 때의 문구.")]
    [SerializeField, TextArea] private string completedMessage = "퀘스트 완료!\n메인 퀘스트를 눌러 보상을 받으세요!";

    private readonly CompositeDisposable _subs = new();
    private bool[] _original;   // 잠그기 직전의 interactable. 잠겨 있지 않으면 null.
    private string _guideShown; // 지금 띄운 안내 문구. 같은 문구를 다시 타이핑하지 않기 위해 기억한다.

    protected override void OnInjected() {
        _subs.Clear();
        if (Ctx.MainQuest == null) { Apply(null); return; }
        Ctx.MainQuest.Current.Subscribe(Apply).AddTo(_subs);
    }

    private void Apply(MainQuestDto quest) {
        bool locked = IsLocked(quest);
        SetLocked(locked);
        ShowGuide(locked ? (quest.IsCompleted ? completedMessage : inProgressMessage) : null);
    }

    // 지정 레벨의 보상을 받아 다음 퀘스트로 넘어가기 전까지 잠근다(목표를 채웠어도 보상을 받기 전이면 잠김).
    // 레벨 0 은 준비된 퀘스트를 전부 끝낸 상태라 잠그지 않는다.
    private bool IsLocked(MainQuestDto quest) {
        if (quest == null || !quest.HasQuest) return false;
        return quest.Level <= unlockQuestLevel;
    }

    private void SetLocked(bool locked) {
        if (buttons == null) return;

        if (locked) {
            if (_original != null) return;   // 이미 잠겨 있다(원래 값을 덮어쓰지 않게).
            _original = new bool[buttons.Length];
            for (int i = 0; i < buttons.Length; i++) {
                if (buttons[i] == null) continue;
                _original[i] = buttons[i].interactable;
                buttons[i].interactable = false;
            }
            return;
        }

        if (_original == null) return;
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null) buttons[i].interactable = _original[i];
        _original = null;
    }

    // message 가 null 이면 안내를 끈다. 문구가 바뀔 때만 처음부터 다시 타이핑한다.
    private void ShowGuide(string message) {
        bool show = message != null;
        if (guideRoot != null) guideRoot.SetActive(show);
        if (!show) { _guideShown = null; return; }
        if (message == _guideShown) return;

        _guideShown = message;
        if (guideText != null) guideText.Play(message);
    }

    private void OnDisable() {
        // 꺼질 때는 잠금과 안내를 풀어 둔다. 다시 켜지면 현황을 보고 다시 판단한다.
        _subs.Clear();
        SetLocked(false);
        ShowGuide(null);
    }

    private void OnDestroy() => _subs.Dispose();
}
