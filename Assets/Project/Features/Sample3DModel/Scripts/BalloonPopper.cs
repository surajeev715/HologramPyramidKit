using UnityEngine;

//Pops the shown balloon on a clap with the Pop animation from Blender and brings it back a moment later.
//ShapeReactionDispatcher calls React() every frame, so this needs no Update().
[RequireComponent(typeof(Animator))]
public class BalloonPopper : MonoBehaviour
{
    [Tooltip("Name of the looping Animator state the balloons show normally.")]
    public string idleState = "Idle";

    [Tooltip("Name of the Animator state that pops the balloon.")]
    public string popState = "Pop";

    [Tooltip("The animation played by the Pop state. The balloon comes back after it has finished.")]
    public AnimationClip popClip;

    [Tooltip("How long the popped balloon stays away after the pop animation before it comes back.")]
    public float respawnDelaySeconds = 2f;

    private Animator animator;
    private bool popped = false;
    private float popStartTime = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        //Start with a whole balloon whenever the figure is shown again.
        ShowIdle();
    }

    public void React(bool hasClap)
    {
        if (!popped)
        {
            if (hasClap)
            {
                Pop();
            }

            return;
        }

        float popSeconds = popClip != null ? popClip.length : 0f;

        if (Time.time - popStartTime >= popSeconds + respawnDelaySeconds)
        {
            ShowIdle();
        }
    }

    void Pop()
    {
        popped = true;
        popStartTime = Time.time;
        animator.Play(popState, 0, 0f);
    }

    void ShowIdle()
    {
        popped = false;
        animator.Play(idleState, 0, 0f);
    }
}
