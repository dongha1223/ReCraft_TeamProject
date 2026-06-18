using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 보스2 상승 플랫폼을 흰 박스 → 타일맵 비주얼로 일괄 변환하는 에디터 유틸.
/// 물리는 기존 BoxCollider2D+PlatformEffector2D가 담당,
/// Grid+Tilemap_Platform 자식은 비주얼 전용.
/// </summary>
public static class PlatformTilemapConverter
{
    private static readonly string[] PlatformNames =
        { "Platform_HL", "Platform_HR", "Platform_ML", "Platform_MR" };

    [MenuItem("Boss2/Convert Platforms to Tilemap")]
    static void ConvertPlatforms()
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>(
            "Assets/Sprites/Tiles/TileAssets/tile_lava_0.asset");

        if (tile == null)
        {
            Debug.LogError("[PlatformConverter] tile_lava_0.asset을 찾을 수 없습니다.");
            return;
        }

        int platformLayer = LayerMask.NameToLayer("Platform");
        if (platformLayer < 0) platformLayer = 10; // fallback: layer 10

        foreach (string pName in PlatformNames)
        {
            var go = GameObject.Find(pName);
            if (go == null)
            {
                Debug.LogWarning($"[PlatformConverter] {pName} 오브젝트를 찾지 못했습니다.");
                continue;
            }

            // 기존 메쉬 렌더링 컴포넌트 제거
            var mf = go.GetComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>();
            if (mf != null) Undo.DestroyObjectImmediate(mf);
            if (mr != null) Undo.DestroyObjectImmediate(mr);

            // 스케일 리셋 (크기 정보는 BoxCollider2D.size로 이전)
            Undo.RecordObject(go.transform, "Convert Platform Scale");
            go.transform.localScale = Vector3.one;

            // BoxCollider2D: 원래 4×0.5 크기를 collider size로 명시
            var col = go.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Undo.RecordObject(col, "Convert Platform Collider");
                col.size   = new Vector2(4f, 0.5f);
                col.offset = new Vector2(0f, 0.25f);   // 상단 y=0.5, 하단 y=0
            }

            // Grid 자식 생성
            // localPosition(-2, -0.5, 0) → 타일 x: -2..2, y: -0.5..0.5 (상단 y=0.5 = 콜라이더 상단)
            var gridGO = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridGO, "Convert Platform Grid");
            gridGO.transform.SetParent(go.transform, false);
            gridGO.transform.localPosition = new Vector3(-2f, -0.5f, 0f);
            gridGO.transform.localScale    = Vector3.one;
            gridGO.AddComponent<Grid>();

            // Tilemap 자식 생성 (비주얼 전용)
            var tmGO = new GameObject("Tilemap_Platform");
            Undo.RegisterCreatedObjectUndo(tmGO, "Convert Platform Tilemap");
            tmGO.transform.SetParent(gridGO.transform, false);
            tmGO.layer = platformLayer;

            var tm = tmGO.AddComponent<Tilemap>();
            tmGO.AddComponent<TilemapRenderer>(); // SortingLayerID=0(Default), Order=0 기본값

            // 4칸 타일 페인팅 (0,0)~(3,0)
            for (int i = 0; i < 4; i++)
                tm.SetTile(new Vector3Int(i, 0, 0), tile);

            tm.RefreshAllTiles();

            EditorUtility.SetDirty(go);
            Debug.Log($"[PlatformConverter] {pName} 변환 완료");
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[PlatformConverter] 모든 플랫폼 변환 완료. 씬을 저장하세요.");
    }
}
