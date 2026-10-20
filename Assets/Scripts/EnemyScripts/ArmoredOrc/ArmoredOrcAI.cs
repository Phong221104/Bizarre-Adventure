using System.Collections;
using Pathfinding;
using UnityEngine;

public class ArmoredOrcAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform attackPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float nextWaypointDistance = 0.3f;

    [Header("Idle")]
    [SerializeField] private float idleMinTime = 1f;
    [SerializeField] private float idleMaxTime = 3f;

    [Header("Attack")]
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int damage = 25;
    [SerializeField] private LayerMask playerLayer;

    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private Path path;
    private int currentWaypoint;

    private Coroutine stateCoroutine;

    private bool isFacingRight = true;

    public enum States
    {
        Idle,
        Walk,
        Attack
    }

    public States currentState;
    public States previousState;

    private void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0;

        currentState = States.Idle;
        previousState = currentState;
    }

    private void Start()
    {
        stateCoroutine = StartCoroutine(Idle());

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
    }

    private void FixedUpdate()
    {
        if (currentState != States.Walk)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null || path == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Vector2.Distance(rb.position, target.position) <= attackRange)
        {
            ChangeState(States.Attack);
            return;
        }

        if (currentWaypoint >= path.vectorPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            ChangeState(States.Idle);
            return;
        }

        float distance = Vector2.Distance(
            rb.position,
            path.vectorPath[currentWaypoint]
        );

        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
        }

        if (currentWaypoint >= path.vectorPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction =
            ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;

        rb.linearVelocity = direction * moveSpeed;
    }

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

    private IEnumerator Idle()
    {
        rb.linearVelocity = Vector2.zero;

        float idleTime = Random.Range(idleMinTime, idleMaxTime);

        yield return new WaitForSeconds(idleTime);

        if (target == null)
        {
            ChangeState(States.Idle);
            yield break;
        }

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackRange)
        {
            ChangeState(States.Attack);
        }
        else
        {
            ChangeState(States.Walk);
        }
    }

    // ===================== ANIMATION EVENTS =====================

    // Called via Animation Event during the attack clip (the "hit frame")
    private void AttackHit()
    {
        if (attackPoint == null)
            return;

        Collider2D hit = Physics2D.OverlapCircle(
            attackPoint.position,
            attackRadius,
            playerLayer
        );

        if (hit == null)
            return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    // Called via Animation Event at the end of the attack clip.
    //
    // IMPORTANT: this always exits to Idle, even if the target is still in
    // attack range. Two reasons:
    //  1) Re-entering Attack directly (ChangeState(States.Attack) while
    //     currentState is already Attack) is a no-op in ChangeState, so the
    //     "Attack" bool never got re-enabled -> stuck animation.
    //  2) Forcing an Animator.Play() to restart the clip mid-state caused the
    //     glitch on consecutive attacks (animation stuck on the first frame).
    // Routing every attack end through Idle guarantees a real state change
    // (Attack -> Idle -> Attack), so the animator transitions normally, and
    // it also gives a natural cooldown between attacks (idleMinTime..idleMaxTime)
    // so the orc can no longer attack twice in a row.
    private void CancelAttackAnimation()
    {
        animator.SetBool("Attack", false);
        ChangeState(States.Idle);
    }

    // ===============================================================

    private void ChangeState(States newState)
    {
        if (currentState == newState)
            return;

        if (stateCoroutine != null)
        {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }

        currentState = newState;

        if (currentState == States.Idle)
        {
            stateCoroutine = StartCoroutine(Idle());
        }
    }

    private void HandleStatesTransition(States currentState, States previousState)
    {
        if (currentState == States.Idle && previousState == States.Walk)
        {
            animator.SetBool("Idle", true);
            animator.SetBool("Walk", false);
        }
        else if (currentState == States.Walk && previousState == States.Idle)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", true);
        }
        else if (currentState == States.Attack)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", false);
            animator.SetBool("Attack", true);
        }
        else if (currentState == States.Walk && previousState == States.Attack)
        {
            animator.SetBool("Walk", true);
        }
        else if (currentState == States.Idle && previousState == States.Attack)
        {
            animator.SetBool("Idle", true);
        }
    }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(
                attackPoint.position,
                attackRadius
            );
        }
    }
}