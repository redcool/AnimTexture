using AnimTexture;
using PowerUtilities;
using System;
using UnityEngine;
using UnityEngine.AI;

public class SimpleAnimationControl : MonoBehaviour
{
    public TextureAnimation texAnim;
    [Header("Anim names")]
    public string idleName = "Idle";
    public string runName="Run", deathName="Death", attachName="Attack", behitName = "BeHit";

    [Header("Play Anim")]
    public string curName = "Idle";
    public string nextName;

    public bool isCrossFade = true;
    [Range(0,1)]
    public float crossFadeTime = .2f;

    public bool isUseAgent;
    [Header("Queue")]
    public bool isTestPlayQueue;

    NavMeshAgent agent;

    [EditorDisableGroup]
    public string lastName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        texAnim = GetComponent<TextureAnimation>();
        texAnim.Play(curName);

        agent = GetComponentInParent<NavMeshAgent>();
    }

    void Update()
    {
        if (!texAnim)
            return;

        if (isUseAgent && agent)
        {
            curName = agent.velocity.sqrMagnitude > 0.1f ? runName : idleName;
        }

        TryCrossFade();
        TryPlayQueue();

        if (texAnim.isPlayFinished)
        {
            texAnim.CrossFade(idleName, 0.2f);
        }
    }

    private void TryPlayQueue()
    {
        if (isTestPlayQueue)
        {
            isTestPlayQueue = false;
            texAnim.PlayQueue(curName, nextName);
        }
    }

    public void TryCrossFade()
    {

        if (lastName != curName)
        {
            if (isCrossFade)
                texAnim.CrossFade(lastName, curName, crossFadeTime);
            else
                texAnim.Play(curName);

            lastName = curName;
        }
    }
}
