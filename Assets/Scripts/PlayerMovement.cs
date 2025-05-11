using UnityEngine.InputSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    // Global vars
    [Header("Velocity")]
    [SerializeField] float speed;
    [SerializeField] float smoothTime; // time will takes to reach the speed.
    [SerializeField] float lerpSpeed;  // Interpolation Time

    [Header("Jump")]
    [SerializeField] float jumpForce;       // Jumping applied Force
    [SerializeField] float jumpVertSpeed;       // Jumping applied Force
    [SerializeField] private float jumpHorizSpeed;       // Jumping applied Force
    [SerializeField] bool jumpTriggered;    // Jumping applied Force

    [SerializeField] float minJumpDist;     // Min. Jumping Dist. Uds.
    [SerializeField] float maxJumpDist;     // Min. Jumping Dist. Uds.    
    [SerializeField] float jumpingTimer;    // Jumping Timer
    private float minJumpingTime;           // Min & Max Jumping Times (in func. of Jumping distance. & Jump. speed)                                            
    private float maxJumpingTime;
    private bool jumpPressed;    

    [SerializeField] private float maxJumpHorizDist;  // Max allowed Horizontal Jumping distance
    //[SerializeField] private float maxJumpHorizTimer; // Horizontal Jumping Timer                                                      
    [SerializeField] private float maxJumpHorizTime;         // Max Jumping time on horizontal Movement
                                                             // (Calculated in func. of maxJumpDistance & Player's speed)        

    // Coyote Time vars
    [Header("Coyote Time")]
    [SerializeField] private float maxCoyoteTime;
    [SerializeField] private float coyoteTimer;
    [SerializeField] private bool coyoteTimerEnabled;

    // Raycast Corner checks
    [Header("Corner Detection")]
    [SerializeField] Transform cornerLeftCheck;   //Raycast origin point (Vitamini feet)
    [SerializeField] Transform cornerRightCheck;  //Raycast origin point (Vitamini feet)
    [SerializeField] float rayCornerLength;     //Raycast Corner Length
                                                //[SerializeField] bool cornerDetected;       //Corner detection flag

    // Input Buffer
    [Header("Input Buffer")]
    [SerializeField] private float jumpBufferTime;
    [SerializeField] private float jumpBufferTimer;
    
    // UI        
    [Header("Acorn")]
    [SerializeField] private TextMeshProUGUI textAcornUI;
    private float numAcorn;
    private float NumAcorn
    { get { return numAcorn; } 
      set { numAcorn = Mathf.Clamp(value,0,99); } 
    }

    [Header("Elevator")]
    [SerializeField] LayerMask elevatorLayer;

    [Header("Raycast")]
    // Raycast Ground check
    [SerializeField] Transform[] groundChecks;  //Raycast origin point (Vitamini feet)
    [SerializeField] LayerMask groundLayer;  //Ground Layer
    [SerializeField] float rayLength;       //Raycast Length
    [SerializeField] bool isGrounded;       //Ground touching flag
    public bool IsGrounded => isGrounded;
    private bool wasGrounded;               //isGrounded value of previous frame

    #region Enums    
    private enum CornerDetected
    {
        NoCeiling,
        Ceiling,
        CornerLeft,
        CornerRight,
    }    
    // Define Character States
    public enum PlayerState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        Swinging,
        Hurting
    }
    [Header("Enums")]
    [SerializeField] private CornerDetected cornerDetected = CornerDetected.NoCeiling;
    [SerializeField] private PlayerState currentState = PlayerState.Idle;
    #endregion

    // GO Components
    Rigidbody2D rb2D;
    // Movement vars.
    float horizontal;
    //Vector2 inputPlayerVelocity;    // Velocity given by the player's input
    Vector2 targetVelocity;          // Desired target player Speed(Velocity Movement type through rb2D)
    Vector2 dampVelocity;            // Player's current speed storage (Velocity Movement type through r
    Vector2 direction;              // To handle the direction with the New Input System

    float rb2DDirVelX;    
    float rb2DJumpVelY;

    // GO Components
    SpriteRenderer spriteRenderer;
    Animator animator;
    BoxCollider2D boxCollider2D;
    AudioSource audioSource;

    // Audio Clips
    [Header("Audio Clips")]
    [SerializeField] AudioClip jumpAudioFx;
    [SerializeField] AudioClip acornAudioFx;

    // Flip Flag
    //private bool lastFlipState;

    private bool isDead;
    public bool IsDead { get => isDead; set => isDead = value; }

    #region Unity API

    private void OnDrawGizmos()
    {
        if (Camera.current == null || Camera.current.name != "Main Camera") return;

        // Ground Raycasts Debugging
        Gizmos.color = Color.red;
        Gizmos.DrawRay(groundChecks[0].position, Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[0].position + (Vector3.right * 0.01f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[0].position + (Vector3.right * 0.02f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[0].position + (Vector3.left * 0.01f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[0].position + (Vector3.left * 0.02f), Vector2.down * rayLength);
        //Gizmos.color = Color.blue;
        Gizmos.DrawRay(groundChecks[1].position, Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[1].position + (Vector3.right * 0.01f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[1].position + (Vector3.right * 0.02f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[1].position + (Vector3.left * 0.01f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[1].position + (Vector3.left * 0.02f), Vector2.down * rayLength);
        //Gizmos.color = Color.green;
        Gizmos.DrawRay(groundChecks[2].position, Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[2].position + (Vector3.right * 0.01f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[2].position + (Vector3.right * 0.02f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[2].position + (Vector3.left * 0.01f), Vector2.down * rayLength);
        Gizmos.DrawRay(groundChecks[2].position + (Vector3.left * 0.02f), Vector2.down * rayLength);

        // Ceiling Raycast Debugging
        Gizmos.color = Color.green;
        Gizmos.DrawRay(cornerLeftCheck.position, Vector2.up * rayCornerLength);
        Gizmos.DrawRay(cornerLeftCheck.position + (Vector3.right * 0.01f), Vector2.up * rayCornerLength);
        Gizmos.DrawRay(cornerLeftCheck.position + (Vector3.left * 0.01f), Vector2.up * rayCornerLength);
        Gizmos.DrawRay(cornerRightCheck.position, Vector2.up * rayCornerLength);
        Gizmos.DrawRay(cornerRightCheck.position + (Vector3.right * 0.01f), Vector2.up * rayCornerLength);
        Gizmos.DrawRay(cornerRightCheck.position + (Vector3.left * 0.01f), Vector2.up * rayCornerLength);
    }

    void Awake()
    {
        // ONLY FOR RECORDING
        // Establecer cámara lenta (por ejemplo, a la mitad de velocidad)
        //Time.timeScale = 0.3f;
        // ONLY FOR RECORDING

        rb2D = GetComponent<Rigidbody2D>();   
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        boxCollider2D = GetComponent<BoxCollider2D>();  
        audioSource = GetComponent<AudioSource>();

        NumAcorn = 0;
        textAcornUI.text = NumAcorn.ToString();        
    }
    private void Update()
    {
        // Check Jump Input Buffer
        CheckJumpInputBuffer();

        // Update the player state
        UpdatePlayerState();        

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
        if (jumpTriggered)
            JumpTrigger();

        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            jumpingTimer += Time.fixedDeltaTime;
            CalculateJumpSpeedMovement();            
        }

        UpdateMovement();
    }
    // Collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ant"))
        {
            AttackEnemy(collision.gameObject);
        }
        else if (collision.collider.CompareTag("Acorn"))
        {
            // Acorn dissappear
            Destroy(collision.collider.gameObject);
            // Increase Acorn counter
            NumAcorn++;
            // Update Acorn counter UI Text
            textAcornUI.text = NumAcorn.ToString();

            // Play Acorn Fx
            audioSource.PlayOneShot(acornAudioFx);

            // Condition to pass to the next Scene
            if (NumAcorn == 3)
                LoadScene();
        }
    }
    #endregion

    #region Player State
    // Player State
    private void UpdatePlayerState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                if (isGrounded && direction.x != 0)
                    currentState = PlayerState.Running;
                //else if (!isGrounded && rb2D.velocity.y > 0)
                else if (jumpTriggered)
                    currentState = PlayerState.Jumping;
                else if (!isGrounded && rb2D.velocity.y < 0)
                    currentState = PlayerState.Falling;
                break;
            case PlayerState.Running:
                if (isGrounded && direction.x == 0)
                    currentState = PlayerState.Idle;
                //else if (!isGrounded && rb2D.velocity.y > 0)
                else if (jumpTriggered)
                    currentState = PlayerState.Jumping;
                else if (!isGrounded && rb2D.velocity.y < 0)
                    currentState = PlayerState.Falling;
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                if ((isGrounded && !jumpPressed) && direction.x == 0)                                    
                    currentState = PlayerState.Idle;                                    
                else if ((isGrounded && !jumpPressed) && direction.x != 0)                                 
                    currentState = PlayerState.Running;                                    
                //if (!isGrounded && rb2D.velocity.y > 0)
                else if (jumpTriggered)
                    currentState = PlayerState.Jumping;
                else if (!isGrounded && rb2D.velocity.y < 0)
                    currentState = PlayerState.Falling;                
                break;            
            default:
                // Default logic
                break;
        }

        // From any State
        // TO-DO
    }
    //////////////////////////////////////////////////
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
        //foreach(Transform groundCheck in groundChecks)
        //{
        //    Debug.DrawRay(groundCheck.position, Vector2.down * rayLength, Color.red);
        //    // Draw 2 aditional lines to make easier the raycast visualization
        //    //Debug.DrawRay(groundCheck.position + (Vector3.right * 0.01f), Vector2.down * rayLength, Color.red);
        //    //Debug.DrawRay(groundCheck.position + (Vector3.left * 0.01f), Vector2.down * rayLength, Color.red);
        //}
        //Debug.DrawRay(groundChecks[0].position, Vector2.down * rayLength, Color.red);
        //Debug.DrawRay(groundChecks[1].position, Vector2.down * rayLength, Color.blue);
        //Debug.DrawRay(groundChecks[2].position, Vector2.down * rayLength, Color.green);        
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
        //Debug.DrawRay(cornerLeftCheck.position, Vector2.up * rayCornerLength, Color.green);
        //Debug.DrawRay(cornerRightCheck.position, Vector2.up * rayCornerLength, Color.green);
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
        else if ((wasGrounded && !isGrounded) && currentState != PlayerState.Jumping)
            coyoteTimerEnabled = true;
    }
    #endregion

    #region Input Player 
    public void JumpActionInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            jumpPressed = true;
            jumpBufferTimer = jumpBufferTime;
        }

        //if (context.phase == InputActionPhase.Performed && (isGrounded || coyoteTimerEnabled))
        //{                        
        //    // Set the Jumping flags & Reset the Jumping Timer
        //    jumpTriggered = true;
        //    jumpPressed = true;
        //    jumpingTimer = 0;

        //    //Set the Jumping Horizontal Speed in func. of the max Horiz Jump Distance and the Max Jump Horiz time
        //    jumpHorizSpeed = maxJumpHorizDist / maxJumpHorizTime;            
        //}

        if(context.phase == InputActionPhase.Canceled)
        {
            jumpPressed = false;            
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
    private void CheckJumpInputBuffer()
    {
        if (jumpBufferTimer > 0)
            jumpBufferTimer -= Time.deltaTime;

        if((isGrounded || coyoteTimerEnabled) && jumpBufferTimer > 0)
        {
            // Set the Jumping flags & Reset the Jumping Timer
            jumpTriggered = true;            
            jumpingTimer = 0;
            jumpBufferTimer = 0;

            //Set the Jumping Horizontal Speed in func. of the max Horiz Jump Distance and the Max Jump Horiz time
            jumpHorizSpeed = maxJumpHorizDist / maxJumpHorizTime;
        }
    }
    #endregion

    #region RigidBody
    public void SetRbAsKinematics()
    {
        rb2D.isKinematic = true;
    }
    public void SetRbAsDynamics()
    {
        rb2D.isKinematic = false;
    }
    #endregion

    #region Movement
    void JumpTrigger()
    {
        jumpTriggered = false;

        // Fix Player's position due to corner detection
        if (cornerDetected == CornerDetected.CornerLeft)
            transform.position -= new Vector3(0.7f, 0f);
        else if (cornerDetected == CornerDetected.CornerRight)
            transform.position += new Vector3(0.7f, 0f);

        CalculateJumpTimes();

        // Trigger Jump Sound
        audioSource.PlayOneShot(jumpAudioFx);
    }
    void CalculateJumpTimes()
    {
        // Solve the MRUA equation--> h = v0*t - (1/2)g*(t^2);

        float discrimMinJumpTime = Mathf.Pow(jumpVertSpeed, 2) - 2 * Physics2D.gravity.y * minJumpDist;
        float discrimMaxJumpTime = Mathf.Pow(jumpVertSpeed, 2) - 2 * Physics2D.gravity.y * maxJumpDist;

        if (discrimMinJumpTime >= 0)
            minJumpingTime = (jumpVertSpeed - Mathf.Sqrt(discrimMinJumpTime)) / Physics2D.gravity.y;
        else
            // Jumping not posible, not enought initial speed
            Debug.LogError("The jumping is not posible with the initial speed and desired height");

        if (discrimMaxJumpTime >= 0)
            maxJumpingTime = (jumpVertSpeed - Mathf.Sqrt(discrimMaxJumpTime)) / Physics2D.gravity.y;
        else
            // Jumping not posible, not enought initial speed
            Debug.LogError("The jumping is not posible with the initial speed and desired height");

        // Max Jumping Time Correction
        maxJumpingTime += 0.03f;
    }    
    void CalculateJumpSpeedMovement()
    {                
        // Calculate the Player's Jump Speed component 
        if (currentState == PlayerState.Jumping)
        {            
            // Jump button released and elapsed min Time
            if (jumpingTimer >= minJumpingTime)
            {
                // Stop giving jump speed;            
                if (!jumpPressed || jumpingTimer >= maxJumpingTime)
                {
                    rb2DJumpVelY *= 0.5f;
                }
                else
                    rb2DJumpVelY = Vector2.up.y * jumpVertSpeed;
            }
            else
            {
                // Jumping force through velocity
                rb2DJumpVelY = Vector2.up.y * jumpVertSpeed;
                //rb2D.velocity = new Vector2(rb2D.velocity.x,rb2DJumpVelY);
            }
        }
        else if (currentState == PlayerState.Falling)
            rb2DJumpVelY = rb2D.velocity.y;

        // Jumping force through Add Force
        //rb2D.AddForce(Vector2.up*jumpForce);                
    }    
    void UpdateMovement()
    {                        
        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
                rb2DDirVelX = direction.x * speed;
                targetVelocity = new Vector2(rb2DDirVelX, rb2D.velocity.y);
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                rb2DDirVelX = direction.x * jumpHorizSpeed;                                                
                targetVelocity = new Vector2(rb2DDirVelX, rb2DJumpVelY);
                break;
            //case PlayerState.Falling:
            //    playerDirVelocity *= 0.8f;
            //    targetVelocity = new Vector2(playerDirVelocity.x, rb2D.velocity.y);
            //    break;
        }

        // Updates the correspondent new velocity                
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            // Don't smooth the Y-axis while jumping
            //rb2D.velocity = new Vector2(
            //                Mathf.SmoothDamp(rb2D.velocity.x, targetVelocity.x, ref dampVelocity.x, smoothTime),
            //                targetVelocity.y);

            rb2D.velocity = new Vector2(
                            Mathf.Lerp(rb2D.velocity.x, targetVelocity.x, Time.fixedDeltaTime * lerpSpeed),
                            targetVelocity.y);
            //rb2D.velocity = targetVelocity;
        }
        else
        {
            // Smooth both axis on normal states
            //rb2D.velocity = Vector2.SmoothDamp(rb2D.velocity, targetVelocity, ref dampVelocity, smoothTime);
            rb2D.velocity = Vector2.Lerp(rb2D.velocity, targetVelocity, Time.fixedDeltaTime * lerpSpeed);
        }
    }
    private void ChangeGravity()
    {
        // Gravity will be heavier when the player is falling down
        if (rb2D.velocity.y < 0)
        {
            if (isDead)
                rb2D.gravityScale = 5f;
            else
                rb2D.gravityScale = 2.5f;
        }
        else
        {
            if (isDead)
                rb2D.gravityScale = 2f;
            else
                rb2D.gravityScale = 1f;
        }
    }
    public void ResetVelocity()
    {
        // Stops the player resetting its velocity
        targetVelocity = Vector2.zero;
    }
    #endregion

    #region Attack
    private void AttackEnemy(GameObject enemy)
    {
        if(isGrounded)
            return;

        rb2D.AddForce(Vector2.up * jumpForce);
        enemy.GetComponent<Animator>().SetTrigger("Death");
        enemy.GetComponent<EnemyMovement>().PlayDeathFx();
        Destroy(enemy,0.5f);
    }
    #endregion

    #region Sprite & Animations
    // Flip the Player sprite in function of its movement
    void FlipSprite(float horizontal)
    {
        if (horizontal < 0 /*&& rb2D.velocity.x < 0*/)
            spriteRenderer.flipX = true;
        else if (horizontal > 0 /*&& rb2D.velocity.x > 0*/)
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

    #region Scene Management
    private void LoadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level02");
    }
    #endregion

    // Old Input System
    //void InputPlayer()
    //{
    //    horizontal = Input.GetAxis("Horizontal");
    //}    
}
