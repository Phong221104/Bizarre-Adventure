
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum WeaponID
    {
        Pistol,
        Revolver,
        MiniSMG,
        SMG,
        MiniShotgun,
        Shotgun,
        Rank1AssaultRifle,
        Rank2AssaultRifle,
        Rank1MachineGun,
        Rank2MachineGun,
        SniperRifle,
        KnightBurden,
        BloodKatana,
        ThunderRage
    }

    [SerializeField] private WeaponID weaponID;
    [SerializeField] private bool have;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Shooting")]
    [SerializeField] private float fireRate;
    [SerializeField] private Transform firePointRight;
    [SerializeField] private Transform firePointLeft;
    [SerializeField] private Vector2 mousePos;

    [Header("Recoil")]
    [SerializeField] private float recoilDistance;
    [SerializeField] private float recoilKickSpeed;
    [SerializeField] private float recoilReturnSpeed;

    [Header("Melee")]
    [SerializeField] private Transform meleeSprite;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int meleeDamage;
    [SerializeField] private float attackRadius;

    [Header("Melee Animation")]
    [SerializeField] private float startAngle;
    [SerializeField] private float endAngle;
    [SerializeField] private float swingDuration;
    [SerializeField] private float projectileDelay;
    [SerializeField] private float endPauseDuration;
    [SerializeField] private float returnDelay;
    [SerializeField] private float returnDuration;

    [SerializeField] private Camera cam;
    [SerializeField] private Rigidbody2D rb;

    private PlayerMovement playerMovement;
    private SpriteRenderer weaponSprite;
    private SpriteRenderer swordSprite;

    private Vector3 defaultLocalPosition;
    private Vector3 recoilTargetPosition;

    private bool isRecoiling;
    private bool isMelee;
    private bool isSwinging;
    private bool reverseSwing;
    private bool lastFacingLeft;

    private float nextFireTime;
    private float lastSwingEndTime;
    private float currentMeleeAngle;

    private Coroutine swingCoroutine;

    private readonly HashSet<EnemyHealth> hitEnemies =
        new HashSet<EnemyHealth>();

    private readonly Collider2D[] meleeHits =
        new Collider2D[64];

    public GameObject PickupPrefab => pickupPrefab;
    public WeaponID WeaponIDGet => weaponID;
    public Vector2 MousePos => mousePos;

    public bool Have
    {
        get => have;
        set => have = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        cam = Camera.main;

        if (cam == null)
            cam = FindAnyObjectByType<Camera>();

        playerMovement =
            GetComponentInParent<PlayerMovement>();

        weaponSprite =
            GetComponent<SpriteRenderer>();

        defaultLocalPosition =
            transform.localPosition;

        isMelee =
            weaponID == WeaponID.KnightBurden ||
            weaponID == WeaponID.BloodKatana ||
            weaponID == WeaponID.ThunderRage;

        if (isMelee && meleeSprite != null)
        {
            swordSprite =
                meleeSprite.GetComponent<SpriteRenderer>();
        }

        currentMeleeAngle = startAngle;
    }

    private void OnEnable()
    {
        isSwinging = false;
        reverseSwing = false;
        isRecoiling = false;

        nextFireTime = 0f;
        lastSwingEndTime = Time.time;

        currentMeleeAngle = startAngle;

        transform.localPosition =
            defaultLocalPosition;

        if (isMelee && meleeSprite != null)
        {
            ApplyMeleeRotation(false);
        }
    }

    private void OnDisable()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        isSwinging = false;
        hitEnemies.Clear();

        transform.localPosition =
            defaultLocalPosition;
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;

            if (cam == null)
                return;
        }

        mousePos =
            cam.ScreenToWorldPoint(Input.mousePosition);

        AimWeapon();
        RotateWeapon();
        UpdateSortingOrder();

        if (isMelee)
            UpdateMelee();
        else
            UpdateRecoil();

        if (Input.GetMouseButton(0))
            Shoot();
    }

    private void AimWeapon()
    {
        Vector2 origin = transform.position;
        Vector2 direction = mousePos - origin;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private bool IsFacingLeft()
    {
        return mousePos.x < transform.position.x;
    }

    private void RotateWeapon()
    {
        bool facingLeft = IsFacingLeft();

        if (weaponSprite != null)
            weaponSprite.flipY = facingLeft;

        if (isMelee && swordSprite != null)
        {
            swordSprite.flipX = false;
            swordSprite.flipY = facingLeft;
        }
    }

    private void UpdateSortingOrder()
    {
        if (playerMovement == null)
            return;

        int order =
            playerMovement.IsFacingUp ? -1 : 1;

        if (weaponSprite != null)
            weaponSprite.sortingOrder = order;

        if (isMelee && swordSprite != null)
            swordSprite.sortingOrder = order;
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        if (fireRate <= 0f)
            return;

        if (isMelee)
        {
            if (isSwinging || meleeSprite == null)
                return;

            nextFireTime =
                Time.time + 1f / fireRate;

            swingCoroutine =
                StartCoroutine(Swing());

            return;
        }

        Transform firePoint =
            IsFacingLeft()
                ? firePointLeft
                : firePointRight;

        if (firePoint == null ||
            bulletPrefab == null ||
            BulletPoolManager.Instance == null)
            return;

        nextFireTime =
            Time.time + 1f / fireRate;

        switch (weaponID)
        {
            case WeaponID.MiniShotgun:
                ShootShotgun(firePoint, 4);
                break;

            case WeaponID.Shotgun:
                ShootShotgun(firePoint, 6);
                break;

            default:
                BulletPoolManager.Instance.Spawn(
                    bulletPrefab,
                    firePoint.position,
                    firePoint.rotation
                );
                break;
        }

        StartRecoil();
    }

    private IEnumerator Swing()
    {
        isSwinging = true;
        hitEnemies.Clear();

        bool facingLeft = IsFacingLeft();
        lastFacingLeft = facingLeft;

        float from =
            reverseSwing ? endAngle : startAngle;

        float to =
            reverseSwing ? startAngle : endAngle;

        reverseSwing = !reverseSwing;

        currentMeleeAngle = from;
        ApplyMeleeRotation(facingLeft);

        float elapsed = 0f;
        bool projectileSpawned = false;

        float duration =
            Mathf.Max(0.01f, swingDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / duration);

            currentMeleeAngle =
                Mathf.Lerp(from, to, t);

            ApplyMeleeRotation(facingLeft);
            CheckMeleeDamage();

            if (!projectileSpawned &&
                elapsed >= projectileDelay)
            {
                SpawnMeleeProjectile();
                projectileSpawned = true;
            }

            yield return null;
        }

        currentMeleeAngle = to;

        ApplyMeleeRotation(facingLeft);
        CheckMeleeDamage();

        if (!projectileSpawned)
            SpawnMeleeProjectile();

        if (endPauseDuration > 0f)
        {
            yield return new WaitForSeconds(
                endPauseDuration
            );
        }

        isSwinging = false;
        swingCoroutine = null;

        lastSwingEndTime = Time.time;
    }

    private void ApplyMeleeRotation(bool facingLeft)
    {
        if (meleeSprite == null)
            return;

        float angle =
            facingLeft
                ? -currentMeleeAngle
                : currentMeleeAngle;

        meleeSprite.localRotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateMelee()
    {
        if (meleeSprite == null)
            return;

        bool facingLeft = IsFacingLeft();

        if (isSwinging)
            return;

        if (facingLeft != lastFacingLeft)
            lastFacingLeft = facingLeft;

        if (!Input.GetMouseButton(0) &&
            Time.time - lastSwingEndTime >= returnDelay)
        {
            float speed =
                Mathf.Abs(endAngle - startAngle) /
                Mathf.Max(0.01f, returnDuration);

            currentMeleeAngle =
                Mathf.MoveTowards(
                    currentMeleeAngle,
                    startAngle,
                    speed * Time.deltaTime
                );

            if (Mathf.Abs(
                currentMeleeAngle - startAngle
            ) < 0.01f)
            {
                currentMeleeAngle = startAngle;
                reverseSwing = false;
            }
        }

        ApplyMeleeRotation(facingLeft);
    }

    private void CheckMeleeDamage()
    {
        if (attackPoint == null)
            return;

        int count = Physics2D.OverlapCircleNonAlloc(
            attackPoint.position,
            attackRadius,
            meleeHits,
            enemyLayer
        );

        for (int i = 0; i < count; i++)
        {
            Collider2D col = meleeHits[i];

            if (col == null)
                continue;

            EnemyHealth enemy =
                col.GetComponentInParent<EnemyHealth>();

            if (enemy == null)
                continue;

            if (!hitEnemies.Add(enemy))
                continue;

            enemy.TakeDamage(meleeDamage);
        }
    }

    private void SpawnMeleeProjectile()
    {
        if (firePoint == null ||
            bulletPrefab == null ||
            BulletPoolManager.Instance == null)
            return;

        BulletPoolManager.Instance.Spawn(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );
    }

    private void ShootShotgun(
        Transform firePoint,
        int pelletCount)
    {
        for (int i = 0; i < pelletCount; i++)
        {
            float random =
                Random.Range(-17.5f, 17.5f);

            Quaternion rotation =
                firePoint.rotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    random
                );

            BulletPoolManager.Instance.Spawn(
                bulletPrefab,
                firePoint.position,
                rotation
            );
        }
    }

    private void StartRecoil()
    {
        Vector2 origin =
            rb != null
                ? rb.position
                : (Vector2)transform.position;

        Vector2 shootDirection =
            (mousePos - origin).normalized;

        Vector2 recoilDirection =
            -shootDirection;

        recoilTargetPosition =
            defaultLocalPosition +
            transform.parent.InverseTransformDirection(
                recoilDirection
            ) * recoilDistance;

        isRecoiling = true;
    }

    private void UpdateRecoil()
    {
        if (isRecoiling)
        {
            transform.localPosition =
                Vector3.MoveTowards(
                    transform.localPosition,
                    recoilTargetPosition,
                    recoilKickSpeed * Time.deltaTime
                );

            if (Vector3.Distance(
                transform.localPosition,
                recoilTargetPosition
            ) < 0.001f)
            {
                transform.localPosition =
                    recoilTargetPosition;

                isRecoiling = false;
            }
        }
        else
        {
            transform.localPosition =
                Vector3.MoveTowards(
                    transform.localPosition,
                    defaultLocalPosition,
                    recoilReturnSpeed * Time.deltaTime
                );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!attackPoint)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            attackRadius
        );
    }
}
