using UnityEngine.InputSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

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
    [SerializeField] bool wallJumpTriggered;    // Jumping applied Force

    [SerializeField] float minJumpVertDist;     // Min. Vert. Jumping Dist. Uds.
    [SerializeField] float maxJumpVertDist;     // Max. Vert. Jumping Dist. Uds.    
    [SerializeField] float jumpingTimer;    // Jumping Timer
    private float minJumpingTime;           // Min & Max Jumping Times (in func. of Jumping distance. & Jump. speed)                                            
    private float maxJumpingTime;
    private bool jumpPressed;    

    [SerializeField] private float maxJumpHorizDist;  // Max allowed Horizontal Jumping distance
    //[SerializeField] private float maxJumpHorizTimer; // Horizontal Jumping Timer                                                      
    [SerializeField] private float maxJumpHorizTime;         // Max Jumping time on horizontal Movement
                                                             // (Calculated in func. of maxJumpDistance & Player's speed)        

    [Header("Wall Jump")]
    [SerializeField] float wallJumpVertSpeed;           // Wall Jumping applied Speed
    private float wallJumpHorizSpeed;                   // Wall Jumping applied Speed    
    //[SerializeField] float wallJumpVertDist;          // Wall Jumping Vert. Dist. Uds.
    [SerializeField] private float wallJumpHorizDist;   // Max allowed Horizontal Wall Jumping distance    
    [SerializeField] private float wallJumpHorizTime;   // Max Jumping time on horizontal Movement
    private float wallJumpingVertTime;                          // Wall Jumping Time (in func. of Wall Jumping distance. & Wall Jump. speed)                                                    

    Vector2 wallJumpSpeedVector;

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
    [SerializeField] private bool jumpBufferTimerEnabled;

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
    [SerializeField] LayerMask groundLayer;     //Ground Layer
    [SerializeField] float rayLength;           //Raycast Length
    [SerializeField] bool isGrounded;           //Ground touching flag
    [SerializeField] bool isJumping;            //Jumping/Falling/WallJumping Flag
    [SerializeField] private bool isRayGroundDetected;       // Aux. Ray Ground var
    [SerializeField] private bool isRecentlyJumping;           // Aux. var 
    public bool IsGrounded => isGrounded;
    private bool wasGrounded;               //isGrounded value of previous frame

    [Header("Raycast Walls")]
    [SerializeField] Transform wallLeftCheck;     //Raycast origin point 
    [SerializeField] Transform wallRightCheck;     //Raycast origin point 
    [SerializeField] LayerMask wallLayer;       //Wall Layer
    [SerializeField] float rayWallLength;    //Raycast Wall Fwd Length
    [SerializeField] bool isWallDetected;       //Wall Fwd detection flag    
    Vector2 rayWallOrigin;
    Vector2 rayWallDir;
    [SerializeField] private bool isRayWallDetected;       // Aux. Ray Wall var
    [SerializeField] private bool isRecentlyWallJumping;     // Aux. var 

    [Header("Platform")]
    [SerializeField] LayerMask platformLayer;

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
        WallJumping,
        Falling,
        WallBraking,
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

        // Ground Raycasts Debugging
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(rayWallOrigin, rayWallDir * rayWallLength);
        Gizmos.DrawRay(rayWallOrigin + (Vector2.up * 0.01f), rayWallDir * rayWallLength);
        Gizmos.DrawRay(rayWallOrigin + (Vector2.up * 0.02f), rayWallDir * rayWallLength);
        Gizmos.DrawRay(rayWallOrigin + (Vector2.down * 0.01f), rayWallDir * rayWallLength);
        Gizmos.DrawRay(rayWallOrigin + (Vector2.down * 0.02f), rayWallDir * rayWallLength);
    }
    void EnableSlowMotion()
    {
        Time.timeScale = 0.2f; // Velocidad al 20%
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Ajustar física
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

        // Just for debugging
        //EnableSlowMotion();
    }
    private void Update()
    {                       
        // Update the Input player velocity        
        //inputPlayerVelocity = new Vector2(horizontal * speed, 0);
        // Update the target Velocity
        //targetVelocity += rb2D.velocity + inputPlayerVelocity;
                        
    }
    void FixedUpdate()
    {
        // Last State vars Update
        wasGrounded = isGrounded;        

        // Launch the raycast to detect the ground
        RaycastGrounded();
        // Launch the raycast to detect the ceiling
        RaycastCeiling();
        // Launch the raycast to detect Vertical Walls
        RaycastVertWalls();
        // Controls if the player is on the elevator
        //Elevator();

        // Update the isGrounded, isWallDetected & isJumping Flags
        isGrounded = isRayGroundDetected && !isRecentlyJumping;
        isWallDetected = isRayWallDetected && !isRecentlyWallJumping;
        isJumping = (currentState == PlayerState.Jumping || 
                    currentState == PlayerState.Falling ||
                    currentState == PlayerState.WallJumping);

        // Jump Input Buffer
        CheckJumpTrigger();
        if (jumpBufferTimerEnabled)
            UdpateJumpBufferTimer();

        // Coyote Timer
        CheckCoyoteTimer();
        if (coyoteTimerEnabled)
            UdpateCoyoteTimer();

        // Update the player state
        UpdatePlayerState();

        // Jumping Timer
        if (isJumping)        
            UpdateJumpTimer();

        // Update the Horiz & Vertical Player's Speed
        UpdateHorizSpeed();
        UpdateVerticalSpeed();                    
        // Update the Player's Movement
        UpdatePlayerSpeed();

        // Update the player's gravity when falling down
        ChangeGravity();
       
        // Jumping Animation
        //AnimatingJumpìng();
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
                if (isGrounded)
                {
                    if (direction.x != 0)
                        currentState = PlayerState.Running;
                    //else if (!isGrounded && rb2D.velocity.y > 0)
                    else if (jumpTriggered)
                    {
                        TriggerJump();
                        currentState = PlayerState.Jumping;                        
                    }
                }                
                else if (rb2D.velocity.y < Mathf.Epsilon)
                    currentState = PlayerState.Falling;

                //Debug.Log("From Idle state to " + currentState + ". Time: " + (Time.realtimeSinceStartup * 1000f) + "ms");
                break;
            case PlayerState.Running:  
                if (isGrounded)
                {
                    if (direction.x == 0)
                        currentState = PlayerState.Idle;
                    //else if (!isGrounded && rb2D.velocity.y > 0)
                    else if (jumpTriggered)
                    {
                        TriggerJump();
                        currentState = PlayerState.Jumping;                        
                    }                                            
                }
                else if (rb2D.velocity.y < Mathf.Epsilon)
                    currentState = PlayerState.Falling;

                //Debug.Log("From Running state to " + currentState + ". Time: " + (Time.realtimeSinceStartup * 1000f) + "ms");
                break;
            case PlayerState.Jumping:
                if (rb2D.velocity.y < 0 && !isRecentlyJumping)
                {
                    currentState = PlayerState.Falling;
                }
                //Debug.Log("From Jumping state to " + currentState + ". Time: " + (Time.realtimeSinceStartup * 1000f) + "ms");
                break;
            case PlayerState.WallJumping:
                if (rb2D.velocity.y < 0 && !isRecentlyWallJumping) 
                {                                        
                    currentState = PlayerState.Falling;                    
                }                
                //Debug.Log("From Wall Jumping state to " + currentState + ". Time: " + (Time.realtimeSinceStartup * 1000f) + "ms");
                break;
            case PlayerState.Falling:
                if (jumpTriggered)
                {
                    TriggerJump();
                    currentState = PlayerState.Jumping;                    
                }                    
                else if (isGrounded)
                {
                    //if (/*!jumpPressed &&*/ direction.x == 0)
                    //    currentState = PlayerState.Idle;
                    //else if (/*!jumpPressed &&*/ direction.x != 0)
                    //    currentState = PlayerState.Running;

                    currentState = PlayerState.Idle;
                }
                else if (isWallDetected)
                {                
                    currentState = PlayerState.WallBraking;
                    //Debug.Log("Switched to WallBraking state");
                }

                //Debug.Log("From Falling state to " + currentState + ". Time: " + (Time.realtimeSinceStartup * 1000f) + "ms");
                break;
            case PlayerState.WallBraking:
                if (isGrounded)
                {
                    //if (/*!jumpPressed &&*/ direction.x == 0)
                    //    currentState = PlayerState.Idle;
                    //else if (/*!jumpPressed &&*/ direction.x != 0)
                    //    currentState = PlayerState.Running;

                    currentState = PlayerState.Idle;
                }
                else
                {
                    if (!isWallDetected)
                    {
                        currentState = PlayerState.Falling;
                    }
                    else if(wallJumpTriggered)
                    {
                        TriggerWallJump();
                        currentState = PlayerState.WallJumping;
                        //Debug.Log("Switched to WallJumping state");
                    }                        
                }
                //Debug.Log("From Falling state to " + currentState + ". Time: " + (Time.realtimeSinceStartup * 1000f) + "ms");
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
        isRayGroundDetected = false;
        for (int i = 0; i < groundChecks.Count(); i++)
        {
            raycastsHit2D[i] = Physics2D.Raycast(groundChecks[i].position, Vector2.down, rayLength, (int)groundLayer | (int)platformLayer);
            isRayGroundDetected |= raycastsHit2D[i];
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
        raycastCornerLeft = Physics2D.Raycast(cornerLeftCheck.position, Vector2.up, rayCornerLength, (int)groundLayer | (int)platformLayer);
        raycastCornerRight = Physics2D.Raycast(cornerRightCheck.position, Vector2.up, rayCornerLength, (int)groundLayer | (int)platformLayer);

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
    void RaycastVertWalls()
    {
        // Raycast Launching
        RaycastHit2D raycastWallFwd;

        // Calculate RaycastWallDirection & rayWallOrigin
        rayWallDir = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        rayWallOrigin = spriteRenderer.flipX ? wallLeftCheck.position : wallRightCheck.position;

        raycastWallFwd = Physics2D.Raycast(rayWallOrigin, rayWallDir, rayWallLength, wallLayer);

        // Update the Wall detection
        isRayWallDetected = raycastWallFwd;

        // Raycast Debugging        
        //Debug.DrawRay(rayWallOrigin, rayWallDir * rayWallLength, Color.blue);        
    }
    #endregion

    #region Coyote Time
    private void UdpateCoyoteTimer()
    {
        // Coyote Timer update
        coyoteTimer += Time.fixedDeltaTime;

        // Reset Coyote Timer
        if (coyoteTimer >= maxCoyoteTime)
        {
            coyoteTimerEnabled = false;
            coyoteTimer = 0f;
        }                           
    }
    private void CheckCoyoteTimer()
    {        
        // Coyote Timer will be triggered when the player stop touching the ground
        if ((wasGrounded && !isGrounded) /*&& currentState == PlayerState.Falling*/ && !coyoteTimerEnabled) 
        {
            coyoteTimerEnabled = true;
            coyoteTimer = 0f;
        }            
    }        
    #endregion

    #region Input Player 
    public void JumpActionInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            jumpPressed = true;

            // Enable the Jump Buffer Timer            
            SetJumpBufferTimer();
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
    #region Jumping Buffer    
    private void UdpateJumpBufferTimer()
    {
        // Jump Buffer Timer update
        jumpBufferTimer -= Time.fixedDeltaTime;

        // Reset Jump Buffer Timer
        if (jumpBufferTimer <= 0)
        {
            ResetJumpBufferTimer();
        }
    }
    private void SetJumpBufferTimer()
    {        
        jumpBufferTimerEnabled = true;
        jumpBufferTimer = jumpBufferTime;
    }
    private void ResetJumpBufferTimer()
    {
        jumpBufferTimerEnabled = false;
        jumpBufferTimer = 0f;
    }
    private void CheckJumpTrigger()
    {        
        // If a possible Normal Jump is detected (Either through isGrounded or through CoyoteTime)
        if((isGrounded || coyoteTimerEnabled) && jumpBufferTimerEnabled)
        {
            // Reset the Jumping Buffer Timer
            ResetJumpBufferTimer();
            // Reset the Jumping Timer
            ResetJumpTimer();
            //Set the Jumping Horizontal Speed in func. of the max Horiz Jump Distance and the Max Jump Horiz time
            jumpHorizSpeed = maxJumpHorizDist / 
                            maxJumpHorizTime;

            // Register a Jumping Trigger Request
            jumpTriggered = true;
        }
        // Otherwise, if a Wall Jump is detected (wallFwdDetected by raycastWallFwd)
        else if ((!isGrounded && isWallDetected) && jumpBufferTimerEnabled)
        {
            // Reset the Jumping Buffer Timer             
            ResetJumpBufferTimer();
            // Reset the Jumping Timer
            // (Will be used to calculate the min/maxJumpingTimes on CalculateJumpSpeedMovement)  
            ResetJumpTimer();

            //Set the Jumping Horizontal Speed in func. of the max Horiz Jump Distance and the Max Jump Horiz time            
            wallJumpHorizSpeed = wallJumpHorizDist /
                                wallJumpHorizTime;

            // Register a Wall Jumping Trigger Request
            wallJumpTriggered = true;
        }
    }
    private void UpdateJumpTimer()
    {
        jumpingTimer += Time.fixedDeltaTime;
    }
    private void ResetJumpTimer()
    {
        jumpingTimer = 0;
    }
    #endregion
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

    #region Jumping
    void TriggerJump()
    {        
        // Clear the jumpTriggered Flag (Avoid to enter on undesired States)
        jumpTriggered = false;

        // Enable the isJumpTriggered for a certain time;
        isRecentlyJumping = true;
        //Invoke(nameof(DisableIsJumpTriggered),0.2f);
        StartCoroutine(nameof(DisableJumpTriggerFlag));

        // Fix Player's position due to corner detection
        if (cornerDetected == CornerDetected.CornerLeft)
            transform.position -= new Vector3(0.7f, 0f);
        else if (cornerDetected == CornerDetected.CornerRight)
            transform.position += new Vector3(0.7f, 0f);

        CalculateJumpTimes(jumpVertSpeed);

        // Trigger Jump Sound
        audioSource.PlayOneShot(jumpAudioFx);
    }
    IEnumerator DisableJumpTriggerFlag()
    {
        yield return new WaitForSeconds(0.2f);
        isRecentlyJumping = false;
    }
    //void DisableJumpTriggerFlag()
    //{
    //    isJumpTriggered = false;
    //}
    void TriggerWallJump()
    {        
        // Clear the wallJumpTriggered Flag (Avoid to enter on undesired States)
        wallJumpTriggered = false;

        // Enable the isWallJumpTriggered for a certain time;
        isRecentlyWallJumping = true;
        //Invoke(nameof(DisableWallJumpTriggerFlag), 0.2f);        
        StartCoroutine(nameof(DisableWallJumpTriggerFlag));

        //CalculateWallJumpTimes();                                                                   
        CalculateJumpTimes(wallJumpVertSpeed);

        // Reset Rb velocity to avoid inconsistencies
        rb2D.velocity = Vector2.zero;

        //wallSpeedVector = (-rayWallDir * Mathf.Cos(Mathf.Deg2Rad * 30) * wallJumpForce) +
        //                    (Vector2.up * Mathf.Sin(Mathf.Deg2Rad * 30) * wallJumpForce);
        
        wallJumpSpeedVector = (-rayWallDir * wallJumpHorizSpeed) +
                            (Vector2.up * wallJumpVertSpeed);        

        //rb2D.AddForce(wallForce, ForceMode2D.Impulse);
        //rb2D.velocity = wallJumpSpeedVector;        

        // Trigger Jump Sound
        audioSource.PlayOneShot(jumpAudioFx);

        // Flip Sprite
        //spriteRenderer.flipX = !spriteRenderer.flipX;
    }
    IEnumerator DisableWallJumpTriggerFlag()
    {
        yield return new WaitForSeconds(0.2f);
        isRecentlyWallJumping = false;
    }
    //void DisableWallJumpTriggerFlag()
    //{
    //    isWallJumpTriggered = false;
    //}
    void CalculateJumpTimes(float jumpSpeed)
    {
        // Solve the MRUA equation--> h = v0*t - (1/2)g*(t^2);

        float discrimMinJumpTime = Mathf.Pow(jumpSpeed, 2) - 2 * Physics2D.gravity.y * minJumpVertDist;
        float discrimMaxJumpTime = Mathf.Pow(jumpSpeed, 2) - 2 * Physics2D.gravity.y * maxJumpVertDist;

        if (discrimMinJumpTime >= 0)
            minJumpingTime = (jumpSpeed - Mathf.Sqrt(discrimMinJumpTime)) / Physics2D.gravity.y;
        else
            // Jumping not posible, not enought initial speed
            Debug.LogError("The jumping is not posible with the initial speed and desired height");

        if (discrimMaxJumpTime >= 0)
            maxJumpingTime = (jumpSpeed - Mathf.Sqrt(discrimMaxJumpTime)) / Physics2D.gravity.y;
        else
            // Jumping not posible, not enought initial speed
            Debug.LogError("The jumping is not posible with the initial speed and desired height");

        // Max Jumping Time Correction
        maxJumpingTime += 0.03f;
    }
    //void CalculateWallJumpTimes()
    //{
    //    // Solve the MRUA equation--> h = v0*t - (1/2)g*(t^2);

    //    float discrimWallJumpTime = Mathf.Pow(wallJumpVertSpeed, 2) - 2 * Physics2D.gravity.y * wallJumpVertDist;        

    //    if (discrimWallJumpTime >= 0)
    //        wallJumpingVertTime = (wallJumpVertSpeed - Mathf.Sqrt(discrimWallJumpTime)) / Physics2D.gravity.y;
    //    else
    //        // Wall Jumping not posible, not enought initial speed
    //        Debug.LogError("The Wall jumping is not posible with the initial speed and desired height");        

    //    // Wall Jumping Time Correction
    //    //wallJumpingTime += 0.03f;
    //}
    #endregion

    #region Movement
    void UpdateHorizSpeed()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
                rb2DDirVelX = direction.x * speed;
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                rb2DDirVelX = direction.x * jumpHorizSpeed;
                break;
            case PlayerState.WallJumping:                
                // X-velocity starts decreasing after a certain time
                if (jumpingTimer >= wallJumpHorizTime)
                {
                    rb2DDirVelX = 0;
                }
                else
                {
                    // Jumping force through velocity
                    rb2DDirVelX = wallJumpSpeedVector.x;
                    //rb2D.velocity = new Vector2(rb2D.velocity.x,rb2DJumpVelY);
                }
                break;
            case PlayerState.WallBraking:
                rb2DDirVelX = 0;
                break;
            default:
                break;
        }
    }
    void UpdateVerticalSpeed()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
            case PlayerState.Falling:
            case PlayerState.WallBraking:
                rb2DJumpVelY = rb2D.velocity.y;
                break;
            case PlayerState.Jumping:
            case PlayerState.WallJumping:
                // Jump button released and elapsed min Time
                if (jumpingTimer >= minJumpingTime)
                {
                    // Stop giving jump speed;            
                    if (!jumpPressed || jumpingTimer >= maxJumpingTime)
                    {
                        rb2DJumpVelY *= 0.5f;
                    }
                    else
                    {
                        if (currentState == PlayerState.Jumping)
                            rb2DJumpVelY = Vector2.up.y * jumpVertSpeed;
                        else if (currentState == PlayerState.WallJumping)
                            rb2DJumpVelY = Vector2.up.y * wallJumpVertSpeed;
                    }                        
                }
                else
                {                    
                    if (currentState == PlayerState.Jumping)
                        rb2DJumpVelY = Vector2.up.y * jumpVertSpeed;
                    else if (currentState == PlayerState.WallJumping)
                        rb2DJumpVelY = Vector2.up.y * wallJumpVertSpeed;
                }
                break;                        
            default:
                break;
        }

        // Jumping force through Add Force
        //rb2D.AddForce(Vector2.up*jumpForce);               
    }    
    void UpdatePlayerSpeed()
    {
        targetVelocity = new Vector2(rb2DDirVelX, rb2DJumpVelY);

        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
                // Smooth both axis on normal states
                //rb2D.velocity = Vector2.SmoothDamp(rb2D.velocity, targetVelocity, ref dampVelocity, smoothTime);
                rb2D.velocity = Vector2.Lerp(rb2D.velocity, targetVelocity, Time.fixedDeltaTime * lerpSpeed);
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                // Don't smooth the Y-axis while jumping
                //rb2D.velocity = new Vector2(
                //                Mathf.SmoothDamp(rb2D.velocity.x, targetVelocity.x, ref dampVelocity.x, smoothTime),
                //                targetVelocity.y);

                rb2D.velocity = new Vector2(
                                Mathf.Lerp(rb2D.velocity.x, targetVelocity.x, Time.fixedDeltaTime * lerpSpeed),
                                targetVelocity.y);
                //rb2D.velocity = targetVelocity;
                break;
            case PlayerState.WallBraking:
                rb2D.velocity = targetVelocity;
                // Try the same as on Idle and Running States? (To check the feeling)
                //rb2D.velocity = Vector2.Lerp(rb2D.velocity, targetVelocity, Time.fixedDeltaTime * lerpSpeed);
                break;
            case PlayerState.WallJumping:                      
                rb2D.velocity = targetVelocity;
                break;
            default:
                break;
        }
    }

    //void UpdateVerticalSpeedOld()
    //{        
    //    // Calculate the Player's Jump Speed component 
    //    if (currentState == PlayerState.Jumping)
    //    {
    //        // Jump button released and elapsed min Time
    //        if (jumpingTimer >= minJumpingTime)
    //        {
    //            // Stop giving jump speed;            
    //            if (!jumpPressed || jumpingTimer >= maxJumpingTime)
    //            {
    //                rb2DJumpVelY *= 0.5f;
    //            }
    //            else
    //                rb2DJumpVelY = Vector2.up.y * jumpVertSpeed;
    //        }
    //        else
    //        {
    //            // Jumping force through velocity
    //            rb2DJumpVelY = Vector2.up.y * jumpVertSpeed;
    //            //rb2D.velocity = new Vector2(rb2D.velocity.x,rb2DJumpVelY);
    //        }
    //    }
    //    else if (currentState == PlayerState.WallJumping)
    //    {
    //        // TO IMPLEMENT
    //    }
    //    else if (currentState == PlayerState.Falling)
    //    {
    //        rb2DJumpVelY = rb2D.velocity.y;
    //    }

    //    // Jumping force through Add Force
    //    //rb2D.AddForce(Vector2.up*jumpForce);                
    //}

    //void UpdatePlayerSpeedOld()
    //{                        
    //    switch (currentState)
    //    {
    //        case PlayerState.Idle:
    //        case PlayerState.Running:
    //            rb2DDirVelX = direction.x * speed;
    //            targetVelocity = new Vector2(rb2DDirVelX, rb2D.velocity.y);
    //            break;
    //        case PlayerState.Jumping:
    //        case PlayerState.Falling:
    //            rb2DDirVelX = direction.x * jumpHorizSpeed;                                                
    //            targetVelocity = new Vector2(rb2DDirVelX, rb2DJumpVelY);
    //            break;            
    //        case PlayerState.WallJumping:
    //            targetVelocity = wallSpeedVector;
    //            break;
    //    }

    //    // Updates the correspondent new velocity
    //    // Jumping or Falling States
    //    if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
    //    {
    //        // Don't smooth the Y-axis while jumping
    //        //rb2D.velocity = new Vector2(
    //        //                Mathf.SmoothDamp(rb2D.velocity.x, targetVelocity.x, ref dampVelocity.x, smoothTime),
    //        //                targetVelocity.y);

    //        rb2D.velocity = new Vector2(
    //                        Mathf.Lerp(rb2D.velocity.x, targetVelocity.x, Time.fixedDeltaTime * lerpSpeed),
    //                        targetVelocity.y);
    //        //rb2D.velocity = targetVelocity;
    //    }
    //    else if (currentState == PlayerState.WallBraking)
    //    {
    //        rb2D.velocity = new Vector2(0,rb2D.velocity.y);
    //    }
    //    //else if (currentState == PlayerState.WallJumping && !isRecentlyWallJumping)
    //    //{            
    //    //    rb2D.velocity = targetVelocity;
    //    //}
    //    // Idle or Running States
    //    else
    //    {
    //        // Smooth both axis on normal states
    //        //rb2D.velocity = Vector2.SmoothDamp(rb2D.velocity, targetVelocity, ref dampVelocity, smoothTime);
    //        rb2D.velocity = Vector2.Lerp(rb2D.velocity, targetVelocity, Time.fixedDeltaTime * lerpSpeed);
    //    }
    //}
    private void ChangeGravity()
    {
        //Gravity will be heavier when the player is falling down
        if (currentState == PlayerState.WallBraking)
        {            
            rb2D.gravityScale = 0.5f;
        }        
        else if (currentState == PlayerState.Falling)
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
