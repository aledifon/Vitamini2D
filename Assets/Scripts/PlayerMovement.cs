using UnityEngine.InputSystem;
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
    
    // UI
    [Header("Acorn")]
    private float numAcorn;
    [SerializeField] private TextMeshProUGUI textAcornUI;

    [Header("Elevator")]
    [SerializeField] LayerMask elevatorLayer;

    [Header("Raycast")]
    [SerializeField] Transform groundCheck;  //Raycast origin point (Vitamini feet)
    [SerializeField] LayerMask groundLayer;  //Ground Layer
    [SerializeField] float rayLength;       //Raycast Length
    [SerializeField] bool isGrounded;       //Ground touching flag    
       
    // GO Components
    Rigidbody2D rb2D;
    // Movement vars.
    float horizontal;
    //Vector2 inputPlayerVelocity;    // Velocity given by the player's input
    Vector2 targetVelocity;          // Desired target player Speed(Velocity Movement type through rb2D)
    Vector2 dampVelocity;            // Player's current speed storage (Velocity Movement type through r
    Vector2 direction;              // To handle the direction with the New Input System

    // GO Components
    SpriteRenderer spriteRenderer;
    Animator animator;

    #region Unity API
    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();   
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        numAcorn = 0;
        textAcornUI.text = numAcorn.ToString();
    }
    private void Update()
    {               
        // Update the player's gravity when falling down
        ChangeGravity();
        // Launch the raycast to detect the ground
        RaycastGrounded();
        // Controls if the player is on the elevator
        //Elevator();

        // Jumping Animation
        AnimatingJumpìng();        

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
    // Collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Acorn"))
        {
            // Acorn dissappear
            Destroy(collision.collider.gameObject);
            // Increase Acorn counter
            numAcorn++;
            // Update Acorn counter UI Text
            textAcornUI.text = numAcorn.ToString();
        }
    }
    #endregion

    #region Raycast
    void RaycastGrounded()
    {
        // Raycast Launching
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down,rayLength, groundLayer);
        // Raycast Debugging
        Debug.DrawRay(groundCheck.position,Vector2.down*rayLength,Color.red);
    }
    #endregion

    #region Movement
    public void JumpActionInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed && isGrounded)
        {
            jumpPressed = true;
        }        
    }
    public void MoveActionInput(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>();        
        targetVelocity = new Vector2(direction.x * speed, rb2D.velocity.y);
        // Flip the player sprite & change the animations State
        FlipSprite(direction.x);       
        AnimatingRunning(direction.x);
    }
    // Player jump handling
    void Jump()
    {
        jumpPressed = false;
        rb2D.AddForce(Vector2.up*jumpForce);
    }    
    void UpdateMovement()
    {                
        // Applies the correspondent new velocity        
        rb2D.velocity = Vector2.SmoothDamp(rb2D.velocity, targetVelocity, ref dampVelocity, smoothTime);
    }
    private void ChangeGravity()
    {
        // Gravity will be heavier when the player is falling down
        if (rb2D.velocity.y < 0)
            rb2D.gravityScale = 2.5f;
        else
            rb2D.gravityScale = 1;
    }
    #endregion    

    #region Sprite & Animations
    // Flip the Player sprite in function of its movement
    void FlipSprite(float horizontal)
    {
        if (horizontal < 0 && rb2D.velocity.x < 0)
            spriteRenderer.flipX = true;
        else if (horizontal > 0 && rb2D.velocity.x > 0)
            spriteRenderer.flipX = false;
    }
    private void AnimatingRunning(float horizontal)
    {
        animator.SetBool("IsRunning", horizontal != 0);
    }
    private void AnimatingJumpìng()
    {
        animator.SetBool("IsJumping", !isGrounded);
    }
    #endregion    

    // Old Input System
    //void InputPlayer()
    //{
    //    horizontal = Input.GetAxis("Horizontal");
    //}    
}
