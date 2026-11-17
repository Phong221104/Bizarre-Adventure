using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 4f;

    private Vector2 move;
    public bool IsFacingUp { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer playerSprite;
    private Animator animator;
    private Camera cam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        cam = Camera.main;
    }

    private void Update()
    {
        MoveInput();
        AnimationHandle();
        Flip();
    }

    private void FixedUpdate()
    {
        MoveHandle();
    }

    private void MoveInput()
    {
        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");
    }

    private void MoveHandle()
    {
        rb.MovePosition(rb.position + move * speed * Time.fixedDeltaTime);
    }

    private void AnimationHandle()
    {
        animator.SetFloat("moveX", Mathf.Abs(move.x));
        animator.SetFloat("moveY", move.y);
        animator.SetFloat("AbsMoveY", Mathf.Abs(move.y));

        if (Mathf.Abs(move.x) < 0.01f)
        {
            if (move.y > 0.01f)
            {
                IsFacingUp = true;
            }
            else if (move.y < -0.01f)
            {
                IsFacingUp = false;
            }
        }
        else
        {
            IsFacingUp = false;
        }
    }

    private void Flip()
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        playerSprite.flipX = mousePos.x <= transform.position.x;
    }
}