using UnityEngine.InputSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PlayerMovement : MonoBehaviour
{
    // Global vars
    [Header("Velocity")]
    [SerializeField] float speed;
    [SerializeField] float smoothTime; // time will takes to reach the speed.

    [Header("Jump")]
    [SerializeField] float jumpForce;       // Jumping applied Force
    [SerializeField] float jumpSpeed;       // Jumping applied Force
    [SerializeField] bool canJump;       // Jumping applied Force
    
    // UI
    [Header("Acorn")]
    private float numAcorn;
    [SerializeField] private TextMeshProUGUI textAcornUI;

    [Header("Elevator")]
    [SerializeField] LayerMask elevatorLayer;

    [Header("Raycast")]
    // Raycast Ground check
    [SerializeField] Transform[] groundChecks;  //Raycast origin point (Vitamini feet)
    [SerializeField] LayerMask groundLayer;  //Ground Layer
    [SerializeField] float rayLength;       //Raycast Length
    [SerializeField] bool isGrounded;       //Ground touching flag
    private bool wasGrounded;               //isGrounded value of previous frame

    // Raycast Corner checks
    [SerializeField] Transform cornerLeftCheck;   //Raycast origin point (Vitamini feet)
    [SerializeField] Transform cornerRightCheck;  //Raycast origin point (Vitamini feet)
    [SerializeField] float rayCornerLength;     //Raycast Corner Length
    //[SerializeField] bool cornerDetected;       //Corner detection flag

    private enum CornerDetected
    {
        NoCeiling,
        Ceiling,
        CornerLeft,
        CornerRight,
    }
    [SerializeField] private CornerDetected cornerDetected = CornerDetected.NoCeiling;

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
    BoxCollider2D boxCollider2D;

    // Coyote Time vars
    [SerializeField] private float maxCoyoteTime;
    [SerializeField] private float coyoteTimer;
    [SerializeField] private bool coyoteTimerEnabled;

    // Flip Flag
    //private bool lastFlipState;

    #region Unity API
    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();   
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        boxCollider2D = GetComponent<BoxCollider2D>();  

        numAcorn = 0;
        textAcornUI.text = numAcorn.ToString();
    }
    private void Update()
    {        
        // Update the player's gravity when falling down
        ChangeGravity();
        // Launch the raycast to detect the ground
        RaycastGrounded();
        // Launch the raycast to detect the ceiling
        RaycastCeiling();
        // Controls if the player is on the elevator
        //Elevator();

        // Coyote Timer
        CoyoteTimerCheck();
        if (coyoteTimerEnabled)
            CoyoteTimerUpdate();

        // Jumping Animation
        AnimatingJumpìng();

        // Update the Input player velocity        
        //inputPlayerVelocity = new Vector2(horizontal * speed, 0);
        // Update the target Velocity
        //targetVelocity += rb2D.velocity + inputPlayerVelocity;
        
        // Last State vars Update
        wasGrounded = isGrounded;
        //lastFlipState = spriteRenderer.flipX;
    }
    void FixedUpdate()
    {        
        if (canJump)
            Jump();

        UpdateMovement();
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
        //isGrounded = Physics2D.Raycast(groundChecks[0].position, Vector2.down, rayLength, groundLayer);

        // Raycast Launching
        RaycastHit2D[] raycastsHit2D = new RaycastHit2D[groundChecks.Count()];
        isGrounded = false;
        for (int i = 0; i < groundChecks.Count(); i++)
        {
            raycastsHit2D[i] = Physics2D.Raycast(groundChecks[i].position, Vector2.down, rayLength, groundLayer);
            isGrounded |= raycastsHit2D[i];
        }        

        // Raycast Debugging
        foreach(Transform groundCheck in groundChecks)
            Debug.DrawRay(groundCheck.position,Vector2.down*rayLength,Color.red);        
    }
    void RaycastCeiling()
    {
        // Raycast Launching
        RaycastHit2D raycastCornerLeft;        
        RaycastHit2D raycastCornerRight;        

        //cornerDetected = false;        
        raycastCornerLeft = Physics2D.Raycast(cornerLeftCheck.position, Vector2.up, rayCornerLength, groundLayer);
        raycastCornerRight = Physics2D.Raycast(cornerRightCheck.position, Vector2.up, rayCornerLength, groundLayer);

        // Update the corner detection
        if (raycastCornerLeft && raycastCornerRight)       
            cornerDetected = CornerDetected.Ceiling;        
        else if (!raycastCornerLeft && raycastCornerRight)        
            cornerDetected = CornerDetected.CornerLeft;        
        else if (raycastCornerLeft && !raycastCornerRight)        
            cornerDetected = CornerDetected.CornerRight;        
        else        
            cornerDetected = CornerDetected.NoCeiling;
        
        // Raycast Debugging        
        Debug.DrawRay(cornerLeftCheck.position, Vector2.up * rayCornerLength, Color.green);
        Debug.DrawRay(cornerRightCheck.position, Vector2.up * rayCornerLength, Color.green);
    }
    #endregion

    #region Coyote Time
    private void CoyoteTimerUpdate()
    {
        // Reset Coyote Timer
        if (coyoteTimer >= maxCoyoteTime)
        {
            coyoteTimerEnabled = false;
            coyoteTimer = 0;
        }           
        // Coyote Timer update
        else        
            coyoteTimer += Time.fixedDeltaTime;
        
    }
    private void CoyoteTimerCheck()
    {
        // Coyote Timer will be disabled as long as the player is on the Ground
        if(isGrounded)
        {
            coyoteTimerEnabled = false;
            coyoteTimer = 0;
        }
        // Coyote Timer will be triggered when the player stop touching the ground
        else if (wasGrounded && !isGrounded)
            coyoteTimerEnabled = true;
    }
    #endregion

    #region Movement
    public void JumpActionInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed && (isGrounded || coyoteTimerEnabled))
        {
            canJump = true;
        }        
    }
    public void MoveActionInput(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>();
        //targetVelocity = new Vector2(direction.x * speed, rb2D.velocity.y);
        // Flip the player sprite & change the animations State
        FlipSprite(direction.x);       
        AnimatingRunning(direction.x);
    }
    // Player jump handling
    void Jump()
    {
        canJump = false;

        // Fix Player's position due to corner detection
        if (cornerDetected == CornerDetected.CornerLeft)
            transform.position -= new Vector3(0.5f, 0f);
        else if (cornerDetected == CornerDetected.CornerRight)
            transform.position += new Vector3(-0.5f, 0f);

        // Jumping force through Add Force
        //rb2D.AddForce(Vector2.up*jumpForce);

        // Jumping force through velocity
        float rb2DJumpVelY = Vector2.up.y * jumpSpeed;
        rb2D.velocity = new Vector2(rb2D.velocity.x,rb2DJumpVelY);
    }
    void UpdateMovement()
    {
        targetVelocity = new Vector2(direction.x * speed, rb2D.velocity.y);
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
    //void UpdateBoxCollider()
    //{
    //    if(spriteRenderer.flipX != lastFlipState)
    //    {
    //        if (spriteRenderer.flipX)
    //            boxCollider2D.offset = new Vector2(0.16f, boxCollider2D.offset.y);
    //        else
    //            boxCollider2D.offset = new Vector2(-0.16f, boxCollider2D.offset.y);
    //    }
    //}
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
