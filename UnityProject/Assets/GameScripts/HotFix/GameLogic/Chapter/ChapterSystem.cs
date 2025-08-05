using TEngine;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public class ChapterSystem : Singleton<ChapterSystem>
    {
        private GameObject _root;
        private ObjectPool<ChapterRenderer> _renderers;
        private readonly Vector2Int _size = new(720, 1440);
        private float _axisZ;

        public ChapterRenderer ChapterRenderer { get; private set; }
        public ChapterModel ChapterModel { get; private set; }

        protected override void OnInit()
        {
            Utility.Unity.AddUpdateListener(Update);

            Initialize();

            base.OnInit();
        }

        private void Initialize()
        {
            _root = InitRoot();
            _renderers = InitRenderers();

            UpdateModel();
        }

        private GameObject InitRoot()
        {
            GameObject ret = new(nameof(ChapterSystem));
            Object.DontDestroyOnLoad(ret);
            ret.transform.position = 8 * Vector3.right;

            return ret;
        }

        private ObjectPool<ChapterRenderer> InitRenderers()
        {
            _axisZ = 0;

            if (_root == null)
                _root = InitRoot();

            return new ObjectPool<ChapterRenderer>(() =>
            {
                var r = new ChapterRenderer(_size.x, _size.y);

                r.Camera.gameObject.SetActive(false);

                r.Camera.transform.SetParent(_root.transform);

                _axisZ += r.Camera.nearClipPlane;
                r.Camera.transform.localPosition = _axisZ * Vector3.forward;
                _axisZ -= r.Camera.farClipPlane;

                return r;
            }, r =>
            {
                if (r.Camera) r.Camera.gameObject.SetActive(true);
            }, r =>
            {
                if (r.Camera) r.Camera.gameObject.SetActive(false);
            }, r =>
            {
                r.OnDispose();
            });
        }

        public void LoadView()
        {
            GameModule.UI.ShowUIAsync<UIChapterWindow>();
        }

        private void UpdateModel()
        {
            _renderers ??= InitRenderers();

            ChapterModel?.OnRelease();
            ChapterModel = new ChapterModel(GameModule.Resource.LoadGameObject(nameof(ChapterModel)));

            ChapterRenderer ??= _renderers.Get();
            ChapterRenderer.SetTarget(ChapterModel.Root);

            GameEvent.Send(ChapterEventDefine.RenderTexture, ChapterRenderer.RenderTexture);
        }

        private void Update()
        {

        }

        protected override void OnRelease()
        {
            if (_renderers != null && ChapterRenderer != null)
            {
                _renderers.Release(ChapterRenderer);
                _renderers.Dispose();
            }

            ChapterModel?.OnRelease();

            base.OnRelease();
        }
    }
}