using Pathfinding;
using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    private float nextWaypointDistance = 0.3f;

    [Header("Attack")]
//    [SerializeField] private int damage = 1;

    private bool isFacingRight = true;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private int currentWaypoint;
    private bool reachedEndOfPath;

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

        Chase();

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

    void Chase()
    {
        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    #endregion

    #region Combat

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (!collision.gameObject.CompareTag("Player"))
    //        return;

    //    collision.gameObject.GetComponent<PlayerHealth>().TakeDamage(damage);
    //}

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
}