using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 팝업 1개. 팝인(작게 -> 오버슈트) -> 홀드(기준 크기 세틀, 또렷하게) -> 소멸(상승 + 페이드아웃) 3단으로
/// 튀어 오르는 타격감을 낸 뒤 풀로 반환한다(PLANNING 1-14).
/// 일반/치명타는 서로 다른 프리팹(스타일)로 관리되므로 이 스크립트는 색/스타일을 건드리지 않는다.
/// 월드공간 TextMeshPro(3D) 사용을 전제로 한다(캔버스 불필요).
/// </summary>
[DisallowMultipleComponent]
public sealed class DamagePopup : MonoBehaviour {
    [SerializeField] private TMP_Text label;

    private Action<DamagePopup> _onDone;
    private Sequence _seq;
    private Renderer _labelRenderer;   // 3D TMP 의 MeshRenderer. 정렬순서를 유닛 위로 올리는 데 사용.

    public void Bind(Action<DamagePopup> onDone) => _onDone = onDone;

    // 겹침 방지 부채꼴의 좌우 최대 칸수(과한 연타 시 화면 밖으로 벗어나지 않게 폭을 제한).
    private const int MaxLane = 4;

    /// <param name="spreadIndex">근처에 겹쳐 뜬 최근 팝업 수. 0=중앙, 1=+1칸, 2=-1칸 ... 좌우 교대로 벌어진다.</param>
    /// <param name="text">숫자 대신 표시할 문구(MISS 등). null/빈 문자열이면 amount 를 숫자로 표시한다.</param>
    public void Play(Vector3 worldPos, float amount, BattleSettings s, int spreadIndex = 0, string text = null) {
        _seq?.Kill();

        // 겹칠수록 좌우로 벌린 시작 위치 + 상승하며 벌어진 방향으로 흘려 부채꼴로 퍼지게 한다.
        float lane = Lane(spreadIndex);
        float startX = worldPos.x + lane * s.DamagePopupSpreadStep;
        float driftX = lane == 0f ? 0f : Mathf.Sign(lane) * s.DamagePopupSpreadDrift;   // lane 0이면 수직 상승

        Vector3 start = new Vector3(startX, worldPos.y, worldPos.z);
        Vector3 hopPos = new Vector3(startX, worldPos.y + s.DamagePopupPopHop, worldPos.z);   // 팝인 때 살짝 튀어 오른 지점
        Vector3 end = new Vector3(startX + driftX, worldPos.y + s.DamagePopupRiseDistance, worldPos.z);

        transform.position = start;
        transform.localScale = Vector3.one * s.DamagePopupPopInStartScale;
        if (label != null) {
            // 회피처럼 숫자가 아닌 결과도 같은 연출(팝인->홀드->소멸)을 타야 타격감이 일관된다.
            label.text = string.IsNullOrEmpty(text) ? Mathf.RoundToInt(amount).ToString() : text;
            label.alpha = 1f;
        }

        // 유닛(캐릭터/몬스터) 정렬순서보다 위로 올려 팝업이 항상 앞에 보이게 한다.
        if (_labelRenderer == null && label != null) _labelRenderer = label.GetComponent<Renderer>();
        if (_labelRenderer != null) _labelRenderer.sortingOrder = s.DamagePopupSortingOrder;

        // 3단 페이즈: 팝인(오버슈트 + 살짝 튀어오름) -> 홀드(기준 크기 세틀, 알파 유지) -> 소멸(상승 + 페이드).
        _seq = DOTween.Sequence();
        // 1) 팝인: 작게 시작해 오버슈트 크기까지 탁 튄다(OutBack). 동시에 살짝 위로 hop.
        _seq.Append(transform.DOScale(Vector3.one * s.DamagePopupOvershootScale, s.DamagePopupPopInDuration).SetEase(Ease.OutBack));
        _seq.Join(transform.DOMove(hopPos, s.DamagePopupPopInDuration).SetEase(Ease.OutQuad));
        // 2) 홀드: 오버슈트에서 기준 크기로 되돌아오며 또렷하게 보여준다(알파 100% 유지).
        _seq.Append(transform.DOScale(Vector3.one * s.DamagePopupSettleScale, s.DamagePopupHoldDuration).SetEase(Ease.OutQuad));
        // 3) 소멸: 위로 떠오르며 이 구간에서만 페이드아웃한다.
        _seq.Append(transform.DOMove(end, s.DamagePopupFadeDuration).SetEase(Ease.OutSine));
        if (label != null)
            _seq.Join(DOTween.To(() => label.alpha, a => label.alpha = a, 0f, s.DamagePopupFadeDuration).SetEase(Ease.InQuad));
        _seq.OnComplete(() => _onDone?.Invoke(this));
    }

    // spreadIndex -> 부호 있는 칸: 0,1,2,3,4 -> 0,+1,-1,+2,-2 (중앙에서 좌우 교대). 폭은 MaxLane 로 제한.
    private static float Lane(int index) {
        if (index <= 0) return 0f;
        int magnitude = (index + 1) / 2;
        if (magnitude > MaxLane) magnitude = MaxLane;
        int sign = (index % 2 == 1) ? 1 : -1;
        return magnitude * sign;
    }

    private void OnDisable() => _seq?.Kill();
}
