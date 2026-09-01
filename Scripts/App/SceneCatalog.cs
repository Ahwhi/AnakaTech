using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 씬 프리팹(어드레서블 씬) 참조 카탈로그. UIPopupCatalog 와 같은 철학으로, 문자열 주소 대신 AssetReference 를
/// 인스펙터에서 드래그로 연결해 오타/주소 불일치를 원천 차단한다.
///
/// 씬 사이를 오가는 코드(의뢰 진입/복귀 등)가 서로의 씬 이름을 문자열로 알 필요가 없어진다.
/// 새 씬을 추가할 때는 필드 하나 + 프로퍼티 하나를 더하고 인스펙터에서 할당하면 된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Scene Catalog", fileName = "SceneCatalog")]
public sealed class SceneCatalog : ScriptableObject {
    [Tooltip("메인 씬(방치 전투). 의뢰 씬에서 '나가기' 로 돌아올 대상.")]
    [SerializeField] private AssetReference mainScene;
    [Tooltip("의뢰 씬. 메인의 의뢰 팝업에서 '의뢰도전' 으로 진입할 대상.")]
    [SerializeField] private AssetReference commissionScene;
    [Tooltip("길드 레이드 씬. 길드 팝업의 레이드 패널에서 '참여' 로 진입할 대상.")]
    [SerializeField] private AssetReference guildRaidScene;
    [Tooltip("유저 대 유저 전투 씬. 길드 대항전의 '공격'(향후 아레나 등)에서 진입할 대상.")]
    [SerializeField] private AssetReference pvpBattleScene;
    [Tooltip("스토리 씬. StoryPlaybackRequest 로 재생할 스토리와 끝난 뒤 갈 씬을 예약하고 진입한다.")]
    [SerializeField] private AssetReference storyScene;

    public AssetReference MainScene => mainScene;
    public AssetReference CommissionScene => commissionScene;
    public AssetReference GuildRaidScene => guildRaidScene;
    public AssetReference PvpBattleScene => pvpBattleScene;
    public AssetReference StoryScene => storyScene;
}
