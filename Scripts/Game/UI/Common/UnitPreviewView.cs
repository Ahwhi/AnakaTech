using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 유닛 프리팹(CharacterUnit)을 UI 에 그대로 그리는 미리보기(아웃핏 확인용 등).
///
/// 유닛은 월드 스프라이트(마스크/프레임/그림자)로 그려지므로 UI 에 직접 넣을 수 없다. 그래서 화면 밖 먼 곳에
/// 유닛을 하나 세우고 전용 카메라로 찍어 RenderTexture 를 RawImage 에 띄운다. 카메라/무대는 켜질 때 만들고
/// 꺼질 때 지우므로 프리팹/씬에 따로 배치할 것이 없다(RawImage 와 이 컴포넌트만 있으면 된다).
///
/// 무대는 전투 카메라가 절대 비추지 않을 만큼 먼 좌표에 두므로 레이어를 따로 나누지 않는다.
/// 미리보기 유닛은 전투 로직이 꺼진 채(CharacterUnit.InitPreview) 서 있기만 한다.
/// </summary>
[RequireComponent(typeof(RawImage))]
public sealed class UnitPreviewView : MonoBehaviour {
    [Tooltip("미리보기 무대의 월드 좌표. 전투 화면이 비추지 않는 먼 곳이어야 한다.")]
    [SerializeField] private Vector3 stageOrigin = new Vector3(10000f, 10000f, 0f);
    [Tooltip("미리보기 카메라의 직교 크기(작을수록 크게 보인다). 유닛 한 개가 꽉 차게 맞춘다.")]
    [SerializeField] private float orthographicSize = 1.6f;
    [Tooltip("카메라가 무대 중심에서 벗어날 거리(유닛 그림이 위/아래로 치우쳐 있으면 맞춘다).")]
    [SerializeField] private Vector2 cameraOffset = Vector2.zero;
    [Tooltip("렌더 텍스처 해상도(정사각형 한 변 픽셀).")]
    [SerializeField] private int textureSize = 512;
    [Tooltip("배경색. 알파 0 이면 UI 배경이 비쳐 보인다.")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);

    // 미리보기 유닛의 마스크 대역. 전투 유닛(파티/적/아군)과 겹치지 않게 아주 큰 번호를 쓴다.
    private const int PreviewMaskSlot = 1200;

    private RawImage _image;
    private Camera _camera;
    private RenderTexture _texture;
    private CharacterUnit _unit;
    private CharacterUnit _unitPrefab;

    /// <summary>prefab 을 appearance 의 초상으로 세워 보여 준다. 같은 프리팹이면 다시 만들지 않고 초상만 바꾼다.</summary>
    public void Show(CharacterUnit prefab, CharacterAppearance appearance) {
        if (prefab == null || appearance.Character == null) { Clear(); return; }
        EnsureStage();

        if (_unit == null || _unitPrefab != prefab) {
            DestroyUnit();
            _unitPrefab = prefab;
            _unit = Instantiate(prefab, stageOrigin, Quaternion.identity);
            _unit.name = $"[Preview] {prefab.name}";
        }
        _unit.InitPreview(appearance, PreviewMaskSlot);
        _unit.transform.position = stageOrigin;

        if (_image != null) _image.enabled = true;
    }

    /// <summary>아무것도 보여 주지 않는다.</summary>
    public void Clear() {
        DestroyUnit();
        if (_image != null) _image.enabled = false;
    }

    private void EnsureStage() {
        if (_image == null) _image = GetComponent<RawImage>();

        if (_texture == null) {
            _texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32) { name = "UnitPreview" };
            _texture.Create();
        }
        if (_camera == null) {
            var go = new GameObject("UnitPreviewCamera");
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = backgroundColor;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 100f;
            _camera.targetTexture = _texture;
        }
        _camera.orthographicSize = orthographicSize;
        _camera.transform.position = stageOrigin + (Vector3)cameraOffset + new Vector3(0f, 0f, -10f);

        if (_image != null) _image.texture = _texture;
    }

    private void DestroyUnit() {
        if (_unit != null) Destroy(_unit.gameObject);
        _unit = null;
        _unitPrefab = null;
    }

    // 패널이 닫히면 무대를 통째로 걷는다(보이지 않는 카메라가 계속 그리지 않게).
    private void OnDisable() {
        DestroyUnit();
        if (_camera != null) Destroy(_camera.gameObject);
        _camera = null;
        if (_texture != null) { _texture.Release(); Destroy(_texture); }
        _texture = null;
    }
}
