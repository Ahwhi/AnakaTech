using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 순위표 한 줄(유저 랭킹 / 길드 랭킹 공용).
///
/// 두 랭킹이 보여 주는 항목은 다르지만(유저 = 테두리ㆍ일러ㆍ이름ㆍ길드ㆍ전투력,
/// 길드 = 마크ㆍ이름ㆍ길드장ㆍ전투력) 자리 구성은 "아이콘 + 이름 + 보조 이름 + 점수"로 같다.
/// 그래서 컴포넌트를 나누지 않고 Bind 를 나눴고, 쓰지 않는 칸은 각 Bind 가 알아서 끈다
/// (프리팹은 랭킹별로 따로 만들어 필요한 칸만 배치하면 된다).
/// </summary>
public sealed class RankingSlot : MonoBehaviour {
    [Header("Rank")]
    [Tooltip("순위 표시. 목록에서는 숫자만 보이도록 컴포넌트의 Number Only 를 켜 두면 된다(색은 구간별로 적용된다).")]
    [SerializeField] private RankText rankText;
    [Tooltip("선택. 1~3위처럼 상위에만 켜는 표식(왕관 등). 배열 순서가 곧 1위, 2위, 3위다.")]
    [SerializeField] private GameObject[] topRankMarks;

    [Header("User (유저 / 업적 랭킹)")]
    [Tooltip("대표 캐릭터 일러 슬롯. 길드 랭킹에서는 꺼진다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("프로필 테두리. 길드 랭킹에서는 꺼진다.")]
    [SerializeField] private ProfileFrameView frameView;

    [Header("Guild Mark")]
    [Tooltip("길드 마크. 길드 랭킹에서는 그 길드의 마크, 유저 랭킹에서는 소속 길드의 마크를 보여준다. " +
             "유저가 무소속이면 꺼진다.")]
    [SerializeField] private Image guildMarkImage;
    [Tooltip("유저 줄에서 소속 길드 마크를 보여 줄지. 끄면 유저 줄에서는 길드 마크 칸이 꺼지고 길드 줄에서만 쓰인다 " +
             "(탭마다 내용이 바뀌는 내 순위 칸처럼 한 칸을 유저/길드가 같이 쓸 때 끈다).")]
    [SerializeField] private bool showAffiliationMark = true;

    [Header("Common")]
    [Tooltip("유저 닉네임 또는 길드 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("유저 랭킹은 소속 길드 이름, 길드 랭킹은 길드장 닉네임. 값이 없으면 꺼진다.")]
    [SerializeField] private TMP_Text subNameText;
    [Tooltip("유저 줄에서 무소속일 때 보조 이름('길드 없음')의 글자색. 소속이 있으면 프리팹에 둔 원래 색으로 돌아간다.")]
    [SerializeField] private Color noGuildColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [Tooltip("전투력 또는 업적 점수.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("선택. 길드 랭킹의 길드원 수(예: 12/30). 유저 줄에서는 꺼진다.")]
    [SerializeField] private TMP_Text memberCountText;
    [Tooltip("선택. 내 순위를 강조하는 배경.")]
    [SerializeField] private GameObject highlight;
    [Tooltip("선택. 줄 전체를 덮는 버튼. 유저 랭킹에서만 눌리며 그 유저의 프로필 팝업을 연다.")]
    [SerializeField] private Button profileButton;

    /// <summary>길드가 없는 유저의 소속 칸에 넣는 문구.</summary>
    private const string NoGuildText = "길드 없음";

    private int _targetId;

    /// <summary>
    /// 유저 줄(유저 랭킹 / 업적 랭킹 공용). 점수 문구만 다르므로 표시할 문자열을 받는다.
    /// </summary>
    /// <param name="score">이미 다듬은 점수 문구(전투력이면 숫자, 업적이면 "n점").</param>
    /// <param name="marks">소속 길드 마크 카탈로그. 없으면 마크 없이 이름만 나온다.</param>
    /// <param name="mine">내 줄인지(강조 배경).</param>
    public void BindUser(RankingEntryDto entry, BattleHudContext hud, GuildMarkCatalog marks,
                         string score, bool mine, Action<int> onProfile) {
        ApplyCommon(entry, score, mine);

        // 소속 길드의 마크. 무소속(SubIconId = 0)이면 카탈로그가 스프라이트를 주지 않아 저절로 꺼진다.
        ApplyGuildMark(showAffiliationMark ? entry.SubIconId : 0, marks);

        // 다른 유저의 테두리는 이 화면에 떠 있는 동안 바뀌지 않으므로 구독형이 아니라 1회 표시로 붙인다.
        if (frameView != null) {
            frameView.gameObject.SetActive(true);
            if (hud != null) frameView.Bind(hud.ProfileFrames, entry.IconId);
        }
        if (portraitView != null) {
            portraitView.gameObject.SetActive(true);
            // 상대가 끼운 코스튬까지 반영한 외형을 넘겨야 관심점/프레이밍이 그 그림에 맞게 적용된다.
            var appearance = hud != null && hud.Appearance != null
                ? hud.Appearance.Other(entry.RepresentativeCharId, entry.RepresentativeCostumeId)
                : default;
            if (appearance.Character != null) portraitView.Bind(appearance);
            else portraitView.Clear();
        }

        bool noGuild = string.IsNullOrEmpty(entry.SubName);
        SetSubName(noGuild ? NoGuildText : entry.SubName, noGuild ? noGuildColor : (Color?)null);
        SetActive(memberCountText != null ? memberCountText.gameObject : null, false);
        BindProfileButton(onProfile);
    }

    /// <summary>길드 줄. 아이콘은 길드 마크이고 보조 이름은 길드장이다.</summary>
    /// <param name="onProfile">길드 마크를 눌렀을 때(길드 프로필 팝업). 비우면 버튼이 눌리지 않는다.</param>
    public void BindGuild(RankingEntryDto entry, GuildMarkCatalog marks, string score, bool mine,
                          Action<int> onProfile = null) {
        ApplyCommon(entry, score, mine);

        SetActive(frameView != null ? frameView.gameObject : null, false);
        SetActive(portraitView != null ? portraitView.gameObject : null, false);

        ApplyGuildMark(entry.IconId, marks);

        SetSubName(entry.SubName);
        if (memberCountText != null) {
            memberCountText.gameObject.SetActive(true);
            memberCountText.text = $"{entry.SubValue}/{GuildRules.MaxMembers}";
        }
        BindProfileButton(onProfile);
    }

    /// <summary>
    /// 대상이 없는 줄(예: 길드 랭킹인데 무소속인 내 순위 칸). 순위/점수와 안내 문구만 남기고 나머지 칸은 끈다.
    /// </summary>
    public void BindEmpty(string name, string score) {
        ApplyCommon(new RankingEntryDto { Name = name }, score, false);
        SetActive(frameView != null ? frameView.gameObject : null, false);
        SetActive(portraitView != null ? portraitView.gameObject : null, false);
        ApplyGuildMark(0, null);
        SetSubName(null);
        SetActive(memberCountText != null ? memberCountText.gameObject : null, false);
        BindProfileButton(null);
    }

    // 길드 마크는 두 랭킹이 같은 자리를 쓰고 어느 길드의 마크인지만 다르다(길드 줄 = 그 길드, 유저 줄 = 소속 길드).
    // 마크가 없으면(무소속이거나 카탈로그 미할당) 이미지를 꺼서 빈 사각형이 남지 않게 한다.
    private void ApplyGuildMark(int markId, GuildMarkCatalog marks) {
        if (guildMarkImage == null) return;

        var sprite = markId > 0 && marks != null ? marks.Icon(markId) : null;
        guildMarkImage.gameObject.SetActive(sprite != null);
        guildMarkImage.sprite = sprite;
        guildMarkImage.enabled = sprite != null;
    }

    private void ApplyCommon(RankingEntryDto entry, string score, bool mine) {
        _targetId = entry.TargetId;

        if (rankText != null) rankText.SetRank(entry.Rank);
        if (nameText != null) nameText.text = entry.Name;
        if (scoreText != null) scoreText.text = score;
        if (highlight != null) highlight.SetActive(mine);

        // 상위 표식은 배열에 넣어 둔 만큼만 켠다(1위 표식만 두고 나머지를 비워 둬도 동작한다).
        if (topRankMarks != null)
            for (int i = 0; i < topRankMarks.Length; i++)
                if (topRankMarks[i] != null) topRankMarks[i].SetActive(entry.Rank == i + 1);
    }

    /// <param name="color">덮어쓸 글자색. null 이면 프리팹에 둔 원래 색을 쓴다(슬롯을 다시 Bind 해도 색이 남지 않게).</param>
    private void SetSubName(string text, Color? color = null) {
        if (subNameText == null) return;
        if (_subNameBaseColor == null) _subNameBaseColor = subNameText.color;

        bool has = !string.IsNullOrEmpty(text);
        subNameText.gameObject.SetActive(has);
        if (!has) return;
        subNameText.text = text;
        subNameText.color = color ?? _subNameBaseColor.Value;
    }

    // 보조 이름의 원래 글자색. 처음 쓸 때 한 번 기억해 두고 색을 되돌릴 때 쓴다.
    private Color? _subNameBaseColor;

    // 쓰지 않을 때는 오브젝트가 아니라 버튼 컴포넌트만 끈다. 버튼이 초상화 같은 표시 오브젝트에 함께 붙어 있으면
    // 오브젝트를 끄는 순간 그 표시까지 사라지기 때문이다(내 순위 칸처럼 눌리지 않는 슬롯).
    private void BindProfileButton(Action<int> onProfile) {
        if (profileButton == null) return;
        profileButton.enabled = onProfile != null;
        profileButton.onClick.RemoveAllListeners();
        if (onProfile != null) profileButton.onClick.AddListener(() => onProfile(_targetId));
    }

    private static void SetActive(GameObject go, bool on) {
        if (go != null) go.SetActive(on);
    }
}
