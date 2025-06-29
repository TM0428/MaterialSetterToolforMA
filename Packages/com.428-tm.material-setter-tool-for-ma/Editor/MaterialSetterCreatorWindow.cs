using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace com.tm428.material_setter_tool_for_ma
{
    /// <summary>
    /// Material Setter Creator の EditorWindow
    /// UI の表示とユーザー操作の処理のみを担当
    /// </summary>
    public class MaterialSetterCreatorWindow : EditorWindow
    {
        private GameObject avatarRoot;
        private GameObject targetObject;
        private string menuName = "Color";
        private Texture2D menuIcon;
        private List<ColorVariation> variations = new List<ColorVariation>();
        private Vector2 scrollPosition;
        private bool showHelp = true;
        
        // プレビュー設定
        private float previewCameraDistance = 1.0f; // カメラ距離倍率

        // 各機能のコンポーネント
        private PreviewGenerator previewGenerator;
        private MaterialSetterCreator materialSetterCreator;

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
                variations.Add(new ColorVariation { autoGeneratePreview = true });
            }

            // プレビュー設定を復元
            previewCameraDistance = EditorPrefs.GetFloat("MaterialSetterCreator.PreviewCameraDistance", 1.0f);

            // 各機能のコンポーネントを初期化
            previewGenerator = new PreviewGenerator();
            materialSetterCreator = new MaterialSetterCreator();
        }

        private void OnDisable()
        {
            // プレビュー設定を保存
            EditorPrefs.SetFloat("MaterialSetterCreator.PreviewCameraDistance", previewCameraDistance);
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

            EditorGUILayout.Space();
            
            // プレビュー設定
            EditorGUILayout.LabelField("プレビュー設定", EditorStyles.boldLabel);
            
            float newCameraDistance = EditorGUILayout.Slider(
                new GUIContent("カメラ距離", 
                showHelp ? "プレビュー撮影時のカメラ距離倍率（0.0 = 近い、3.0 = 遠い）" : ""),
                previewCameraDistance, 0.0f, 3.0f);
                
            if (newCameraDistance != previewCameraDistance)
            {
                previewCameraDistance = newCameraDistance;
                EditorPrefs.SetFloat("MaterialSetterCreator.PreviewCameraDistance", previewCameraDistance);
            }
            
            EditorGUILayout.Space();
            
            // 一括プレビュー生成ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全てのプレビューを生成", GUILayout.Height(25)))
            {
                GenerateAllPreviews();
            }
            EditorGUILayout.EndHorizontal();
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

                // 自動プレビュー生成オプション
                variations[i].autoGeneratePreview = EditorGUILayout.Toggle(
                    new GUIContent("プレビュー自動生成", 
                    showHelp ? "このバリエーションのプレビュー画像を自動で生成します" : ""),
                    variations[i].autoGeneratePreview);

                // プレビュー生成ボタン
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("プレビュー生成", GUILayout.Width(120)))
                {
                    GeneratePreviewForVariation(i);
                }
                
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("このバリエーションを削除", GUILayout.Width(150)))
                {
                    if (variations.Count > 1)
                    {
                        variations.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        // 中身を空にする
                        variations[i] = new ColorVariation { autoGeneratePreview = true };
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("バリエーション追加"))
            {
                variations.Add(new ColorVariation { autoGeneratePreview = true });
            }
        }

        private void DrawValidationErrors()
        {
            var errors = MaterialSetterValidator.GetValidationErrors(avatarRoot, targetObject, menuName, variations);
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
            var errors = MaterialSetterValidator.GetValidationErrors(avatarRoot, targetObject, menuName, variations);
            GUI.enabled = errors.Count == 0;
            
            if (GUILayout.Button("Material Setterメニューを作成", GUILayout.Height(30)))
            {
                materialSetterCreator.CreateMaterialSetterMenu(avatarRoot, targetObject, menuName, menuIcon, variations, previewCameraDistance);
            }
            
            GUI.enabled = true;
        }

        /// <summary>
        /// 個別のバリエーションのプレビューを生成
        /// </summary>
        private void GeneratePreviewForVariation(int index)
        {
            if (variations[index].prefab != null && avatarRoot != null)
            {
                var preview = previewGenerator.GeneratePreview(variations[index].prefab, avatarRoot, previewCameraDistance);
                if (preview != null)
                {
                    variations[index].icon = preview;
                    EditorUtility.SetDirty(this);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "アバターのルートオブジェクトとPrefabの両方が設定されている必要があります。", "OK");
            }
        }

        /// <summary>
        /// 全てのバリエーションのプレビューを一括生成
        /// </summary>
        private void GenerateAllPreviews()
        {
            if (avatarRoot == null)
            {
                EditorUtility.DisplayDialog("エラー", "アバターのルートオブジェクトが設定されていません。", "OK");
                return;
            }

            int successCount = 0;
            int totalCount = variations.Count(v => v.prefab != null);

            if (totalCount == 0)
            {
                EditorUtility.DisplayDialog("情報", "プレビューを生成できるバリエーションがありません。\nPrefabが設定されているバリエーションが必要です。", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("プレビュー生成中", "準備中...", 0f);

            try
            {
                for (int i = 0; i < variations.Count; i++)
                {
                    if (variations[i].prefab != null)
                    {
                        EditorUtility.DisplayProgressBar("プレビュー生成中", 
                            $"バリエーション '{variations[i].name}' のプレビューを生成中...", 
                            (float)i / variations.Count);

                        try
                        {
                            var preview = previewGenerator.GeneratePreview(variations[i].prefab, avatarRoot, previewCameraDistance);
                            if (preview != null)
                            {
                                variations[i].icon = preview;
                                successCount++;
                                EditorUtility.SetDirty(this);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"バリエーション '{variations[i].name}' のプレビュー生成中にエラー: {e.Message}");
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("プレビュー生成完了", 
                $"{successCount} / {totalCount} 個のプレビューを生成しました。", "OK");
        }
    }
}
