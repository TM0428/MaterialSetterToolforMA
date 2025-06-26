using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace com.tm428.material_setter_tool_for_ma
{
    public class MaterialSetterCreatorWindow : EditorWindow
    {
        [System.Serializable]
        public class ColorVariation
        {
            public string name = "";
            public GameObject prefab;
            public Texture2D icon;
        }

        private GameObject avatarRoot;
        private GameObject targetObject;
        private string menuName = "Color";
        private Texture2D menuIcon;
        private List<ColorVariation> variations = new List<ColorVariation>();
        private Vector2 scrollPosition;
        private bool showHelp = true;

        [MenuItem("Tools/Modular Avatar/Material Setter Creator")]
        public static void ShowWindow()
        {
            MaterialSetterCreatorWindow window = GetWindow<MaterialSetterCreatorWindow>("Material Setter Creator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            if (variations.Count == 0)
            {
                variations.Add(new ColorVariation());
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Material Setter Creator for Modular Avatar", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ヘルプ表示切り替え
            showHelp = EditorGUILayout.Toggle("ヘルプを表示", showHelp);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 基本設定
            DrawBasicSettings();
            EditorGUILayout.Space();

            // メニュー設定
            DrawMenuSettings();
            EditorGUILayout.Space();

            // バリエーション設定
            DrawVariationSettings();
            EditorGUILayout.Space();

            // エラー検証
            DrawValidationErrors();
            EditorGUILayout.Space();

            // 実行ボタン
            DrawExecuteButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);
            
            avatarRoot = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("アバターのルートオブジェクト", 
                showHelp ? "アバターのルートオブジェクトを設定してください" : ""),
                avatarRoot, typeof(GameObject), true);

            targetObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("着せ替え対象のオブジェクト", 
                showHelp ? "実際に着ている服などの対象オブジェクトを設定してください" : ""),
                targetObject, typeof(GameObject), true);
        }

        private void DrawMenuSettings()
        {
            EditorGUILayout.LabelField("メニュー設定", EditorStyles.boldLabel);
            
            menuName = EditorGUILayout.TextField(
                new GUIContent("メニュー名", 
                showHelp ? "作成するメニューの名前（デフォルト: \"Color\"）" : ""),
                menuName);

            menuIcon = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("メニューアイコン", 
                showHelp ? "メニューのアイコン（オプション）" : ""),
                menuIcon, typeof(Texture2D), false);
        }

        private void DrawVariationSettings()
        {
            EditorGUILayout.LabelField("色バリエーション設定", EditorStyles.boldLabel);

            for (int i = 0; i < variations.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.LabelField($"バリエーション {i + 1}", EditorStyles.boldLabel);
                
                variations[i].name = EditorGUILayout.TextField(
                    new GUIContent("名前", 
                    showHelp ? "バリエーションの名前（例：「赤」「青」「緑」）" : ""),
                    variations[i].name);

                variations[i].prefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Prefab", 
                    showHelp ? "色違いのPrefab" : ""),
                    variations[i].prefab, typeof(GameObject), false);

                variations[i].icon = (Texture2D)EditorGUILayout.ObjectField(
                    new GUIContent("アイコン", 
                    showHelp ? "メニューアイテムのアイコン（オプション）" : ""),
                    variations[i].icon, typeof(Texture2D), false);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("このバリエーションを削除", GUILayout.Width(150)))
                {
                    if (variations.Count > 1)
                    {
                        variations.RemoveAt(i);
                        i--;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("バリエーション追加"))
            {
                variations.Add(new ColorVariation());
            }
        }

        private void DrawValidationErrors()
        {
            var errors = GetValidationErrors();
            if (errors.Count > 0)
            {
                EditorGUILayout.LabelField("エラー・警告", EditorStyles.boldLabel);
                foreach (string error in errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
        }

        private void DrawExecuteButton()
        {
            GUI.enabled = GetValidationErrors().Count == 0;
            
            if (GUILayout.Button("Material Setterメニューを作成", GUILayout.Height(30)))
            {
                CreateMaterialSetterMenu();
            }
            
            GUI.enabled = true;
        }

        private List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (avatarRoot == null)
                errors.Add("アバターのルートオブジェクトが設定されていません");
            else
            {
                // VRChat Avatar Descriptorの存在チェック
                var avatarDescriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
                if (avatarDescriptor == null)
                {
                    errors.Add("アバターのルートオブジェクトにVRCAvatarDescriptorが見つかりません");
                }
            }

            if (targetObject == null)
                errors.Add("着せ替え対象のオブジェクトが設定されていません");
            else if (avatarRoot != null)
            {
                // ターゲットオブジェクトがアバター配下にあるかチェック
                if (!IsChildOf(targetObject.transform, avatarRoot.transform))
                {
                    errors.Add("着せ替え対象のオブジェクトがアバターの配下にありません");
                }
            }

            if (string.IsNullOrEmpty(menuName))
                errors.Add("メニュー名が入力されていません");

            if (variations.Count == 0)
                errors.Add("最低1つのバリエーションが必要です");

            for (int i = 0; i < variations.Count; i++)
            {
                if (string.IsNullOrEmpty(variations[i].name))
                    errors.Add($"バリエーション {i + 1} の名前が入力されていません");

                if (variations[i].prefab == null)
                    errors.Add($"バリエーション {i + 1} のPrefabが設定されていません");
                else
                {
                    // Prefab内にRendererがあるかチェック
                    var renderers = variations[i].prefab.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0)
                    {
                        errors.Add($"バリエーション {i + 1} のPrefab内にRendererが見つかりません");
                    }
                }
            }

            // 同名バリエーションチェック
            var duplicateNames = variations
                .Where(v => !string.IsNullOrEmpty(v.name))
                .GroupBy(v => v.name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (string duplicateName in duplicateNames)
            {
                errors.Add($"バリエーション名 \"{duplicateName}\" が重複しています");
            }

            return errors;
        }

        private bool IsChildOf(Transform child, Transform parent)
        {
            Transform current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        private void CreateMaterialSetterMenu()
        {
            try
            {
                Undo.SetCurrentGroupName("Create Material Setter Menu");
                int undoGroup = Undo.GetCurrentGroup();

                // 既存の同名オブジェクトをチェック
                Transform existingObject = avatarRoot.transform.Find(menuName);
                if (existingObject != null)
                {
                    if (!EditorUtility.DisplayDialog("既存オブジェクト", 
                        $"「{menuName}」という名前のオブジェクトが既に存在します。上書きしますか？", 
                        "上書き", "キャンセル"))
                    {
                        return;
                    }
                    Undo.DestroyObjectImmediate(existingObject.gameObject);
                }

                // "Color" オブジェクトの作成
                GameObject colorObject = new GameObject(menuName);
                Undo.RegisterCreatedObjectUndo(colorObject, "Create Color Object");
                
                colorObject.transform.SetParent(avatarRoot.transform);
                colorObject.transform.localPosition = Vector3.zero;
                colorObject.transform.localRotation = Quaternion.identity;
                colorObject.transform.localScale = Vector3.one;

                // MA Menu Installer の追加
                var menuInstaller = colorObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMenuInstaller>();
                Undo.RegisterCreatedObjectUndo(menuInstaller, "Add Menu Installer");

                // MA Menu Item (SubMenu) の追加 - 子オブジェクトから生成
                var mainMenuItem = colorObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMenuItem>();
                Undo.RegisterCreatedObjectUndo(mainMenuItem, "Add Main Menu Item");
                
                // 新しいMA Menu ItemのAPI使用
                mainMenuItem.Control = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control
                {
                    name = menuName,
                    icon = menuIcon,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = null // 子オブジェクトから自動生成
                };

                // MA Menu Itemの追加プロパティ設定
                mainMenuItem.MenuSource = nadena.dev.modular_avatar.core.SubmenuSource.Children;
                mainMenuItem.label = menuName; // メニュー名を明示的に設定

                // 各バリエーション用のオブジェクト作成
                int createdObjects = 0;
                int materialSetterCount = 0;
                
                for (int i = 0; i < variations.Count; i++)
                {
                    if (CreateVariationObject(colorObject, variations[i], i))
                    {
                        createdObjects++;
                        materialSetterCount += CountMaterials(variations[i].prefab);
                    }
                }

                Debug.Log($"Material Setter メニューを作成しました:");
                Debug.Log($"- 作成されたオブジェクト数: {createdObjects + 1} (メインメニュー + バリエーション {createdObjects})");
                Debug.Log($"- Material Setter設定数: {materialSetterCount}");
                Debug.Log($"- パラメーター名: ColorSelect");
                Debug.Log($"- パラメーター値: 自動設定（階層順で1から順番に割り当て）");
                Debug.Log($"- メニュー構造: 子オブジェクトから自動生成");
                Debug.Log($"- 初期設定: 全てのバリエーションがOFF（パラメーター値=0）");
                
                // 選択状態にする
                Selection.activeGameObject = colorObject;
                EditorGUIUtility.PingObject(colorObject);
                
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Material Setter メニューの作成中にエラーが発生しました: {e.Message}");
                Debug.LogError($"スタックトレース: {e.StackTrace}");
            }
        }

        private bool CreateVariationObject(GameObject parent, ColorVariation variation, int index)
        {
            try
            {
                // バリエーション用オブジェクト作成
                GameObject variationObject = new GameObject(variation.name);
                Undo.RegisterCreatedObjectUndo(variationObject, $"Create Variation {variation.name}");
                
                variationObject.transform.SetParent(parent.transform);
                variationObject.transform.localPosition = Vector3.zero;
                variationObject.transform.localRotation = Quaternion.identity;
                variationObject.transform.localScale = Vector3.one;

                // MA Menu Item (Toggle) の追加
                var menuItem = variationObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMenuItem>();
                Undo.RegisterCreatedObjectUndo(menuItem, $"Add Menu Item for {variation.name}");
                
                // 新しいMA Menu ItemのAPI使用
                menuItem.Control = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control
                {
                    name = variation.name,
                    icon = variation.icon,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter
                    {
                        name = "ColorSelect"
                    }
                };

                // MA Menu Itemの追加プロパティ設定
                menuItem.isSynced = true;
                menuItem.isSaved = true;
                menuItem.isDefault = false; // 全てのアイテムをOFFに設定
                menuItem.automaticValue = true; // 自動でパラメーター値を設定

                // MA Material Setter の追加
                var materialSetter = variationObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMaterialSetter>();
                Undo.RegisterCreatedObjectUndo(materialSetter, $"Add Material Setter for {variation.name}");

                // Material Setter の設定
                int materialCount = SetupMaterialSetter(materialSetter, variation.prefab);
                
                Debug.Log($"バリエーション '{variation.name}' を作成:");
                Debug.Log($"  - パラメーター値: 自動設定");
                Debug.Log($"  - マテリアル設定数: {materialCount}");
                Debug.Log($"  - デフォルト: いいえ（初期状態はOFF）");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"バリエーション '{variation.name}' の作成中にエラー: {e.Message}");
                return false;
            }
        }

        private int SetupMaterialSetter(nadena.dev.modular_avatar.core.ModularAvatarMaterialSetter materialSetter, GameObject prefab)
        {
            if (prefab == null || targetObject == null) return 0;

            int materialCount = 0;
            
            // Prefab内のすべてのRendererを取得
            var prefabRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            
            // MaterialSetterのObjectsプロパティを直接使用
            var objects = new System.Collections.Generic.List<nadena.dev.modular_avatar.core.MaterialSwitchObject>();

            foreach (var prefabRenderer in prefabRenderers)
            {
                // 対応するターゲットのRendererを探す
                var targetRenderer = FindCorrespondingRenderer(prefabRenderer, prefab, targetObject);
                
                if (targetRenderer != null)
                {
                    // 各マテリアルスロットを設定
                    for (int i = 0; i < prefabRenderer.sharedMaterials.Length; i++)
                    {
                        if (prefabRenderer.sharedMaterials[i] != null && i < targetRenderer.sharedMaterials.Length)
                        {
                            // MaterialSwitchObjectを作成
                            var materialSwitchObject = new nadena.dev.modular_avatar.core.MaterialSwitchObject
                            {
                                Object = new nadena.dev.modular_avatar.core.AvatarObjectReference(),
                                Material = prefabRenderer.sharedMaterials[i],
                                MaterialIndex = i
                            };

                            // ターゲットオブジェクトを設定
                            materialSwitchObject.Object.Set(targetRenderer.gameObject);
                            
                            objects.Add(materialSwitchObject);
                            materialCount++;
                            
                            Debug.Log($"マテリアル設定追加: {targetRenderer.name}[{i}] -> {prefabRenderer.sharedMaterials[i].name}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"対応するレンダラーが見つかりません: {GetRelativePath(prefabRenderer.transform, prefab.transform)}");
                }
            }

            // MaterialSetterにオブジェクトリストを設定
            materialSetter.Objects = objects;

            if (materialCount > 0)
            {
                Debug.Log($"Material Setter自動設定完了: {materialCount} 個のマテリアル");
                // EditorUtilityを使用してオブジェクトを保存対象としてマーク
                EditorUtility.SetDirty(materialSetter);
            }
            
            return materialCount;
        }

        private int CountMaterials(GameObject prefab)
        {
            if (prefab == null) return 0;
            
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            return renderers.Sum(r => r.sharedMaterials.Where(m => m != null).Count());
        }

        private Renderer FindCorrespondingRenderer(Renderer prefabRenderer, GameObject prefab, GameObject target)
        {
            // 相対パスを取得
            string relativePath = GetRelativePath(prefabRenderer.transform, prefab.transform);
            
            // 相対パスでターゲット内を検索
            Transform targetTransform = target.transform.Find(relativePath);
            if (targetTransform != null)
            {
                var renderer = targetTransform.GetComponent<Renderer>();
                if (renderer != null) return renderer;
            }

            // 名前での検索（フォールバック）
            var targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            return targetRenderers.FirstOrDefault(r => r.name == prefabRenderer.name);
        }

        private string GetRelativePath(Transform child, Transform parent)
        {
            var path = new List<string>();
            Transform current = child;
            
            while (current != parent && current.parent != null)
            {
                path.Add(current.name);
                current = current.parent;
            }
            
            path.Reverse();
            return string.Join("/", path);
        }
    }
}
