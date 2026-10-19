using System.Linq;
using UnityEngine;

//Shows the balloon in its idle animation and pops it with Pop(). After the pop it stays hidden for a moment and then reappears.
[RequireComponent(typeof(Animator))]
public class BalloonPopper : MonoBehaviour
{
    [Tooltip("Name of the looping Animator state the balloon shows normally.")]
    public string idleState = "Idle";

    [Tooltip("Name of the Animator state that pops the balloon.")]
    public string popState = "Pop";

    [Tooltip("How long the balloon stays hidden after the pop animation before it reappears.")]
    public float respawnDelaySeconds = 2f;

    private Animator animator;
    private Renderer[] renderers;
    private float popSeconds;
    private bool popped = false;
    private float timer = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<Renderer>(true);

        AnimationClip popClip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(clip => clip.name == popState);
        popSeconds = popClip != null ? popClip.length : 0f;
    }

    void OnEnable()
    {
        //Start with a whole balloon whenever the figure is shown again.
        ShowIdle();
    }

    public void Pop()
    {
        if (popped)
        {
            return;
        }

        popped = true;
        timer = 0f;
        animator.Play(popState, 0, 0f);
    }

    void Update()
    {
        if (!popped)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= popSeconds)
        {
            SetVisible(false);
        }

        if (timer >= popSeconds + respawnDelaySeconds)
        {
            ShowIdle();
        }
    }

    void ShowIdle()
    {
        popped = false;
        animator.Play(idleState, 0, 0f);
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}
