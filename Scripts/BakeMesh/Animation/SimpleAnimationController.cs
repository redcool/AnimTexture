using AnimTexture;
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

    NavMeshAgent agent;
    string curAnimName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        texAnim = GetComponent<TextureAnimation>();
        texAnim.Play(nameof(AnimationType.Idle));

        agent = GetComponentInParent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!agent)
            return;

        var animName = agent.velocity.sqrMagnitude > 0.1f ? "Run" : "Idle";
        if (curAnimName != animName)
        {
            curAnimName = animName;
            texAnim.Play(animName);
        }

    }
}
