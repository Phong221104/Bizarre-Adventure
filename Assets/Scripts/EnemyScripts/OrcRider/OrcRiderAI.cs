using System.Collections;
using Pathfinding;
using UnityEngine;

public class OrcRiderAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float walkSpeed;

    [SerializeField] private float attackDistance;

    [SerializeField] private float minWalkTime;
    [SerializeField] private float maxWalkTime;

    [SerializeField] private float minIdleTime;
    [SerializeField] private float maxIdleTime;

    [Header("Swing Attack")]
    [SerializeField] private float swingCooldown;
    [SerializeField] private int swingDamage;
    [SerializeField] private Collider2D swingHitbox;

    [Header("Ram Attack")]
    [SerializeField] private bool isRamMoving;
    private bool ramAttackStarted;
    [SerializeField] private float ramSpeed;
    [SerializeField] private float minRamTime;
    [SerializeField] private float maxRamTime;
    [SerializeField] private int ramDamage;
    [SerializeField] private Collider2D ramHitbox;

    [SerializeField] private float ramForwardRaycastDistance;
    [SerializeField] private float ramGroundRaycastDistance;
    [SerializeField] private float ramRaycastOriginOffset;

    [SerializeField] private LayerMask mapLayer;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private int currentWaypoint;
    private float nextWaypointDistance = 0.3f;

    private bool isFacingRight = true;

    private bool isActionRunning;
    private bool isRamming;
    private bool isSwingAttacking;

    private Vector2 ramTargetPosition;
    private Vector2 ramDirection;

    private float nextSwingAttackTime;

    private Coroutine currentActionCoroutine;

    public enum States
    {
        Walk,
        Idle,
        SwingAttack,
        RamAttack
    }

    [SerializeField] private States currentState;
    [SerializeField] private States previousState;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (swingHitbox != null)
            swingHitbox.enabled = false;

        if (ramHitbox != null)
            ramHitbox.enabled = false;
    }

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;

        InvokeRepeating(
            nameof(UpdatePath),
            0f,
            0.5f
        );

        currentState = States.Walk;
        previousState = currentState;
    }

    private void Update()
    {
        if (target == null)
            return;

        HandleFlip();

        if (currentState != previousState)
        {
            HandleStateTransition(
                currentState,
                previousState
            );

            previousState = currentState;
        }

        if (isActionRunning)
            return;

        DecideNextAction();
    }

    private void FixedUpdate()
    {
        if (currentState == States.RamAttack)
        {
            if (isRamMoving)
                Ram();
            else
                rb.linearVelocity = Vector2.zero;
            return;
        }

        if (path == null)
            return;

        if (currentState != States.Walk)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Walk();
    }

    private void DecideNextAction()
    {
        isActionRunning = true;

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackDistance &&
            Time.time >= nextSwingAttackTime)
        {
            currentState = States.SwingAttack;

            nextSwingAttackTime =
                Time.time + swingCooldown;

            StartSwingAttack();
            return;
        }

        float roll = Random.Range(0f, 100f);

        if (roll < 30f)
        {
            currentState = States.Walk;
            StartWalk();
        }
        else if (roll < 60f)
        {
            currentState = States.Idle;
            StartIdle();
        }
        else
        {
            if (ramAttackStarted)
            {
                currentState = States.Idle;
                StartIdle();
            }
            else
            {
                currentState = States.RamAttack;
                StartRamAttack();
            }
        }
    }

    private void StartWalk()
    {
        if (currentActionCoroutine != null)
            StopCoroutine(currentActionCoroutine);

        currentActionCoroutine = StartCoroutine(WalkRoutine());
    }

    private void StartIdle()
    {
        if (currentActionCoroutine != null)
            StopCoroutine(currentActionCoroutine);

        currentActionCoroutine = StartCoroutine(IdleRoutine());
    }

    private void StartSwingAttack()
    {
        if (currentActionCoroutine != null)
            StopCoroutine(currentActionCoroutine);

        currentActionCoroutine = StartCoroutine(SwingAttackRoutine());
    }

    private void StartRamAttack()
    {
        if (currentActionCoroutine != null)
            StopCoroutine(currentActionCoroutine);

        currentActionCoroutine = StartCoroutine(RamAttackRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        animator.SetBool("Walk", true);
        animator.SetBool("Idle", false);

        float duration = Random.Range(
            minWalkTime,
            maxWalkTime
        );

        yield return new WaitForSeconds(duration);

        animator.SetBool("Walk", false);

        isActionRunning = false;
    }

    private IEnumerator IdleRoutine()
    {
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Idle", true);

        float duration = Random.Range(
            minIdleTime,
            maxIdleTime
        );

        yield return new WaitForSeconds(duration);

        animator.SetBool("Idle", false);

        isActionRunning = false;
    }

    private IEnumerator SwingAttackRoutine()
    {
        isSwingAttacking = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("SwingAttack", true);

        yield return null;
    }

    public void EnableSwingHitbox()
    {
        if (swingHitbox == null)
            return;

        swingHitbox.enabled = true;

        DealDamage(
            swingHitbox,
            swingDamage
        );
    }

    public void DisableSwingHitbox()
    {
        if (swingHitbox == null)
            return;

        swingHitbox.enabled = false;
    }

    public void CancelSwingAttackAnimation()
    {
        DisableSwingHitbox();

        animator.SetBool("SwingAttack", false);

        isSwingAttacking = false;
        isActionRunning = false;

        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator RamAttackRoutine()
    {
        if (ramAttackStarted)
            yield break;

        ramAttackStarted = true;

        animator.SetBool("Walk", false);
        animator.SetBool("Idle", false);

        ramTargetPosition = target.position;

        ramDirection =
            (ramTargetPosition - (Vector2)transform.position)
            .normalized;

        isRamming = true;
        isRamMoving = false;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("RamStart", true);

        yield return null;
    }

    private IEnumerator RamDurationRoutine()
    {
        float duration = Random.Range(
            minRamTime,
            maxRamTime
        );

        yield return new WaitForSeconds(duration);

        if (!isRamming)
            yield break;

        EndRam();
    }

    public void CancelRamStartAnimation()
    {
        if (ramHitbox != null)
        {
            ramHitbox.enabled = true;

            DealDamage(
                ramHitbox,
                ramDamage
            );
        }

        animator.SetBool("RamStart", false);

        isRamMoving = true;

        StartCoroutine(RamDurationRoutine());
    }

    public void DisableRamHitbox()
    {
        if (ramHitbox == null)
            return;

        ramHitbox.enabled = false;
    }

    private void Ram()
    {
        if (!isRamming || !isRamMoving)
            return;

        Vector2 ramDirectionNormalized =
            ramDirection.normalized;

        Vector2 facingDirection = isFacingRight
            ? Vector2.right
            : Vector2.left;

        Vector2 frontOrigin =
            rb.position +
            facingDirection * ramRaycastOriginOffset;

        RaycastHit2D forwardHit = Physics2D.Raycast(
            frontOrigin,
            facingDirection,
            ramForwardRaycastDistance,
            mapLayer
        );

        RaycastHit2D groundHit = Physics2D.Raycast(
            frontOrigin,
            Vector2.down,
            ramGroundRaycastDistance,
            mapLayer
        );

        if (forwardHit.collider != null ||
            groundHit.collider != null)
        {
            EndRam();
            return;
        }

        rb.linearVelocity =
            ramDirectionNormalized * ramSpeed;
    }

    private void EndRam()
    {
        if (!isRamming)
            return;

        isRamming = false;
        isRamMoving = false;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("RamEnd", true);
    }

    public void CancelRamEndAnimation()
    {
        animator.SetBool("RamEnd", false);

        StartCoroutine(FinishRamEnd());
    }

    private IEnumerator FinishRamEnd()
    {
        yield return new WaitForSeconds(0.1f);

        isRamming = false;
        isRamMoving = false;
        ramAttackStarted = false;

        animator.SetBool("RamStart", false);
        animator.SetBool("RamEnd", false);
        Debug.Log("CALLEDDDDDDDDDDDDDDDDDDDDDDDDDD");
        animator.SetBool("Idle", true);

        yield return null;

        isActionRunning = false;
        rb.linearVelocity = Vector2.zero;
    }

    private void DealDamage(
        Collider2D hitbox,
        int damage)
    {
        if (target == null)
            return;

        Collider2D targetCollider =
            target.GetComponent<Collider2D>();

        if (targetCollider == null)
            return;

        if (!hitbox.IsTouching(targetCollider))
            return;

        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage);
    }

    private void Walk()
    {
        if (path == null)
            return;

        if (currentWaypoint >= path.vectorPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(
            rb.position,
            path.vectorPath[currentWaypoint]
        );

        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;

            if (currentWaypoint >= path.vectorPath.Count)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        Vector2 direction =
            ((Vector2)path.vectorPath[currentWaypoint]
            - rb.position).normalized;

        rb.linearVelocity =
            direction * walkSpeed;
    }

    #region A* AI

    private void UpdatePath()
    {
        if (target == null)
            return;

        if (!seeker.IsDone())
            return;

        seeker.StartPath(
            rb.position,
            target.position,
            OnPathComplete
        );
    }

    private void OnPathComplete(Path p)
    {
        if (p.error)
            return;

        path = p;
        currentWaypoint = 0;
    }

    #endregion

    private void HandleStateTransition(
        States current,
        States previous)
    {
    }

    #region Flip

    private void HandleFlip()
    {
        if (rb.linearVelocity.x < -0.01f &&
            isFacingRight)
        {
            Flip();
        }
        else if (rb.linearVelocity.x > 0.01f &&
                 !isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = transform.position;

        Vector2 direction = ramDirection;

        if (direction == Vector2.zero)
        {
            direction = isFacingRight
                ? Vector2.right
                : Vector2.left;
        }

        direction.Normalize();

        Vector2 frontOrigin =
            origin +
            direction * ramRaycastOriginOffset;

        Vector2 forwardEnd =
            frontOrigin +
            direction * ramForwardRaycastDistance;

        Vector2 groundEnd =
            frontOrigin +
            Vector2.down * ramGroundRaycastDistance;

        Gizmos.DrawWireSphere(
            origin,
            attackDistance
        );

        Gizmos.DrawLine(
            frontOrigin,
            forwardEnd
        );

        Gizmos.DrawLine(
            frontOrigin,
            groundEnd
        );

        if (swingHitbox != null)
        {
            Gizmos.DrawWireCube(
                swingHitbox.bounds.center,
                swingHitbox.bounds.size
            );
        }

        if (ramHitbox != null)
        {
            Gizmos.DrawWireCube(
                ramHitbox.bounds.center,
                ramHitbox.bounds.size
            );
        }
    }
}