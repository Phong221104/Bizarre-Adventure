using System.Collections;
using Pathfinding;
using UnityEngine;

public class EvilCrowAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform shootPosition;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float shootRange = 6f;
    float nextWaypointDistance = 0.3f;

    [Header("Shoot")]
    [SerializeField] float shootCooldown = 2f;

    bool canShoot = true;
    bool isFacingRight = true;

    Path path;
    Seeker seeker;
    Rigidbody2D rb;
    Animator animator;

    int currentWaypoint;
    bool reachedEndOfPath;

    public enum State
    {
        Fly,
        Shoot
    }

    [SerializeField] private State currentState;
    [SerializeField] private State previousState;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0;

        InvokeRepeating(nameof(UpdatePath), 0f, 0.5f);
    }

    private void Update()
    {
        HandleFlip();

        if (currentState != previousState)
        {
            HandleStateTransition(currentState, previousState);
            previousState = currentState;
        }

        if (currentState == State.Shoot)
            return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= shootRange && canShoot)
        {
            currentState = State.Shoot;
        }
        else
        {
            currentState = State.Fly;
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

        switch (currentState)
        {
            case State.Fly:
                Fly();
                break;

            case State.Shoot:
                rb.linearVelocity = Vector2.zero;

                if (canShoot)
                {
                    canShoot = false;
                    StartCoroutine(Shoot());
                }
                break;
        }

        float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
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

    #region AI

    void HandleStateTransition(State current, State previous)
    {
        if (current == State.Shoot && previous == State.Fly)
        {
            animator.SetBool("Fly", false);  
        }

        if (current == State.Fly && previous == State.Shoot)
        {
            animator.SetBool("Fly", true);
        }
    }

    void Fly()
    {
        Vector2 direction =
            ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;

        rb.linearVelocity = direction * moveSpeed;
    }

    IEnumerator Shoot()
    {
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Shoot", true);

        yield return new WaitForSeconds(shootCooldown);

        canShoot = true;
    }

    void SpawnBullet()
    {
            Instantiate(bulletPrefab, shootPosition.position, shootPosition.rotation);

            animator.SetBool("Shoot", false);

            currentState = State.Fly;
    }

    #endregion

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }

    #endregion
}