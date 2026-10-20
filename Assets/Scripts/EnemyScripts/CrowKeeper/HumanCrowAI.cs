using System.Collections;
using Pathfinding;
using UnityEngine;

public class CrowKeeperAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform shootPosition;
    [SerializeField] private GameObject magicBulletPrefabs;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float teleRange = 6f;
    [SerializeField] private float teleportDistance = 3f;
    [SerializeField] private float nextWaypointDistance = 0.3f;

    [Header("Cooldown")]
    [SerializeField] private float teleportCooldown = 8f;
    [SerializeField] private float shootCooldown = 5f;

    private bool isFacingRight = true;

    [SerializeField] private bool canTeleport;
    [SerializeField] private bool canShoot;
    [SerializeField] private bool isTeleporting;
    [SerializeField] private bool isShooting;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private int currentWaypoint;
    private bool reachedEndOfPath;

    public enum States
    {
        Walk,
        Shoot,
        Teleport
    }

    public States currentState;
    public States previousState;

    private void Awake()
    {
        canShoot = true;
        Invoke("SetTele", 6f);
    }

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0;

        animator.SetBool("Walk", true);

        InvokeRepeating(nameof(UpdatePath), 0f, 0.5f);
    }

    private void Update()
    {
        HandleFlip();

        if (currentState != previousState)
        {
            HandleStatesTransition(currentState, previousState);
            previousState = currentState;
        }

        if (isTeleporting || isShooting)
            return;

        if (canShoot)
        {
            currentState = States.Shoot;
        }
        else if (canTeleport)
        {
            currentState = States.Teleport;
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

        float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
        }

        if (isTeleporting || isShooting)
            return;

        if (currentState == States.Walk)
        {
            Walk();
        }
        else if (currentState == States.Teleport && canTeleport)
        {
            canTeleport = false;
            StartCoroutine(Teleport());
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

    void UpdatePath()
    {
        if (target == null)
            return;

        if (seeker.IsDone())
        {
            seeker.StartPath(rb.position, target.position, OnPathComplete);
        }
    }

    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    #endregion

    void Walk()
    {
        Vector2 dir = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    IEnumerator Teleport()
    {
        isTeleporting = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Jump", true);

        currentState = States.Walk;

        yield return new WaitForSeconds(teleportCooldown);

        canTeleport = true;
    }

    IEnumerator Shoot()
    {
        isShooting = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Shooting", true);

        yield return new WaitForSeconds(shootCooldown);

        canShoot = true;
    }

    void SetTele()
    {
        canTeleport = true;
    }

    void CancelJumpAnimation()
    {
        Vector2 dir = (target.position - transform.position).normalized;
        float distance = Vector2.Distance(target.position, transform.position);

        Vector2 targetPos;

        if (distance > teleRange)
        {
            targetPos = (Vector2)transform.position + dir * teleportDistance;
        }
        else
        {
            targetPos = (Vector2)transform.position + dir * (distance * 0.7f);
        }

        transform.position = targetPos;

        animator.SetBool("Jump", false);
        animator.SetBool("Land", true);
    }

    void CancelLandAnimation()
    {
        isTeleporting = false;

        animator.SetBool("Land", false);
        animator.SetBool("Walk", true);
    }

    void CancelShootAnimation()
    {
        isShooting = false;

        animator.SetBool("Shooting", false);
        animator.SetBool("Walk", true);

        currentState = States.Walk;
    }

    void SpawnMagicBullet()
    {
        Instantiate(magicBulletPrefabs, shootPosition.position, shootPosition.rotation);
    }

    void HandleStatesTransition(States currentState, States previousState)
    {
        if (currentState == States.Teleport && previousState == States.Walk)
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Jump", true);
        }

        if (currentState == States.Shoot && previousState == States.Walk)
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Shooting", true);
        }
    }

    #region Flip

    void HandleFlip()
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

    void Flip()
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, teleRange);
    }

    #endregion
}