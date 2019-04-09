using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float speed = 5f; //max speed of player
    public float acceleration = 5f; //acceleration of speed
    public float startSpeed = 1f; //minimum value of speed (from which it starts growing)
    public float thresholdSpeed = 2f; //speed value from which the animation starts - TAKE IT FROM THE ANIMTOR TRASNITION STATE!
    public float angularSpeed = 5f; //rotation speed of the character
    public float stopDistance = 3f; //distance to an obstacle to stop moving

    [Header("Sounds")]
    public WalkingSoundSettings[] walkingSounds;

    Transform thisTransform;
    Animator anim;
    float h, v;
    float currentSpeed;
    int collideMask;
    int currentSoundIndex;
    Vector3 rotationVector;
    Vector2 prevRot, currRot;
    Quaternion rot;
    Ray ray;
    RaycastHit hit;
    QueryTriggerInteraction queryTrigger;
    AudioSource source;
    static readonly string defaultTag = "Untagged";
    static readonly string gravelTag = "Gravel";

    // Use this for initialization
    void Awake()
    {
        anim = GetComponent<Animator>();
        thisTransform = transform;
        currentSpeed = startSpeed;
        rotationVector = Vector3.zero;
        currRot = Vector2.zero;
        prevRot = new Vector2(1f, 0f);
        collideMask = -1;
        queryTrigger = QueryTriggerInteraction.Ignore;
        source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        v = Input.GetAxis("Vertical");
        h = Input.GetAxis("Horizontal");

        DetectRotation();

        Movement();
    }

    private void FixedUpdate()
    {
        RotateTowards();
    }

	/// <summary>
	/// Checks if we can move in certain direction. Then checks if player is grounded to play walking sound.
	/// </summary>
    void Movement()
    {
        Acceleration();

        if (CheckAccess())
        {
            anim.SetFloat("Move", currentSpeed);

            if (currentSpeed > startSpeed)
            {
                CheckGround();

                if (!source.isPlaying)
                {
                    PlayWalkingSound();
                }
            }
        }
    }

	/// <summary>
	/// This method applies acceleration in connection with player input
	/// </summary>
    void Acceleration()
    {
        if (h != 0 || v != 0)
        {
            if (currentSpeed < speed)
            {
                currentSpeed += acceleration * Time.deltaTime;
            }
        }
        else
        {
            if (currentSpeed > thresholdSpeed)
                currentSpeed -= acceleration * Time.deltaTime;
            else
            {
                currentSpeed = startSpeed;
                StopWalkingSound();
            }
        }
    }

	/// <summary>
	/// This method applies rotation to player.
	/// </summary>
    void RotateTowards()
    {
        if (h != 0 || v != 0)
        {
            rotationVector.x = h;
            rotationVector.z = v;
            rot = Quaternion.LookRotation(rotationVector);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, rot, angularSpeed * Time.deltaTime);
    }

	
	/// <summary>
	/// This method applies rotation animation to player. Applied animation depends on player's input and current rotation of player.
	/// </summary>
    void DetectRotation()
    {
        currRot.x = h;
        currRot.y = v;

        if ((currRot == Vector2.zero) || (currentSpeed > thresholdSpeed))
        {
            return;
        }

        if (currentSpeed >= thresholdSpeed)
            prevRot = currRot;

        if (currRot != prevRot)
        {
            if (prevRot.x == 1)
            {
                if (currRot.y == 1)
                    anim.SetTrigger("TurnLeft");
                else if (currRot.y == -1)
                    anim.SetTrigger("TurnRight");
                else if (currRot.x == -1)
                    anim.SetTrigger("Turn");

            }
            else if (prevRot.x == -1)
            {
                if (currRot.y == 1)
                    anim.SetTrigger("TurnRight");
                else if (currRot.y == -1)
                    anim.SetTrigger("TurnLeft");
                else if (currRot.x == 1)
                    anim.SetTrigger("Turn");

            }
            else if (prevRot.y == 1)
            {
                if (currRot.x == 1)
                    anim.SetTrigger("TurnRight");
                else if (currRot.x == -1)
                    anim.SetTrigger("TurnLeft");
                else if (currRot.y == -1)
                    anim.SetTrigger("Turn");

            }
            else if (prevRot.y == -1)
            {
                if (currRot.x == 1)
                    anim.SetTrigger("TurnLeft");
                else if (currRot.x == -1)
                    anim.SetTrigger("TurnRight");
                else if (currRot.y == 1)
                    anim.SetTrigger("Turn");

            }

            prevRot = currRot;
        }
    }

    public void NullMovement()
    {
        currentSpeed = startSpeed;
        anim.SetFloat("Move", currentSpeed);
        StopWalkingSound();
    }

    bool CheckAccess()
    {
        SetRay(true);

        if (Physics.Raycast(ray, stopDistance, collideMask, queryTrigger))
        {
            NullMovement();

            return false;
        }
        else
        {
            return true;
        }
    }

    void CheckGround()
    {
        SetRay(false);

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag(defaultTag) && (currentSoundIndex != 0))
            {
                currentSoundIndex = 0;
                PlayWalkingSound();
            }
            else if (hit.collider.CompareTag(gravelTag) && (currentSoundIndex != 1))
            {
                currentSoundIndex = 1;
                PlayWalkingSound();
            }
        }
    }

    void SetRay(bool isForward)
    {
        ray.origin = thisTransform.position + new Vector3(0f, 0.5f, 0f);

        if (isForward)
            ray.direction = thisTransform.forward;
        else
            ray.direction = -thisTransform.up;
    }

	/// <summary>
	/// This is Apply Root Motion implementation
	/// </summary>
    private void OnAnimatorMove()
    {
        thisTransform.position += thisTransform.forward * anim.GetFloat("RunSpeed") * Time.deltaTime;
    }

    void PlayWalkingSound()
    {
        source.Stop();
        source.clip = walkingSounds[currentSoundIndex].clip;
        source.pitch = walkingSounds[currentSoundIndex].pitch;
        source.volume = walkingSounds[currentSoundIndex].volume;
        source.loop = true;
        source.Play();
    }

    void StopWalkingSound()
    {
        if(source.isPlaying)
            source.Stop();
    }

    
}

[System.Serializable]
public class WalkingSoundSettings
{
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume;
    [Range(-3f, 3f)]
    public float pitch;
}