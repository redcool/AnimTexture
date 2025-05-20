using AnimTexture;
using UnityEngine;

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
    AnimationType lastAnimation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        texAnim = GetComponent<TextureAnimation>();

        texAnim.Play(nameof(AnimationType.Idle));
    }

    // Update is called once per frame
    void Update()
    {
        if (lastAnimation == currentAnimation)
            return;

        lastAnimation = currentAnimation;

        var animName = currentAnimation.ToString();
        texAnim.Play(animName);
    }
}
