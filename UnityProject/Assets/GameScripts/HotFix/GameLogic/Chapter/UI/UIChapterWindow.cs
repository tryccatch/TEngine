using UnityEngine;
using UnityEngine.UI;
using TEngine;
using System;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class UIChapterWindow : UIWindow
    {
        #region 脚本工具生成的代码
        private RawImage _rimgTarget;
        protected override void ScriptGenerator()
        {
            _rimgTarget = FindChildComponent<RawImage>("m_rimgTarget");
        }
        #endregion

        #region 事件
        #endregion

        protected override void RegisterEvent()
        {
            AddUIEvent<RenderTexture>(ChapterEventDefine.RenderTexture, OnRenderTexture);
        }

        protected override void OnCreate()
        {
            _rimgTarget.color = Color.clear;
            _rimgTarget.texture = ChapterSystem.Instance.ChapterRenderer.RenderTexture;
            _rimgTarget.color = Color.white;
        }

        private void OnRenderTexture(RenderTexture texture)
        {
            _rimgTarget.texture = texture;
            _rimgTarget.color = Color.white;
        }
    }
}
