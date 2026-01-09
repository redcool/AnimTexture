namespace AnimTexture
{
    using PowerUtilities;
    using System;
    using UnityEngine;

    [System.Serializable]
    public class AnimTextureClipInfo
    {
        public string clipName;
        public int clipNameHash;
        public int startFrame;
        public int endFrame;
        public bool isLoop;
        public float length;
        public float frameRate;

        [EditorButton(onClickCall = nameof(ConvertNameToHash))]
        public bool isConverTClipNameHash;

        public void ConvertNameToHash()
        {
            if (!string.IsNullOrEmpty(clipName))
                clipNameHash = Animator.StringToHash(clipName);
        }

        public AnimTextureClipInfo(string clipName,int startFrame,int endFrame)
        {
            this.clipName = clipName;
            this.startFrame = startFrame;
            this.endFrame = endFrame;

            if(!string.IsNullOrEmpty(clipName))
                clipNameHash = Animator.StringToHash(clipName);
        }
    }
}