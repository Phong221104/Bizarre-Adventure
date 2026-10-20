using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

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
        SniperRifle
    }

    [SerializeField] private WeaponID weaponID;
    [SerializeField] private bool have;

    [SerializeField] private GameObject pickupPrefab;
    public GameObject PickupPrefab => pickupPrefab;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate;
    private float nextFireTime;

    [SerializeField] private Transform firePointRight;
    [SerializeField] private Transform firePointLeft;

    [SerializeField] private Vector2 mousePos;
    [SerializeField] Camera cam;
    [SerializeField] Rigidbody2D rb;
    SpriteRenderer weaponSprite;

    public WeaponID WeaponIDGet { get { return weaponID; } }
    public bool Have
    {
        get { return have; }
        set { have = value; }
    }
    public Vector2 MousePos { get { return mousePos; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = FindAnyObjectByType<Camera>();
        weaponSprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        RotateWeapon();

        if (Input.GetMouseButton(0))
            Shoot();
    }

    private void FixedUpdate()
    {
        Vector2 lookDir = (mousePos - rb.position);
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + 1f / fireRate;

        Transform firePoint = mousePos.x < transform.position.x
            ? firePointLeft
            : firePointRight;

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
    }

    private void ShootShotgun(Transform firePoint, int pelletCount)
    {
        for (int i = 0; i < pelletCount; i++)
        {
            float random = Random.Range(-17.5f, 17.5f);

            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, 0, random);

            BulletPoolManager.Instance.Spawn(
                bulletPrefab,
                firePoint.position,
                rot
            );
        }
    }

    private void RotateWeapon()
    {
        Vector2 dir = mousePos - rb.position;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        weaponSprite.flipY = mousePos.x < transform.position.x;
    }
}
