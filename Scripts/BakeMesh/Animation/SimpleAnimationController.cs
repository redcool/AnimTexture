using AnimTexture;
using System;
using UnityEngine;
using UnityEngine.AI;

public class SimpleAnimationController : MonoBehaviour
{
    public TextureAnimation texAnim;
    public enum AnimationType
    {
        Idle,
        Run,
        GetHit,
        Death
    }

    public AnimationType currentAnimation;
    public float crossFadeTime = .2f;

    NavMeshAgent agent;
    string curAnimName,lastAnimName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        texAnim = GetComponent<TextureAnimation>();

        texAnim.Play(currentAnimation.ToString());

        agent = GetComponentInParent<NavMeshAgent>();
    }

    public void TestRunIdle()
    {
        var animName = agent.velocity.sqrMagnitude > 0.1f ? "Run" : "Idle";
        if (curAnimName != animName)
        {
            curAnimName = animName;
            lastAnimName = curAnimName == "Run" ? "Idle" : "Run";

            //texAnim.Play(animName);
            texAnim.CrossFade(lastAnimName, animName, crossFadeTime);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!agent)
            return;

        TestRunIdle();
    }

}
