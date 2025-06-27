using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.IO;
using System;

namespace com.tm428.material_setter_tool_for_ma
{
    /// <summary>
    /// Prefabからプレビュー画像を生成するクラス
    /// </summary>
    public class PreviewGenerator
    {
        private readonly int previewLayer = 31; // プレビュー専用レイヤー
        private readonly int previewSize = 128; // プレビューサイズ
        private readonly float cameraDistance = 1.5f; // カメラ距離倍率
        private readonly float fieldOfView = 30f; // カメラの視野角
        private readonly Vector3 cameraOffset = Vector3.zero; // カメラオフセット

        /// <summary>
        /// Prefabからプレビュー画像を生成します
        /// </summary>
        public Texture2D GeneratePreview(GameObject prefab, GameObject avatarRoot)
        {
            if (prefab == null || avatarRoot == null)
                return null;

            // 一時的にアバターとPrefabをインスタンス化
            GameObject tempAvatar = null;
            GameObject tempPrefab = null;
            PreviewCameraController cameraController = null;

            try
            {
                // VRCAvatarDescriptorの確認
                var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
                if (descriptor == null)
                {
                    Debug.LogError("VRCAvatarDescriptorが見つかりません。");
                    return null;
                }

                // アバターの複製を作成
                tempAvatar = UnityEngine.Object.Instantiate(avatarRoot);
                tempAvatar.name = "TempAvatar_Preview";
                tempAvatar.SetActive(false);

                // Prefabのインスタンスを作成
                tempPrefab = UnityEngine.Object.Instantiate(prefab);
                tempPrefab.name = "TempPrefab_Preview";

                // アバターの対応するオブジェクトにPrefabのマテリアルを適用
                if (!ApplyPrefabMaterials(tempPrefab, tempAvatar))
                {
                    Debug.LogWarning("Prefabのマテリアルをアバターに適用できませんでした。");
                }

                // プレビューカメラコントローラーを作成
                cameraController = new PreviewCameraController(tempAvatar, previewLayer, previewSize, 
                    fieldOfView, cameraDistance, cameraOffset);

                // プレビュー画像を生成
                var preview = cameraController.CapturePreview();

                // 生成したテクスチャをアセットとして保存
                if (preview != null)
                {
                    return SavePreviewAsAsset(preview, prefab.name);
                }

                return preview;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"プレビュー生成中にエラーが発生しました: {e.Message}");
                Debug.LogError($"スタックトレース: {e.StackTrace}");
                return null;
            }
            finally
            {
                // 後処理
                cameraController?.Dispose();
                
                if (tempAvatar != null)
                    UnityEngine.Object.DestroyImmediate(tempAvatar);
                if (tempPrefab != null)
                    UnityEngine.Object.DestroyImmediate(tempPrefab);
            }
        }

        /// <summary>
        /// Prefabのマテリアルをアバターのオブジェクトに適用
        /// </summary>
        private bool ApplyPrefabMaterials(GameObject prefab, GameObject avatar)
        {
            if (prefab == null || avatar == null)
                return false;

            var prefabRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            bool anyApplied = false;
            
            foreach (var prefabRenderer in prefabRenderers)
            {
                if (prefabRenderer == null || prefabRenderer.sharedMaterials == null)
                    continue;

                // 対応するアバターのRendererを探す
                var avatarRenderer = FindCorrespondingRenderer(prefabRenderer, prefab, avatar);
                
                if (avatarRenderer != null)
                {
                    // マテリアルを適用
                    var materials = avatarRenderer.sharedMaterials;
                    bool materialChanged = false;
                    
                    for (int i = 0; i < Mathf.Min(materials.Length, prefabRenderer.sharedMaterials.Length); i++)
                    {
                        if (prefabRenderer.sharedMaterials[i] != null)
                        {
                            materials[i] = prefabRenderer.sharedMaterials[i];
                            materialChanged = true;
                        }
                    }
                    
                    if (materialChanged)
                    {
                        avatarRenderer.sharedMaterials = materials;
                        anyApplied = true;
                    }
                }
            }

            return anyApplied;
        }

        /// <summary>
        /// 対応するRendererを検索
        /// </summary>
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
            return System.Linq.Enumerable.FirstOrDefault(targetRenderers, r => r.name == prefabRenderer.name);
        }

        /// <summary>
        /// 相対パスを取得
        /// </summary>
        private string GetRelativePath(Transform child, Transform parent)
        {
            var path = new System.Collections.Generic.List<string>();
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
        /// プレビューテクスチャをアセットとして保存
        /// </summary>
        private Texture2D SavePreviewAsAsset(Texture2D texture, string prefabName)
        {
            try
            {
                // プレビュー保存フォルダの作成
                string folderPath = "Assets/MaterialSetterPreviews";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "MaterialSetterPreviews");
                }

                // ファイル名の生成
                string fileName = $"Preview_{prefabName}.png";
                string filePath = Path.Combine(folderPath, fileName);
                
                // 既存ファイルがある場合は連番を追加
                int counter = 1;
                while (AssetDatabase.LoadAssetAtPath<Texture2D>(filePath) != null)
                {
                    fileName = $"Preview_{prefabName}_{counter}.png";
                    filePath = Path.Combine(folderPath, fileName);
                    counter++;
                }

                // PNG形式でエンコード
                byte[] pngData = texture.EncodeToPNG();
                
                // ファイルに保存
                File.WriteAllBytes(filePath, pngData);
                
                // アセットデータベースに反映
                AssetDatabase.Refresh();
                
                // 保存したアセットを読み込み
                var savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                
                if (savedTexture != null)
                {
                    Debug.Log($"プレビュー画像を保存しました: {filePath}");
                    return savedTexture;
                }
                else
                {
                    Debug.LogError($"保存したプレビュー画像の読み込みに失敗しました: {filePath}");
                    return texture;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"プレビュー画像の保存中にエラーが発生しました: {e.Message}");
                return texture;
            }
        }
    }
}
