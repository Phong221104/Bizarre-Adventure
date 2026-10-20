using System.Collections;
using Pathfinding;
using UnityEngine;

public class ArcherSkeletonAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform shootPosition;
    [SerializeField] private GameObject arrowPrefab;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float nextWaypointDistance = 0.3f;

    [Header("Random Stop")]
    [SerializeField] private float minStopTime = 1f;
    [SerializeField] private float maxStopTime = 3f;
    [SerializeField] private float minMoveTime = 2f;
    [SerializeField] private float maxMoveTime = 5f;

    [Header("Shoot")]
    [SerializeField] private float shootCooldown = 5f;

    private bool isFacingRight = true;

    [SerializeField] private bool canShoot;
    [SerializeField] private bool isShooting;
    [SerializeField] private bool isStopping;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private int currentWaypoint;
    private bool reachedEndOfPath;

    private Coroutine movementRoutine;

    public enum States
    {
        Walk,
        Shoot
    }

    public States currentState;
    public States previousState;

    private void Awake()
    {
        canShoot = true;
    }

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0;

        currentState = States.Walk;
        previousState = States.Walk;

        animator.SetBool("Walk", true);

        InvokeRepeating(nameof(UpdatePath), 0f, 0.5f);

        movementRoutine = StartCoroutine(RandomMovement());
    }

    private void Update()
    {
        HandleFlip();

        if (currentState != previousState)
        {
            HandleStatesTransition(currentState, previousState);
            previousState = currentState;
        }

        if (isShooting)
            return;

        if (isStopping)
        {
            currentState = States.Walk;
            return;
        }

        if (canShoot)
        {
            currentState = States.Shoot;
        }
        else
        {
            currentState = States.Walk;
        }
    }

    private void FixedUpdate()
    {
        if (path == null)
            return;

        if (currentWaypoint >= path.vectorPath.Count)
        {
            reachedEndOfPath = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        reachedEndOfPath = false;

        float distance = Vector2.Distance(
            rb.position,
            path.vectorPath[currentWaypoint]
        );

        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
        }

        if (isShooting || isStopping)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (currentState == States.Walk)
        {
            Walk();
        }
        else if (currentState == States.Shoot && canShoot)
        {
            canShoot = false;
            StartCoroutine(Shoot());
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    #region Pathfinding

    private void UpdatePath()
    {
        if (target == null)
            return;

        if (seeker.IsDone())
        {
            seeker.StartPath(
                rb.position,
                target.position,
                OnPathComplete
            );
        }
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    #endregion

    #region Movement

    private void Walk()
    {
        if (path == null || currentWaypoint >= path.vectorPath.Count)
            return;

        Vector2 dir =
            ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;

        rb.linearVelocity = dir * moveSpeed;
    }

    private IEnumerator RandomMovement()
    {
        while (true)
        {
            float moveTime = Random.Range(minMoveTime, maxMoveTime);

            isStopping = false;

            yield return new WaitForSeconds(moveTime);

            float stopTime = Random.Range(minStopTime, maxStopTime);

            isStopping = true;
            rb.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(stopTime);
        }
    }

    #endregion

    #region Shoot

    private IEnumerator Shoot()
    {
        isShooting = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Shooting", true);

        yield return new WaitForSeconds(shootCooldown);

        canShoot = true;
    }

    private void SpawnArrow()
    {
        Instantiate(
            arrowPrefab,
            shootPosition.position,
            shootPosition.rotation
        );
    }

    private void CancelShootAnimation()
    {
        isShooting = false;

        animator.SetBool("Shooting", false);
        animator.SetBool("Walk", true);

        currentState = States.Walk;
    }

    #endregion

    #region State

    private void HandleStatesTransition(
        States currentState,
        States previousState)
    {
        if (currentState == States.Shoot &&
            previousState == States.Walk)
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Shooting", true);
        }

        if (currentState == States.Walk &&
            previousState == States.Shoot)
        {
            animator.SetBool("Shooting", false);
            animator.SetBool("Walk", true);
        }
    }

    #endregion

    #region Flip

    private void HandleFlip()
    {
        if (rb.linearVelocity.x < -0.01f && isFacingRight)
        {
            Flip();
        }
        else if (rb.linearVelocity.x > 0.01f && !isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {

    }

    #endregion
}
