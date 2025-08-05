using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public class ChapterRenderer
    {
        public RenderTexture RenderTexture { get; private set; }
        public Camera Camera { get; private set; }

        public ChapterRenderer(int width, int height)
        {
            RenderTexture = new RenderTexture(width, height, 0);
            Camera = CreateCamera(RenderTexture);
        }

        private Camera CreateCamera(RenderTexture texture)
        {
            Camera camera = new GameObject(nameof(Camera), typeof(Camera)).GetComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = Math.Max(texture.width, texture.height) / 200f;
            camera.nearClipPlane = -camera.orthographicSize;
            camera.farClipPlane = camera.orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.targetTexture = texture;
            // camera.cullingMask = LayerMask.GetMask(nameof(ChapterRenderer));
            camera.cullingMask &= ~(1 << LayerMask.NameToLayer(nameof(UnityEngine.UI)));
            Object.DontDestroyOnLoad(camera);

            return camera;
        }

        public void SetTarget(GameObject target)
        {
            if (target)
            {
                target.transform.SetParent(Camera.transform);
                target.transform.localPosition = Vector3.zero;
            }
        }

        public void OnDispose()
        {
            if (RenderTexture)
                Object.Destroy(RenderTexture);

            if (Camera)
                Object.Destroy(Camera.gameObject);
        }
    }
}