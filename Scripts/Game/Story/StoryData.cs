using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리 대사 한 줄. 배경과 화자는 "바뀔 때만" 채우는 것이 규약이다.
///
/// 배경(background)을 비워 두면 직전 대사의 배경이 그대로 유지된다. 한 장면이 여러 대사로 이어지는 것이
/// 보통이라, 줄마다 같은 스프라이트를 다시 지정하게 하면 배경을 교체할 때 그 장면의 모든 줄을 고쳐야 한다.
/// 암전(blackBackground)은 "배경이 없는 상태"도 하나의 배경으로 다루기 위한 것이다. 스프라이트를 비우는 것이
/// "유지"를 뜻하므로, 검은 화면은 비움이 아니라 별도의 지정으로 표현해야 한다.
///
/// 화자(speakerName/speakerPortrait)를 둘 다 비우면 화자 없는 나레이션으로 보고 화자 묶음을 아예 끈다
/// (누가 말하는지 없는 서술은 이름표가 비어 있는 것이 아니라 이름표 자체가 없어야 한다).
/// </summary>
[Serializable]
public sealed class StoryLine {
    [Tooltip("이 줄부터 바뀌는 배경. 비우면 직전 배경을 그대로 유지한다.")]
    [SerializeField] private Sprite background;

    [Tooltip("이 줄부터 배경을 빈 검은 화면으로 바꾼다(장면 전환/암전). 체크하면 위 background 는 무시된다. " +
             "일반 배경으로 돌아가려면 뒷줄에서 체크를 풀고 background 를 지정하면 된다.")]
    [SerializeField] private bool blackBackground;

    [Tooltip("화자 이름. 비우고 초상도 비우면 나레이션(화자 묶음을 끈다).")]
    [SerializeField] private string speakerName;

    [Tooltip("화자 초상(선택). 이름만 있고 초상이 없어도 화자 묶음은 켜진다.")]
    [SerializeField] private Sprite speakerPortrait;

    [Tooltip("대사 본문. 한 글자씩 나타난다.\n\n" +
             "일부만 색을 주려면 TMP 리치 텍스트를 쓴다(태그는 글자 수에 들어가지 않아 타이핑 연출과 함께 동작한다).\n" +
             "  이름으로: <color=yellow>노랑</color>  (yellow / green / red / blue / orange / purple / white / black)\n" +
             "  코드로  : <color=#00FFFF>사이언</color>  (이름에 없는 색은 16진수로)\n\n" +
             "굵게 <b>..</b> / 기울임 <i>..</i> / 크기 <size=120%>..</size> 도 같은 방식으로 쓸 수 있다.\n" +
             "여는 태그와 닫는 태그의 수가 맞지 않으면 그 뒤 문장 전체가 물든다(저장할 때 경고로 알려 준다).")]
    [TextArea(2, 6)]
    [SerializeField] private string text;

    public Sprite Background => background;
    /// <summary>배경을 빈 검은 화면으로 둘지. 켜면 Background 는 무시된다.</summary>
    public bool BlackBackground => blackBackground;
    public string SpeakerName => speakerName;
    public Sprite SpeakerPortrait => speakerPortrait;
    public string Text => text;

    /// <summary>화자가 있는 줄인지. 이름과 초상이 모두 없으면 나레이션이다.</summary>
    public bool HasSpeaker => !string.IsNullOrEmpty(speakerName) || speakerPortrait != null;
}

/// <summary>
/// 스토리 1편(대사 묶음). 서버는 "이 스토리를 끝까지 봤는가"만 기억하므로 내용은 전부 이 에셋이 갖는다.
///
/// Key 는 서버 StoryCatalog 에 등록된 키와 반드시 같아야 한다. 다르면 완료 보고가 거부되어
/// 같은 스토리가 접속할 때마다 다시 재생된다.
/// </summary>
[CreateAssetMenu(fileName = "StoryData", menuName = "Anaka/Data/Story")]
public sealed class StoryData : ScriptableObject {
    [Header("Identity")]
    [Tooltip("서버와 공유하는 스토리 키(StoryKeys / 서버 StoryCatalog 와 일치해야 한다).")]
    [SerializeField] private string key;

    [Header("Lines")]
    [Tooltip("재생 순서대로의 대사 목록.")]
    [SerializeField] private List<StoryLine> lines = new();

    [Header("Options")]
    [Tooltip("건너뛰기 버튼을 쓸 수 있는지. 끄면 이 스토리는 끝까지 봐야 한다.")]
    [SerializeField] private bool skippable = true;

    public string Key => key;
    public IReadOnlyList<StoryLine> Lines => lines;
    public bool Skippable => skippable;

    /// <summary>재생할 수 있는 스토리인지(키와 대사가 모두 있어야 한다).</summary>
    public bool IsPlayable => !string.IsNullOrEmpty(key) && lines != null && lines.Count > 0;

#if UNITY_EDITOR
    /// <summary>
    /// 저장할 때 리치 텍스트 태그의 짝을 본다. 닫지 않은 태그는 오류를 내지 않고 그 뒤 문장을 통째로 물들이는데,
    /// 19편 x 수십 줄을 눈으로 검수할 수는 없어 여기서 잡는다.
    ///
    /// 실행에는 영향이 없는 검사이므로 에디터에서만 돈다. 여는 개수와 닫는 개수만 비교하는 얕은 검사라
    /// 순서가 엉킨 경우(<b><color=...></b></color>)는 잡지 못한다. 그건 TMP 가 알아서 넘긴다.
    /// </summary>
    private void OnValidate() {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++) {
            string body = lines[i]?.Text;
            if (string.IsNullOrEmpty(body)) continue;

            foreach (string tag in RichTextTags) {
                int open = CountOpenTags(body, tag);
                int close = CountOccurrences(body, "</" + tag + ">");
                if (open == close) continue;

                Debug.LogWarning(
                    $"[StoryData] {name} {i + 1}번째 대사의 <{tag}> 태그 짝이 맞지 않습니다" +
                    $"(여는 {open} / 닫는 {close}). 닫지 않으면 뒤 문장까지 적용됩니다.", this);
            }
        }
    }

    // 짝을 확인할 태그. 여기에 없는 태그는 검사하지 않는다(TMP 태그가 수십 개라 전부 볼 이유가 없다).
    private static readonly string[] RichTextTags = { "color", "b", "i", "size", "style" };

    private static int CountOccurrences(string source, string token) {
        int count = 0;
        for (int at = source.IndexOf(token, System.StringComparison.Ordinal); at >= 0;
             at = source.IndexOf(token, at + token.Length, System.StringComparison.Ordinal)) {
            count++;
        }
        return count;
    }

    /// <summary>
    /// 여는 태그의 수. 태그 이름 뒤에 오는 글자까지 봐야 한다.
    /// 그냥 "&lt;b" 를 세면 &lt;br&gt; 가, "&lt;i" 를 세면 &lt;indent&gt; 가 함께 잡혀 짝이 어긋난 것처럼 보인다.
    /// </summary>
    private static int CountOpenTags(string source, string tag) {
        string token = "<" + tag;
        int count = 0;

        for (int at = source.IndexOf(token, System.StringComparison.Ordinal); at >= 0;
             at = source.IndexOf(token, at + token.Length, System.StringComparison.Ordinal)) {
            int next = at + token.Length;
            if (next >= source.Length) break;

            // 이름이 끝나는 자리여야 그 태그다. <color=..> / <b> / <size 120%> 를 모두 받아들인다.
            char c = source[next];
            if (c == '>' || c == '=' || c == ' ') count++;
        }
        return count;
    }
#endif
}
