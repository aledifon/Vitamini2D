using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    // Global vars
    [Header("Velocity")]
    [SerializeField] float speed;
    [SerializeField] float smoothTime; // time will takes to reach the speed.

    [Header("Jump")]
    [SerializeField] float jumpForce;       // Jumping applied Force
    [SerializeField] bool jumpPressed;       // Jumping applied Force

    [Header("Raycast")]
    [SerializeField] Transform groundCheck;  //Raycast origin point (Vitamini feet)
    [SerializeField] LayerMask groundLayer;  //Ground Layer
    [SerializeField] float rayLength;       //Raycast Length
    [SerializeField] bool isGrounded;       //Ground touching flag

    // UI
    private float acornCounter;
    [SerializeField] private TextMeshProUGUI acornCounterTextUI;

    // Movement vars.
    float horizontal;
    Vector2 inputPlayerVelocity;    // Velocity given by the player's input
    Vector2 targetVelocity;          // Desired target player Speed(Velocity Movement type through rb2D)
    Vector2 dampVelocity;            // Player's current speed storage (Velocity Movement type through r

    // GO Components
    Rigidbody2D rb2D;
    SpriteRenderer spriteRenderer;
    Animator animator;

    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();   
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        acornCounter = 0;
        acornCounterTextUI.text = acornCounter.ToString();
    }
    private void Update()
    {
        InputPlayer();

        // Launch raycast
        RaycastGrounded();

        // Update the target Velocity
        targetVelocity = new Vector2(horizontal * speed, rb2D.velocity.y);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            jumpPressed = true;

        // Update the player's gravity when falling down
        ChangeGravity();
        // Flip the player sprite
        FlipSprite(horizontal);
        // Change the animations State
        Animating(horizontal);

        // Update the Input player velocity        
        //inputPlayerVelocity = new Vector2(horizontal * speed, 0);
        // Update the target Velocity
        //targetVelocity += rb2D.velocity + inputPlayerVelocity;         
    }
    void FixedUpdate()
    {
        UpdateMovement();
        if (jumpPressed)
            Jump();        
    }
    void InputPlayer()
    {
        horizontal = Input.GetAxis("Horizontal");
    }
    void RaycastGrounded()
    {
        // Raycast Launching
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down,rayLength, groundLayer);
        // Raycast Debugging
        Debug.DrawRay(groundCheck.position,Vector2.down*rayLength,Color.red);
    }
    // Player jump handling
    void Jump()
    {
        jumpPressed = false;
        rb2D.AddForce(Vector2.up*jumpForce);
    }
    private void ChangeGravity()
    {
        // Gravity will be heavier when the player is falling down
        if(rb2D.velocity.y<0)
            rb2D.gravityScale = 2.5f;
        else
            rb2D.gravityScale = 1;
    }
    void UpdateMovement()
    {                
        // Applies the correspondent new velocity        
        rb2D.velocity = Vector2.SmoothDamp(rb2D.velocity, targetVelocity, ref dampVelocity, smoothTime);

        // Applies the correspondent new velocity        
        //rb2D.velocity = Vector2.SmoothDamp(rb2D.velocity, targetVelocity, ref dampVelocity, smoothTime);
    }

    // Flip the Player sprite in function of its movement
    void FlipSprite(float horizontal)
    {
        if (horizontal < 0 && rb2D.velocity.x < 0)
            spriteRenderer.flipX = true;
        else if (horizontal > 0 && rb2D.velocity.x > 0)
            spriteRenderer.flipX = false;
    }
    private void Animating(float horizontal)
    {        
        animator.SetBool("IsRunning", horizontal != 0);

        animator.SetBool("IsJumping",!isGrounded);
    }

    // Collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Acorn"))
        {
            // Acorn dissappear
            Destroy(collision.collider.gameObject);
            // Increase Acorn counter
            acornCounter++;
            // Update Acorn counter UI Text
            acornCounterTextUI.text = acornCounter.ToString();
        }
    }
}
