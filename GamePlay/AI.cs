using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AI : MonoBehaviour
{
    States state;
    Visibility visibilityController;
    bool canHit;
    int maxAllowedScore; //max score after which agent won't react on food and other players while invisible
    int startKillScore = 100; //start score from which AI start to kill
    int simulateGrowRate = 2; // after what number of collisions simulate grow
    int currentGrowRate = 0;

    NavPath path;
    [HideInInspector] public NavMeshAgent ai;
    Animator anim;
    Transform thisTransform;
    int index; //current position index to move
    int pathCount; //number of navPoints
    float updateTime = 0.9f;
    Vector3 destPoint;
    WaitForSeconds walkUpdate;
    static readonly int speedParam = Animator.StringToHash("Speed");

    Food food;
    int currentAim; //index of current food aim
    int arrayLength; //count of a food array
    public Transform[] foodPositions = new Transform[5]; //max number of trackable food

    GrowController controller; //grow controller of other Player
    GrowController thisGrowController; //current grow controller
    int numberOfAttempts = 25; //number of attempts to reach other Player
    Transform followPlayer; //the transform component of aim player
    GameObject followPlayerGO; //follow go to check follow state
    WaitForSeconds followUpdate; //update of aim position
	
	
    private void Start()
    {
        state = States.Null;
        visibilityController = GetComponentInChildren<Visibility>();
        thisTransform = transform;
        thisGrowController = GetComponent<GrowController>();
        path = WorldBounds.singleton.GetRandomPath();
        pathCount = path.positions.Count;
        index = Random.Range(0, pathCount);
        walkUpdate = new WaitForSeconds(updateTime);
        followUpdate = new WaitForSeconds(updateTime / 2f);
        ai = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        SetIdle();
    }

    private void FixedUpdate()
    {
        if (state == States.Search)
        {
            if (currentAim > 0 && foodPositions[currentAim - 1] == null)
            {
                SetNextAim();
            }
        }
        else if (state == States.Follow)
        {
            if (!followPlayerGO.activeSelf)
            {
                SetIdle();
            }
        }

    }

    public void SetMaxAllowedScore(int _maxScore)
    {
        maxAllowedScore = _maxScore;
    }

    #region States
    public void SetInitialState()
    {
        this.enabled = true;
    }

	/// <summary>
	/// This is a transition to idle state
	/// </summary>
    void SetIdle()
    {
        NullState();
        anim.SetFloat(speedParam, ai.speed);
        StartCoroutine(Idle());
        state = States.Idle;
    }

	/// <summary>
	/// This is how agent is acting in idle state (move toward random nav point on map).
	/// </summary>
    IEnumerator Idle()
    {
        float distance;
        index = Random.Range(0, pathCount);
        currentAim = 0;
        arrayLength = 0;

        while (true)
        {
            destPoint.x = path.positions[index].x;
            destPoint.y = thisTransform.position.y;
            destPoint.z = path.positions[index].y;
            index++;

            if (index >= pathCount)
                index = 0;

            ai.SetDestination(destPoint);

            yield return walkUpdate;
        }
    }

	/// <summary>
	/// This is a transition to search state.
	/// </summary>
    public void SetSearch()
    {
        NullState();
        arrayLength = 1;
        currentAim = 0;
        state = States.Search;
        SetNextAim();
    }

	/// <summary>
	/// Search state acting. Agent moves towards next food object if it exists in its buffer list. Else return to idle.
	/// </summary>
    public void SetNextAim()
    {
        if (currentAim >= arrayLength)
        {
            currentAim = 0;
            arrayLength = 0;
            SetIdle();
        }
        else
        {
            if (foodPositions[currentAim] != null)
            {
                ai.SetDestination(foodPositions[currentAim].position);
                currentAim++;
            }
            else
            {
                SetIdle();
            }
        }
    }

	/// <summary>
	/// This is a transition to Follow state.
	/// </summary>
    void SetFollow(Transform _tr, GameObject _go)
    {
        NullState();
        followPlayer = _tr;
        followPlayerGO = _go;
        state = States.Follow;
        StartCoroutine(Follow());
    }

	/// <summary>
	/// Follow state acting. Agent follows to destination agent with some delay. After some number of attempts if agent doesn' reach destination, return to idle state.
	/// </summary>
    IEnumerator Follow()
    {
        for (int i = 0; i < numberOfAttempts; i++)
        {
            if(followPlayer)
                ai.SetDestination(followPlayer.position);
            yield return followUpdate;
        }

        SetIdle();
    }

	/// <summary>
	/// Stops all agent operations under this Monobehaviour
	/// </summary>
    public void NullState()
    {
        state = States.Null;
        StopAllCoroutines();
    }

    #endregion /States

    #region Analisator
	/// <summary>
	/// This is agent's field of view. If next item appears in agent's fov then it decides to transition to other state
	/// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!this.enabled || (thisGrowController.score > maxAllowedScore && !visibilityController.isVisible))
            return;

        /* THIS IS SiMULATION OF GROW FOR AI TO SIMULATE DIFFICULTY*/
        if (!visibilityController.isVisible)
        {
            if (currentGrowRate >= simulateGrowRate)
            {
                thisGrowController.SimulateGrow();
                currentGrowRate = 0;
            }
            else
            {
                currentGrowRate++;
            }
        }

        food = other.GetComponent<Food>();
        controller = other.GetComponent<GrowController>();

        if (food && state != States.Follow && food.points == 0)
        {
            if (arrayLength < foodPositions.Length)
            {
                foodPositions[arrayLength] = other.transform;
                arrayLength++;

                if (state != States.Search)
                    SetSearch();
            }
        }

        if (controller && canHit && thisGrowController.score > startKillScore)
        {
            if (state != States.Follow)
            {
                if (controller.score < thisGrowController.score)
                {
                    SetFollow(controller.transform, controller.gameObject);
                }
            }
        }
    }
    #endregion /Analisator

    public void SetCanHit(bool _canHit)
    {
        canHit = _canHit;
    }

    public void UpdateIdleTime(float value)
    {
        updateTime -= value;
        walkUpdate = new WaitForSeconds(updateTime);
    }
}

enum States
{
    Null, Idle, Search, Follow
}
