using UnityEngine;

namespace com.tm428.material_setter_tool_for_ma
{
    /// <summary>
    /// 色バリエーションの設定を格納するクラス
    /// </summary>
    [System.Serializable]
    public class ColorVariation
    {
        [Tooltip("バリエーションの名前")]
        public string name = "";
        
        [Tooltip("色違いのPrefab")]
        public GameObject prefab;
        
        [Tooltip("メニューアイテムのアイコン")]
        public Texture2D icon;
        
        [Tooltip("プレビュー画像を自動で生成するかどうか")]
        public bool autoGeneratePreview = true;
    }
}
