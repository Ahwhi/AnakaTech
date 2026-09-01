using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가챠 종류. 착지 연출 VFX 를 종류별로 따로 배선하기 위한 조회 키다.
/// 새 가챠(펫 등)가 생기면 여기에 추가하고 UIVfxCatalog 에 세트를 하나 늘리면 된다.
/// </summary>
public enum GachaVfxKind {
    Character,
    Equipment
}

/// <summary>
/// UI 파티클 연출(UIParticle 프리팹) 카탈로그. 이름키/등급으로 프리팹을 조회한다(코드가 아닌 데이터로 관리 - 규칙 5).
/// 가챠 착지(종류별/등급별)·레벨업 등 UI VFX 를 프리팹마다 이 SO 에서 배선한다.
/// MainLifetimeScope 가 RegisterInstance 로 등록해 MainHudContext 로 주입된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/UI VFX Catalog", fileName = "UIVfxCatalog")]
public sealed class UIVfxCatalog : ScriptableObject {
    [Serializable]
    public struct NamedEntry {
        public string key;
        public GameObject prefab;
    }

    [Serializable]
    public struct GradeEntry {
        public Grade grade;
        public GameObject prefab;
    }

    /// <summary>
    /// 가챠 한 종류의 착지 연출 묶음(등급별 프리팹 + 공용 Fallback).
    /// 조회 규칙이 이 안에 갇혀 있으므로 가챠 종류가 늘어도 카탈로그 본체는 그대로다.
    /// </summary>
    [Serializable]
    public sealed class GachaVfxSet {
        [Tooltip("착지 연출을 등급별로. 해당 등급이 없으면 아래 Fallback 을 쓴다.")]
        [SerializeField] private GradeEntry[] byGrade;
        [Tooltip("등급 매핑이 없을 때 쓰는 이 가챠의 공용 착지 VFX.")]
        [SerializeField] private GameObject fallback;

        private Dictionary<Grade, GameObject> _map;

        /// <summary>등급에 대응하는 VFX(없으면 Fallback, 그것도 비어 있으면 null).</summary>
        public GameObject Get(Grade grade) {
            if (_map == null) {
                _map = new Dictionary<Grade, GameObject>();
                if (byGrade != null)
                    foreach (var e in byGrade) _map[e.grade] = e.prefab;
            }
            return _map.TryGetValue(grade, out var p) && p != null ? p : fallback;
        }

        /// <summary>인스펙터에서 값이 바뀌었을 때 캐시를 버린다.</summary>
        public void Invalidate() => _map = null;
    }

    [Header("Named (예: levelup)")]
    [Tooltip("이름 키로 조회하는 UI VFX 프리팹들.")]
    [SerializeField] private NamedEntry[] named;

    [Header("Gacha")]
    [Tooltip("캐릭터 가챠 착지 연출(GachaPopup).")]
    [SerializeField] private GachaVfxSet characterGacha = new GachaVfxSet();
    [Tooltip("장비 가챠 착지 연출(EquipGachaPopup).")]
    [SerializeField] private GachaVfxSet equipmentGacha = new GachaVfxSet();

    private Dictionary<string, GameObject> _namedMap;

    /// <summary>이름 키로 UI VFX 프리팹을 조회한다(없으면 null).</summary>
    public GameObject Get(string key) {
        if (string.IsNullOrEmpty(key)) return null;
        EnsureNamed();
        return _namedMap.TryGetValue(key, out var p) ? p : null;
    }

    /// <summary>가챠 착지 VFX(가챠 종류별 + 등급별, 없으면 해당 종류의 Fallback).</summary>
    public GameObject GachaVfx(GachaVfxKind kind, Grade grade) {
        var set = SetOf(kind);
        return set != null ? set.Get(grade) : null;
    }

    private GachaVfxSet SetOf(GachaVfxKind kind) => kind switch {
        GachaVfxKind.Equipment => equipmentGacha,
        _ => characterGacha
    };

    private void EnsureNamed() {
        if (_namedMap != null) return;
        _namedMap = new Dictionary<string, GameObject>();
        if (named != null)
            foreach (var e in named)
                if (!string.IsNullOrEmpty(e.key)) _namedMap[e.key] = e.prefab;
    }

    private void OnValidate() {
        _namedMap = null;
        characterGacha?.Invalidate();
        equipmentGacha?.Invalidate();
    }
}
