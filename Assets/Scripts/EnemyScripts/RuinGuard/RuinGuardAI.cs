using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class RuinGuardAI : MonoBehaviour
{
    public bool playerInside;

    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Punch")]
    [SerializeField] private Transform punchProjectileSpawnPoint;
    [SerializeField] private GameObject punchProjectilePrefab;

    [Header("Shoot")]
    [SerializeField] private Transform shootProjectileSpawnPoint;
    [SerializeField] private GameObject shootProjectilePrefab;

    [Header("Toxin")]
    [SerializeField] private Collider2D toxinArea;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [SerializeField] private float decisionDistance = 8f;

    [SerializeField] private float punchDistance = 4f;

    [SerializeField] private float nextWaypointDistance = 0.3f;

    [Header("Idle")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 2.5f;

    [Header("Walk Decision")]
    [Range(0f, 100f)]
    [SerializeField] private float walkChance = 50f;

    [Header("Attack Weight")]
    [Range(0f, 100f)]
    [SerializeField] private float shootChance = 70f;

    [Range(0f, 100f)]
    [SerializeField] private float punchChance = 30f;

    [Header("Punch Projectile")]
    [SerializeField] private float punchProjectileSpeed = 8f;

    [SerializeField] private float punchSpreadAngle = 45f;

    [Header("Toxin")]
    [Range(0f, 100f)]
    [SerializeField] private float toxinChance = 80f;

    [SerializeField] private float toxinCooldown = 12f;

    [SerializeField] private float toxinAttackWeight = 40f;

    [Header("Buff")]
    [Range(0f, 1f)]
    [SerializeField] private float buffHealthPercent = 0.5f;

    [SerializeField] private float buffMoveSpeedMultiplier = 1.25f;

    [Header("State")]
    [SerializeField] private States currentState;
    [SerializeField] private States previousState;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Animator animator;

    private int currentWaypoint;
    private bool reachedEndOfPath;

    private bool isAttacking;
    private bool isDead;
    private bool hasBuffed;

    private bool canToxin = true;
    private float toxinTimer;

    private float baseMoveSpeed;

    private bool isFacingRight = true;

    private Coroutine idleCoroutine;

    public enum States
    {
        Wake,
        Walk,
        Idle,
        Punch,
        Shoot,
        ToxinRelease,
        Buff,
        Death
    }

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        baseMoveSpeed = moveSpeed;

        currentState = States.Wake;
        previousState = States.Wake;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        rb.gravityScale = 0f;

        animator.SetBool("Wake", true);

        InvokeRepeating(
            nameof(UpdatePath),
            0f,
            0.5f
        );
    }

    private void Update()
    {
        if (isDead || target == null)
            return;

        HandleFlip();
        UpdateToxinCooldown();
        CheckBuff();

        if (currentState != previousState)
        {
            HandleStateTransition(
                currentState,
                previousState
            );

            previousState = currentState;
        }

        if (isAttacking)
            return;
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (currentState == States.Walk)
        {
            Walk();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    #region Pathfinding

    private void UpdatePath()
    {
        if (isDead || target == null)
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

    #region Movement

    private void StartWalk()
    {
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Idle", false);
        animator.SetBool("Walk", true);
    }

    private void Walk()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distanceToPlayer <= decisionDistance)
        {
            rb.linearVelocity = Vector2.zero;

            currentState = States.Idle;
            return;
        }

        if (path == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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

        if (currentWaypoint >= path.vectorPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction =
            (
                (Vector2)path.vectorPath[currentWaypoint]
                - rb.position
            ).normalized;

        rb.linearVelocity =
            direction * moveSpeed;
    }

    #endregion

    #region State Transition

    private void HandleStateTransition(
        States current,
        States previous)
    {
        switch (current)
        {
            case States.Wake:
                StartWake();
                break;

            case States.Walk:
                StartWalk();
                break;

            case States.Idle:
                StartIdle();
                break;

            case States.Punch:
                StartPunch();
                break;

            case States.Shoot:
                StartShoot();
                break;

            case States.ToxinRelease:
                StartToxinRelease();
                break;

            case States.Buff:
                StartBuff();
                break;

            case States.Death:
                StartDeath();
                break;
        }
    }

    #endregion

    #region Wake

    private void StartWake()
    {
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Idle", false);
        animator.SetBool("Wake", true);
    }

    public void CancelWakeAnimation()
    {
        animator.SetBool("Wake", false);

        currentState = States.Walk;
    }

    #endregion

    #region Idle

    private void StartIdle()
    {
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Idle", true);

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }

        idleCoroutine = StartCoroutine(
            IdleRoutine()
        );
    }

    private IEnumerator IdleRoutine()
    {
        float idleTime = Random.Range(
            minIdleTime,
            maxIdleTime
        );

        yield return new WaitForSeconds(idleTime);

        if (isDead || isAttacking)
            yield break;

        animator.SetBool("Idle", false);

        DecideAfterIdle();
    }

    private void DecideAfterIdle()
    {
        if (target == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance > decisionDistance)
        {
            float roll = Random.Range(
                0f,
                100f
            );

            if (roll < walkChance)
            {
                currentState = States.Walk;
                return;
            }
        }

        bool canPunch =
            distance <= punchDistance;

        ChooseAttack();
    }

    #endregion

    #region Attack Selection

    private void ChooseAttack()
    {
        if (canToxin && IsPlayerInsideToxinArea())
        {
            if (Random.value < 0.8f)
            {
                currentState = States.ToxinRelease;
                return;
            }
        }

        List<AttackOption> attackPool = new List<AttackOption>();

        attackPool.Add(
            new AttackOption(
                States.Shoot,
                shootChance
            )
        );

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance <= punchDistance)
        {
            attackPool.Add(
                new AttackOption(
                    States.Punch,
                    punchChance
                )
            );
        }

        currentState = SelectWeightedAttack(attackPool);
    }

    private States SelectWeightedAttack(List<AttackOption> attackPool)
    {
        float totalWeight = 0f;

        foreach (AttackOption option in attackPool)
        {
            totalWeight += option.weight;
        }

        float roll = Random.Range(0f, totalWeight);

        foreach (AttackOption option in attackPool)
        {
            if (roll < option.weight)
            {
                return option.state;
            }

            roll -= option.weight;
        }

        return attackPool[0].state;
    }

    private struct AttackOption
    {
        public States state;
        public float weight;

        public AttackOption(
            States state,
            float weight)
        {
            this.state = state;
            this.weight = weight;
        }
    }

    #endregion

    #region Punch

    private void StartPunch()
    {
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Punch", true);
    }

    public void SpawnPunchProjectiles()
    {
        if (punchProjectilePrefab == null)
            return;

        if (punchProjectileSpawnPoint == null)
            return;

        if (target == null)
            return;

        Vector2 baseDirection =
            (
                (Vector2)target.position
                - (Vector2)punchProjectileSpawnPoint.position
            ).normalized;

        float halfAngle =
            punchSpreadAngle * 0.5f;

        SpawnPunchProjectile(
            baseDirection,
            -halfAngle
        );

        SpawnPunchProjectile(
            baseDirection,
            0f
        );

        SpawnPunchProjectile(
            baseDirection,
            halfAngle
        );
    }

    private void SpawnPunchProjectile(Vector2 baseDirection, float angle)
    {
        Vector2 direction = RotateVector(baseDirection, angle);

        GameObject projectile = Instantiate(
            punchProjectilePrefab,
            punchProjectileSpawnPoint.position,
            Quaternion.identity
        );

        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity =
                direction * punchProjectileSpeed;
        }
    }

    private Vector2 RotateVector(
        Vector2 vector,
        float angle)
    {
        float radians =
            angle * Mathf.Deg2Rad;

        float cos =
            Mathf.Cos(radians);

        float sin =
            Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        ).normalized;
    }

    public void CancelPunchAnimation()
    {
        isAttacking = false;

        animator.SetBool(
            "Punch",
            false
        );

        currentState = States.Idle;
    }

    #endregion

    #region Shoot

    private void StartShoot()
    {
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Shoot", true);
    }

    public void SpawnShootProjectile()
    {
        if (shootProjectilePrefab == null)
            return;

        if (shootProjectileSpawnPoint == null)
            return;

        if (target == null)
            return;

        Vector2 direction =
            (
                (Vector2)target.position
                - (Vector2)shootProjectileSpawnPoint.position
            ).normalized;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Instantiate(
            shootProjectilePrefab,
            shootProjectileSpawnPoint.position,
            Quaternion.Euler(
                0f,
                0f,
                angle
            )
        );
    }

    public void CancelShootAnimation()
    {
        isAttacking = false;

        animator.SetBool(
            "Shoot",
            false
        );

        currentState = States.Idle;
    }

    #endregion

    #region Toxin

    private void StartToxinRelease()
    {
        isAttacking = true;

        canToxin = false;
        toxinTimer = toxinCooldown;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool(
            "ToxinRelease",
            true
        );
    }

    public void ReleaseToxin()
    {
        if (isDead)
            return;

        if (target == null)
            return;

        if (toxinArea == null)
            return;

        if (!toxinArea.OverlapPoint(target.position))
            return;

        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.ApplyToxinDebuff();
    }

    public void CancelToxinAnimation()
    {
        isAttacking = false;

        animator.SetBool(
            "ToxinRelease",
            false
        );

        currentState = States.Idle;
    }

    private void UpdateToxinCooldown()
    {
        if (canToxin)
            return;

        toxinTimer -= Time.deltaTime;

        if (toxinTimer <= 0f)
        {
            toxinTimer = 0f;
            canToxin = true;
        }
    }

    #endregion

    #region Toxin Collider

    private bool IsPlayerInsideToxinArea()
    {
        if (toxinArea == null)
            return false;

        if (target == null)
            return false;

        return playerInside = toxinArea.OverlapPoint(
            target.position
        );
    }

    #endregion

    #region Buff

    private void CheckBuff()
    {
        if (hasBuffed)
            return;

        RuinGuardHealth health =
            GetComponent<RuinGuardHealth>();

        if (health == null)
            return;

        if (health.CurrentHealth <=
            health.MaxHealth * buffHealthPercent)
        {
            hasBuffed = true;

            if (!isAttacking)
            {
                currentState = States.Buff;
            }
        }
    }

    private void StartBuff()
    {
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Idle", false);
        animator.SetBool("Buff", true);
    }

    public void ApplyBuff()
    {
        moveSpeed =
            baseMoveSpeed *
            buffMoveSpeedMultiplier;
    }

    public void CancelBuffAnimation()
    {
        isAttacking = false;

        animator.SetBool(
            "Buff",
            false
        );

        currentState = States.Idle;
    }

    #endregion

    #region Death

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;

        currentState = States.Death;
    }

    private void StartDeath()
    {
        isAttacking = false;

        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Walk", false);
        animator.SetBool("Idle", false);
        animator.SetBool("Punch", false);
        animator.SetBool("Shoot", false);
        animator.SetBool("ToxinRelease", false);
        animator.SetBool("Buff", false);

        animator.SetTrigger("Death");
    }

    #endregion

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

        Vector3 scale =
            transform.localScale;

        scale.x *= -1f;

        transform.localScale = scale;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            decisionDistance
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            punchDistance
        );
    }

    #endregion
}