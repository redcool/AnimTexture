﻿namespace AnimTexture
{
    using AnimTexture;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// read Animator control TextureAnimation .
    /// controller' StateName need equals AnimationClip's Name.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(TextureAnimation))]
    public class AnimatorControl : MonoBehaviour
    {
        [HideInInspector] public Animator animator;
        [HideInInspector] public TextureAnimation texAnim;
        [Tooltip("Animator's layer")]
        public int layerIndex = 0;
        [Tooltip("play 2 animation one time")]
        public bool isCrossFading = true;

        int lastTransitionNameHash;
        // Start is called before the first frame update
        void Start()
        {
            animator = GetComponent<Animator>();
            if (!animator)
            {
                enabled = false;
                return;
            }

            texAnim = GetComponent<TextureAnimation>();

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            var curIndex = texAnim.GetClipIndex(stateInfo.shortNameHash);
            texAnim.Play(curIndex);
        }

        // Update is called once per frame
        void Update()
        {
            var trans = animator.GetAnimatorTransitionInfo(layerIndex);
            if (trans.nameHash == 0)
                return;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            var curIndex = texAnim.GetClipIndex(stateInfo.shortNameHash);

            //Debug.Log("trans:" + trans.nameHash);
            //var nsi = animator.GetNextAnimatorStateInfo(layerIndex);
            //Debug.Log("state:" + nsi.shortNameHash);

            if (trans.nameHash != lastTransitionNameHash)
            {
                lastTransitionNameHash = trans.nameHash;

                var nextStateInfo = animator.GetNextAnimatorStateInfo(layerIndex);
                var nextIndex = texAnim.GetClipIndex(nextStateInfo.shortNameHash);
                if (isCrossFading)
                    texAnim.CrossFade(curIndex, nextIndex, trans.duration);
                else
                    texAnim.Play(nextIndex);
            }

        }
    }

}