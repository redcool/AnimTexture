namespace AnimTexture
{
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