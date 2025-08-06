using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameConfig;
using TEngine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public class ChapterModel
    {
        public GameObject Root { get; private set; }

        private Dictionary<string, SpriteRenderer> _renderers = new();

        public ChapterModel(GameObject root)
        {
            Root = root;

            Initialize().Forget();
        }

        public async UniTask Initialize()
        {
            var config = ConfigSystem.Instance.Tables.TbChapterConfig.DataList[0];

            foreach (var entity in config.EntityList_Ref)
            {
                var go = new GameObject(entity.Location, typeof(SpriteRenderer));
                go.transform.SetParent(Root.transform);
                go.transform.localPosition = Vector3.zero;

                var renderer = go.GetComponent<SpriteRenderer>();

                renderer.sortingOrder = entity.SortOrder;

                var sprite = await GameModule.Resource.LoadAssetAsync<Sprite>($"{entity.Location}_{entity.UpgradeList[0].Level}");
                renderer.sprite = sprite;

                _renderers.TryAdd(entity.Location, renderer);

                Log.Debug("" + entity.Location);
            }
        }

        public void OnRelease()
        {
            if (Root)
                Object.Destroy(Root);
        }
    }
}