using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Global vars
    [Header("Velocity")]
    [SerializeField] float speed;
    [SerializeField] float smoothTime; // time will takes to reach the speed.

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
    }
    private void Update()
    {
        InputPlayer();

        // Update the target Velocity
        targetVelocity = new Vector2(horizontal * speed, rb2D.velocity.y);

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
    }
    void InputPlayer()
    {
        horizontal = Input.GetAxis("Horizontal");
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
    }
}
