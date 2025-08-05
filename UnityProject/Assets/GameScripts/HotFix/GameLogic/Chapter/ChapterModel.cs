using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public class ChapterModel
    {
        public GameObject Root { get; private set; }

        public ChapterModel(GameObject root)
        {
            Root = root;
        }

        public void OnRelease()
        {
            if (Root)
                Object.Destroy(Root);
        }
    }
}