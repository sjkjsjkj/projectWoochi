using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Woochi.Editor
{
    // Editor-only bootstrap. Existing assets are reused; an existing scene is never rebuilt.
    public static class WoochiTilemapSetup
    {
        const string Root = "Assets/_Woochi";
        const string ScenePath = Root + "/Scenes/TilemapSandbox.unity";
        const string PalettePath = Root + "/Tiles/Palettes/WoochiStarter.prefab";

        [InitializeOnLoadMethod]
        static void ProcessSetupRequest()
        {
            // Only runs for an explicit, one-time local setup request.
            const string request = "Library/WoochiTilemapSetup.request";
            if (!File.Exists(request)) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(request)) return;
                File.Delete(request);
                try
                {
                    CreateStarterAssets();
                    bool dirty = false;
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        dirty |= SceneManager.GetSceneAt(i).isDirty;
                    if (!dirty && !EditorApplication.isPlayingOrWillChangePlaymode) OpenStarterScene();
                }
                catch (Exception exception)
                {
                    File.WriteAllText("Library/WoochiTilemapSetup.txt", exception.ToString());
                    Debug.LogException(exception);
                }
            };
        }

        [MenuItem("Woochi/Tilemap/Create Starter Assets")]
        public static void CreateStarterAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before creating assets.");

            foreach (string folder in new[] { "Scenes", "Art", "Tiles", "Tiles/Palettes", "Materials" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var sprite = CreateSprite();
            var ground = CreateTile("Ground", sprite, new Color32(89, 145, 150, 255), true);
            var background = CreateTile("Background", sprite, new Color32(38, 49, 69, 255), false);
            var foreground = CreateTile("Foreground", sprite, new Color32(184, 135, 73, 255), false);
            var material = CreateMaterial();
            var previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                throw new InvalidOperationException("Save the current untitled scene before creating starter assets.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                CreatePalette(ground, background, foreground);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                {
                    var grid = new GameObject("Grid", typeof(Grid));
                    grid.GetComponent<Grid>().cellSize = Vector3.one;
                    var back = CreateLayer(grid.transform, "Background", -10, material);
                    var floor = CreateLayer(grid.transform, "Ground", 0, material);
                    CreateLayer(grid.transform, "Foreground", 10, material);

                    var body = floor.gameObject.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Static;
                    var composite = floor.gameObject.AddComponent<CompositeCollider2D>();
                    composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                    var collider = floor.gameObject.AddComponent<TilemapCollider2D>();
                    collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                    collider.extrusionFactor = 0.01f;

                    Paint(back, background, -16, -6, 32, 16);
                    Paint(floor, ground, -16, -7, 32, 2);
                    Paint(floor, ground, -16, -5, 1, 14);
                    Paint(floor, ground, 15, -5, 1, 14);
                    Paint(floor, ground, -11, -2, 6, 1);
                    Paint(floor, ground, -2, 1, 6, 1);
                    Paint(floor, ground, 7, 4, 6, 1);

                    var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                    cameraObject.tag = "MainCamera";
                    cameraObject.transform.position = new Vector3(0, 1, -10);
                    var camera = cameraObject.GetComponent<Camera>();
                    camera.orthographic = true;
                    camera.orthographicSize = 9;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color32(17, 22, 34, 255);
                    camera.nearClipPlane = 0.1f;
                    camera.farClipPlane = 100;
                    new GameObject("PlayerSpawn").transform.position = new Vector3(-12, -4, 0);

                    collider.ProcessTilemapChanges();
                    composite.GenerateGeometry();
                    Physics2D.SyncTransforms();
                    if (composite.pathCount == 0)
                        throw new InvalidOperationException("Ground collision geometry was not generated.");
                    var hit = Physics2D.Raycast(new Vector2(-12, -4), Vector2.down, 5);
                    if (hit.collider != composite || Mathf.Abs(hit.point.y + 5) > 0.05f)
                        throw new InvalidOperationException("Ground collision raycast failed.");
                    if (!EditorSceneManager.SaveScene(scene, ScenePath))
                        throw new IOException("Could not save the starter scene.");
                    SavePreview(camera);
                    Debug.Log("[Woochi] Ground raycast passed; composite paths: " + composite.pathCount);
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText("Library/WoochiTilemapSetup.txt", "SUCCESS\n" + ScenePath + "\n" + PalettePath);
                Debug.Log("[Woochi] Tilemap starter ready: " + ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Woochi/Tilemap/Open Starter Scene")]
        public static void OpenStarterScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) CreateStarterAssets();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
            {
                var ground = root.transform.Find("Ground");
                if (ground != null)
                {
                    Selection.activeGameObject = ground.gameObject;
                    GridPaintingState.scenePaintTarget = ground.gameObject;
                }
            }
            GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 1, 0), Quaternion.identity, 18, true);
            }
        }

        static Tilemap CreateLayer(Transform parent, string name, int order, Material material)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = material;
            return go.GetComponent<Tilemap>();
        }

        static void SavePreview(Camera camera)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var target = new RenderTexture(1280, 720, 24);
            var previousTarget = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes("Library/WoochiTilemapPreview.png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousTarget;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void Paint(Tilemap map, Tile tile, int x, int y, int width, int height)
        {
            var tiles = new TileBase[width * height];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
            map.SetTilesBlock(new BoundsInt(x, y, 0, width, height, 1), tiles);
        }

        static Sprite CreateSprite()
        {
            string path = Root + "/Art/PrototypeTile.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        byte shade = (byte)(y >= 29 ? 255 : (x == 0 || y == 0 || x == 31 ? 155 : 215));
                        texture.SetPixel(x, y, new Color32(shade, shade, shade, 255));
                    }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Tile CreateTile(string name, Sprite sprite, Color color, bool solid)
        {
            string path = Root + "/Tiles/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        static Material CreateMaterial()
        {
            string path = Root + "/Materials/TileUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D unlit sprite shader missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreatePalette(params Tile[] tiles)
        {
            if (File.Exists(PalettePath)) return;
            GridPaletteUtility.CreateNewPalette(Root + "/Tiles/Palettes", "WoochiStarter",
                GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Manual, Vector3.one,
                GridLayout.CellSwizzle.XYZ);
            var contents = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var map = contents.GetComponentInChildren<Tilemap>();
                for (int i = 0; i < tiles.Length; i++) map.SetTile(new Vector3Int(i, 0, 0), tiles[i]);
                PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }
}
