using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace com.tm428.material_setter_tool_for_ma
{
    /// <summary>
    /// Material Setterメニューの実際の作成処理を担当するクラス
    /// </summary>
    public class MaterialSetterCreator
    {
        private readonly PreviewGenerator previewGenerator;

        public MaterialSetterCreator()
        {
            previewGenerator = new PreviewGenerator();
        }

        /// <summary>
        /// Material Setterメニューを作成
        /// </summary>
        public void CreateMaterialSetterMenu(GameObject avatarRoot, GameObject targetObject, 
            string menuName, Texture2D menuIcon, List<ColorVariation> variations)
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

                // メインオブジェクトの作成
                GameObject colorObject = new GameObject(menuName);
                Undo.RegisterCreatedObjectUndo(colorObject, "Create Color Object");
                
                colorObject.transform.SetParent(avatarRoot.transform);
                colorObject.transform.localPosition = Vector3.zero;
                colorObject.transform.localRotation = Quaternion.identity;
                colorObject.transform.localScale = Vector3.one;

                // MA Menu Installer の追加
                var menuInstaller = colorObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMenuInstaller>();
                Undo.RegisterCreatedObjectUndo(menuInstaller, "Add Menu Installer");

                // MA Menu Item (SubMenu) の追加
                var mainMenuItem = colorObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMenuItem>();
                Undo.RegisterCreatedObjectUndo(mainMenuItem, "Add Main Menu Item");
                
                // メニュー設定
                mainMenuItem.Control = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control
                {
                    name = menuName,
                    icon = menuIcon,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = null
                };

                mainMenuItem.MenuSource = nadena.dev.modular_avatar.core.SubmenuSource.Children;
                mainMenuItem.label = menuName;

                // 各バリエーション用のオブジェクト作成
                int createdObjects = 0;
                int materialSetterCount = 0;
                
                for (int i = 0; i < variations.Count; i++)
                {
                    if (CreateVariationObject(colorObject, targetObject, variations[i], i, avatarRoot))
                    {
                        createdObjects++;
                        materialSetterCount += CountMaterials(variations[i].prefab);
                    }
                }

                // 結果をログ出力
                LogCreationResults(createdObjects, materialSetterCount);
                
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

        /// <summary>
        /// 個別のバリエーションオブジェクトを作成
        /// </summary>
        private bool CreateVariationObject(GameObject parent, GameObject targetObject, ColorVariation variation, int index, GameObject avatarRoot)
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
                
                // メニューアイテム設定
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

                // 自動プレビュー生成
                if (variation.autoGeneratePreview && variation.icon == null && variation.prefab != null)
                {
                    try
                    {
                        var preview = previewGenerator.GeneratePreview(variation.prefab, avatarRoot);
                        if (preview != null)
                        {
                            variation.icon = preview;
                            menuItem.Control.icon = preview;
                            Debug.Log($"  - プレビュー画像を自動生成しました");
                        }
                    }
                    catch (System.Exception previewException)
                    {
                        Debug.LogWarning($"プレビュー自動生成中にエラーが発生しました: {previewException.Message}");
                    }
                }

                // MA Menu Itemの設定
                menuItem.isSynced = true;
                menuItem.isSaved = true;
                menuItem.isDefault = false;
                menuItem.automaticValue = true;

                // MA Material Setter の追加
                var materialSetter = variationObject.AddComponent<nadena.dev.modular_avatar.core.ModularAvatarMaterialSetter>();
                Undo.RegisterCreatedObjectUndo(materialSetter, $"Add Material Setter for {variation.name}");

                // Material Setter の設定
                int materialCount = SetupMaterialSetter(materialSetter, variation.prefab, targetObject);
                
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

        /// <summary>
        /// Material Setterの設定
        /// </summary>
        private int SetupMaterialSetter(nadena.dev.modular_avatar.core.ModularAvatarMaterialSetter materialSetter, GameObject prefab, GameObject targetObject)
        {
            if (prefab == null || targetObject == null) return 0;

            int materialCount = 0;
            var prefabRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            var objects = new System.Collections.Generic.List<nadena.dev.modular_avatar.core.MaterialSwitchObject>();

            foreach (var prefabRenderer in prefabRenderers)
            {
                var targetRenderer = FindCorrespondingRenderer(prefabRenderer, prefab, targetObject);
                
                if (targetRenderer != null)
                {
                    for (int i = 0; i < prefabRenderer.sharedMaterials.Length; i++)
                    {
                        if (prefabRenderer.sharedMaterials[i] != null && i < targetRenderer.sharedMaterials.Length)
                        {
                            var materialSwitchObject = new nadena.dev.modular_avatar.core.MaterialSwitchObject
                            {
                                Object = new nadena.dev.modular_avatar.core.AvatarObjectReference(),
                                Material = prefabRenderer.sharedMaterials[i],
                                MaterialIndex = i
                            };

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

            materialSetter.Objects = objects;

            if (materialCount > 0)
            {
                Debug.Log($"Material Setter自動設定完了: {materialCount} 個のマテリアル");
                EditorUtility.SetDirty(materialSetter);
            }
            
            return materialCount;
        }

        /// <summary>
        /// マテリアル数をカウント
        /// </summary>
        private int CountMaterials(GameObject prefab)
        {
            if (prefab == null) return 0;
            
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            return renderers.Sum(r => r.sharedMaterials.Where(m => m != null).Count());
        }

        /// <summary>
        /// 対応するRendererを検索
        /// </summary>
        private Renderer FindCorrespondingRenderer(Renderer prefabRenderer, GameObject prefab, GameObject target)
        {
            string relativePath = GetRelativePath(prefabRenderer.transform, prefab.transform);
            
            Transform targetTransform = target.transform.Find(relativePath);
            if (targetTransform != null)
            {
                var renderer = targetTransform.GetComponent<Renderer>();
                if (renderer != null) return renderer;
            }

            var targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            return targetRenderers.FirstOrDefault(r => r.name == prefabRenderer.name);
        }

        /// <summary>
        /// 相対パスを取得
        /// </summary>
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

        /// <summary>
        /// 作成結果をログ出力
        /// </summary>
        private void LogCreationResults(int createdObjects, int materialSetterCount)
        {
            Debug.Log($"Material Setter メニューを作成しました:");
            Debug.Log($"- 作成されたオブジェクト数: {createdObjects + 1} (メインメニュー + バリエーション {createdObjects})");
            Debug.Log($"- Material Setter設定数: {materialSetterCount}");
            Debug.Log($"- パラメーター名: ColorSelect");
            Debug.Log($"- パラメーター値: 自動設定（階層順で1から順番に割り当て）");
            Debug.Log($"- メニュー構造: 子オブジェクトから自動生成");
            Debug.Log($"- 初期設定: 全てのバリエーションがOFF（パラメーター値=0）");
        }
    }
}
