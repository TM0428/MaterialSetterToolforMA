using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace com.tm428.material_setter_tool_for_ma
{
    /// <summary>
    /// Material Setter作成前のバリデーション機能
    /// </summary>
    public static class MaterialSetterValidator
    {
        /// <summary>
        /// 設定内容を検証してエラーリストを返す
        /// </summary>
        public static List<string> GetValidationErrors(GameObject avatarRoot, GameObject targetObject, 
            string menuName, List<ColorVariation> variations)
        {
            var errors = new List<string>();

            // アバターのルートオブジェクトチェック
            if (avatarRoot == null)
            {
                errors.Add("アバターのルートオブジェクトが設定されていません");
            }
            else
            {
                // VRChat Avatar Descriptorの存在チェック
                var avatarDescriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
                if (avatarDescriptor == null)
                {
                    errors.Add("アバターのルートオブジェクトにVRCAvatarDescriptorが見つかりません");
                }
            }

            // ターゲットオブジェクトチェック
            if (targetObject == null)
            {
                errors.Add("着せ替え対象のオブジェクトが設定されていません");
            }
            else if (avatarRoot != null)
            {
                // ターゲットオブジェクトがアバター配下にあるかチェック
                if (!IsChildOf(targetObject.transform, avatarRoot.transform))
                {
                    errors.Add("着せ替え対象のオブジェクトがアバターの配下にありません");
                }
            }

            // メニュー名チェック
            if (string.IsNullOrEmpty(menuName))
            {
                errors.Add("メニュー名が入力されていません");
            }

            // バリエーションチェック
            if (variations.Count == 0)
            {
                errors.Add("最低1つのバリエーションが必要です");
            }

            for (int i = 0; i < variations.Count; i++)
            {
                if (string.IsNullOrEmpty(variations[i].name))
                {
                    errors.Add($"バリエーション {i + 1} の名前が入力されていません");
                }

                if (variations[i].prefab == null)
                {
                    errors.Add($"バリエーション {i + 1} のPrefabが設定されていません");
                }
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

        /// <summary>
        /// 子オブジェクトかどうかを判定
        /// </summary>
        private static bool IsChildOf(Transform child, Transform parent)
        {
            Transform current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }
    }
}
