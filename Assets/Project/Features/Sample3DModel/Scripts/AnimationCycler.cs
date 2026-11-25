using UnityEngine;

//Switches between the states of the Animator's base layer, either with Select() or automatically one after another.
[RequireComponent(typeof(Animator))]
public class AnimationCycler : MonoBehaviour, IShapeCycler
{
    [Tooltip("Names of the Animator states to cycle through, in order.")]
    public string[] states = new string[0];

    [Tooltip("Whether to switch to the next animation automatically after secondsPerState.")]
    public bool autoCycle = false;

    [Tooltip("How long each animation plays before switching to the next one when autoCycle is on.")]
    public float secondsPerState = 6f;

    [Tooltip("Blend time between two animations.")]
    public float crossFadeSeconds = 0.3f;

    private Animator animator;
    private int current = 0;
    private float timer = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        //Restart from the first animation whenever the figure is shown again.
        current = 0;
        timer = 0f;

        if (states.Length > 0)
        {
            animator.Play(states[0], 0, 0f);
        }
    }

    public int Count
    {
        get { return states.Length; }
    }

    public int CurrentIndex
    {
        get { return current; }
    }

    public string CurrentName
    {
        get { return states.Length > 0 ? states[current] : ""; }
    }

    public string NameAt(int index)
    {
        return index >= 0 && index < states.Length ? states[index] : "";
    }

    public void Select(int index)
    {
        if (index < 0 || index >= states.Length || index == current)
        {
            return;
        }

        timer = 0f;
        current = index;
        animator.CrossFadeInFixedTime(states[current], crossFadeSeconds, 0);
    }

    void Update()
    {
        if (!autoCycle)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= secondsPerState && states.Length > 1)
        {
            Select((current + 1) % states.Length);
        }
    }
}
