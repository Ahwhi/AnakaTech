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

    [Header("Common")]
    [Tooltip("유저 닉네임 또는 길드 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("유저 랭킹은 소속 길드 이름, 길드 랭킹은 길드장 닉네임. 값이 없으면 꺼진다.")]
    [SerializeField] private TMP_Text subNameText;
    [Tooltip("전투력 또는 업적 점수.")]
    [SerializeField] private TMP_Text scoreText;
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
        ApplyGuildMark(entry.SubIconId, marks);

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

        SetSubName(string.IsNullOrEmpty(entry.SubName) ? NoGuildText : entry.SubName);
        BindProfileButton(onProfile);
    }

    /// <summary>길드 줄. 아이콘은 길드 마크이고 보조 이름은 길드장이다.</summary>
    /// <param name="onProfile">길드 마크를 눌렀을 때(길드 프로필 팝업). 비우면 버튼이 숨는다.</param>
    public void BindGuild(RankingEntryDto entry, GuildMarkCatalog marks, string score, bool mine,
                          Action<int> onProfile = null) {
        ApplyCommon(entry, score, mine);

        SetActive(frameView != null ? frameView.gameObject : null, false);
        SetActive(portraitView != null ? portraitView.gameObject : null, false);

        ApplyGuildMark(entry.IconId, marks);

        SetSubName(entry.SubName);
        BindProfileButton(onProfile);
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

    private void SetSubName(string text) {
        if (subNameText == null) return;
        bool has = !string.IsNullOrEmpty(text);
        subNameText.gameObject.SetActive(has);
        if (has) subNameText.text = text;
    }

    private void BindProfileButton(Action<int> onProfile) {
        if (profileButton == null) return;
        profileButton.gameObject.SetActive(onProfile != null);
        profileButton.onClick.RemoveAllListeners();
        if (onProfile != null) profileButton.onClick.AddListener(() => onProfile(_targetId));
    }

    private static void SetActive(GameObject go, bool on) {
        if (go != null) go.SetActive(on);
    }
}
