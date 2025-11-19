#if UNITY_EDITOR
namespace AnimTexture
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using PowerUtilities;

    public class AnimTextureEditorInit
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            PlayerSettingTools.AddMacroDefines("ANIMTEXTURE");
        }
    }
}
#endif