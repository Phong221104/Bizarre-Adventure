using System.Collections;
using Pathfinding;
using UnityEngine;

public class ThornSlimeAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 projectileSpawnPoint;
    [SerializeField] private GameObject thornProjectilePrefab;

    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpSpeed;

    [SerializeField] private float jumpDistance;
    [SerializeField] private float attackDistance;

    [SerializeField] private float minWalkTime;
    [SerializeField] private float maxWalkTime;

    [SerializeField] private float minIdleTime;
    [SerializeField] private float maxIdleTime;

    [Header("Jump")]
    [SerializeField] private float jumpDuration;

    [Header("Attack")]
    [SerializeField] private float projectileDelay;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private int currentWaypoint;
    private float nextWaypointDistance = 0.3f;

    private bool isFacingRight = true;

    private bool isActionRunning;
    private bool isJumping;
    private bool isAttacking;

    public enum States
    {
        Walk,
        Idle,
        Attack,
        Jump
    }

    [SerializeField] private States currentState;
    [SerializeField] private States previousState;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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
        if (path == null)
            return;

        if (currentState != States.Walk)
        {
            if (!isJumping)
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

        if (distance > jumpDistance)
        {
            currentState = States.Walk;

            StartWalk();
            return;
        }

        if (distance <= attackDistance)
        {
            DecideInAttackRange();
            return;
        }

        DecideInJumpRange();
    }

    private void DecideInJumpRange()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 50f)
        {
            currentState = States.Walk;
            StartWalk();
        }

        else if (roll < 80f)
        {
            currentState = States.Idle;
            StartIdle();
        }
        else
        {
            currentState = States.Jump;
            StartJump();
        }
    }

    private void DecideInAttackRange()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 40f)
        {
            currentState = States.Attack;
            StartAttack();
        }
        else if (roll < 70f)
        {
            currentState = States.Walk;
            StartWalk();
        }
        else
        {
            currentState = States.Idle;
            StartIdle();
        }
    }

    private void StartWalk()
    {
        StartCoroutine(WalkRoutine());
    }

    private void StartIdle()
    {
        StartCoroutine(IdleRoutine());
    }

    private void StartJump()
    {
        StartCoroutine(JumpRoutine());
    }

    private void StartAttack()
    {
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        float duration = Random.Range(
            minWalkTime,
            maxWalkTime
        );

        yield return new WaitForSeconds(duration);

        isActionRunning = false;
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

    private IEnumerator IdleRoutine()
    {
        rb.linearVelocity = Vector2.zero;

        float duration = Random.Range(
            minIdleTime,
            maxIdleTime
        );

        yield return new WaitForSeconds(duration);

        isActionRunning = false;
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Jump", true);

        while (isJumping)
        {
            if (path != null &&
                currentWaypoint < path.vectorPath.Count)
            {
                float distance = Vector2.Distance(
                    rb.position,
                    path.vectorPath[currentWaypoint]
                );

                if (distance < nextWaypointDistance)
                {
                    currentWaypoint++;
                }

                if (currentWaypoint < path.vectorPath.Count)
                {
                    Vector2 direction =
                        ((Vector2)path.vectorPath[currentWaypoint]
                        - rb.position).normalized;

                    rb.linearVelocity = direction * jumpSpeed;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public void CancelJumpAnimation()
    {
        animator.SetBool("Jump", false);

        isJumping = false;
        isActionRunning = false;

        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator AttackRoutine()
    {
        isActionRunning = true;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Attack", true);

        yield return null;
    }
    public void StartProjectileSequence()
    {
        StartCoroutine(ProjectileSequence());
    }

    private IEnumerator ProjectileSequence()
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnThornProjectile();

            if (i < 2)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        FinishAttack();
    }

    public void SpawnThornProjectile()
    {
        projectileSpawnPoint = target.position;
        if (thornProjectilePrefab == null ||
            projectileSpawnPoint == null)
            return;

        if (target == null)
            return;

        Instantiate(
            thornProjectilePrefab,
            target.position,
            Quaternion.identity
        );
    }

    public void FinishAttack()
    {
        isAttacking = false;
        isActionRunning = false;

        animator.SetBool("Attack", false);
    }

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

    private void HandleStateTransition(
        States current,
        States previous)
    {
        //switch (current)
        //{
        //    case States.Idle:
        //        animator.SetBool("Walk", false);
        //        break;

        //    case States.Walk:
        //        animator.SetBool("Walk", true);
        //        break;

        //    case States.Attack:
        //        animator.SetBool("Walk", false);
        //        break;

        //    case States.Jump:
        //        animator.SetBool("Walk", false);
        //        break;
        //}
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
}