using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System;

namespace com.tm428.material_setter_tool_for_ma
{
    /// <summary>
    /// プレビュー撮影用のカメラコントローラー
    /// avatar-pose-libraryのThumbnailGeneratorを参考に実装
    /// </summary>
    public class PreviewCameraController : IDisposable
    {
        private float avatarHeight = 1.5f;
        private GameObject avatarObject;
        private GameObject cameraObject;
        private Transform headTransform;
        private float distance;
        private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
        private Camera camera;
        private RenderTexture renderTexture;
        private readonly int previewLayer;
        private readonly int textureSize;
        private readonly float fieldOfView;
        private readonly float cameraDistanceMultiplier;
        private readonly Vector3 cameraOffset;

        public PreviewCameraController(GameObject avatar, int layer, int texSize, float fov, 
            float distanceMultiplier, Vector3 offset)
        {
            previewLayer = layer;
            textureSize = texSize;
            fieldOfView = fov;
            cameraDistanceMultiplier = distanceMultiplier;
            cameraOffset = offset;
            
            SetupCamera(avatar);
        }

        private void SetupCamera(GameObject avatar)
        {
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                Debug.LogWarning("VRCAvatarDescriptor が見つかりません。");
                return;
            }
            
            var animator = descriptor.GetComponent<Animator>();
            if (animator && animator.avatar.isHuman)
            {
                // HeadBoneの取得
                headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
            }

            // 身長の計算。多少余裕をもたせる。
            avatarHeight = descriptor.ViewPosition.y * 1.2f + 0.1f;

            // アバターのセッティング
            avatarObject = avatar;
            SetLayerRecursively(avatarObject, previewLayer);

            // カメラ生成
            cameraObject = new GameObject("PreviewCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear; // 背景を透過
            camera.cullingMask = 1 << previewLayer;
            camera.orthographic = false;
            camera.fieldOfView = fieldOfView;

            // カメラ位置調整
            Vector3 center = avatarObject.transform.position + new Vector3(0, avatarHeight / 2f, 0);
            distance = avatarHeight / (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            distance = Mathf.Max(distance * cameraDistanceMultiplier, avatarHeight * 0.6f);
            camera.transform.position = center + new Vector3(0f, 0f, distance) + cameraOffset * avatarHeight;
            camera.transform.LookAt(center);

            // カリング設定
            camera.nearClipPlane = distance * 0.01f;
            camera.farClipPlane = distance * 2.5f;

            // RenderTexture
            renderTexture = new RenderTexture(textureSize, textureSize, 24);
            camera.targetTexture = renderTexture;
            avatarObject.SetActive(false);
        }

        /// <summary>
        /// プレビュー画像をキャプチャ
        /// </summary>
        public Texture2D CapturePreview()
        {
            if (camera == null || renderTexture == null)
                return null;

            avatarObject.SetActive(true);

            // 頭にカメラを合わせる
            if (headTransform != null)
            {
                // カメラ位置調整
                var aPos = avatarObject.transform.position;
                var hPos = headTransform.position;
                Vector3 center = (aPos + hPos) * 0.5f;
                cameraObject.transform.position = center 
                                              + distance * Vector3.forward
                                              + cameraOffset * avatarHeight;
                cameraObject.transform.LookAt((center + hPos) * 0.5f);
            }
            
            camera.Render();

            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = currentActiveRT;

            avatarObject.SetActive(false);
            return tex;
        }

        /// <summary>
        /// 撮影のためにレイヤーを変更する
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            originalLayers.Add(obj, obj.layer);
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// レイヤーの初期化
        /// </summary>
        private void ResetLayers()
        {
            foreach (var layerPair in originalLayers)
            {
                if (layerPair.Key != null)
                    layerPair.Key.layer = layerPair.Value;
            }
            originalLayers.Clear();
        }

        public void Dispose()
        {
            if (originalLayers != null)
            {
                ResetLayers();
            }
            
            if (cameraObject != null)
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                cameraObject = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
            
            if (avatarObject != null)
            {
                avatarObject.SetActive(true);
            }
        }
    }
}
