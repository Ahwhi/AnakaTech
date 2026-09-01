using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// AudioClip 전용 AssetReference. 인스펙터 드래그 시 오디오클립만 허용되어 오할당을 막고,
/// 무거운 오디오를 원격 번들로 분리한 채 카탈로그(SoundLibrary)에는 참조만 보관하게 한다.
/// (Unity 는 Sprite/Texture/GameObject 용 타입드 레퍼런스만 기본 제공하므로 AudioClip 용은 직접 정의한다.)
/// </summary>
[Serializable]
public class AssetReferenceAudioClip : AssetReferenceT<AudioClip> {
    public AssetReferenceAudioClip(string guid) : base(guid) { }
}
