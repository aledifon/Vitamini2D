using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;

    [Header("UI")]
    [SerializeField] private Image acornLife;
    [SerializeField] private float amountLife;

    [Header("Death")]
    [SerializeField] private float forceJumpDeath;

    [Header("Fading")]
    [SerializeField] private float fadingOutTimer;
    [SerializeField] private float fadeOutDuration;
    [SerializeField] private float fadingTimer;
    [SerializeField] private float fadingTotalDuration;
    public float FadingTotalDuration => fadingTotalDuration;

    // GO Components
    Animator anim;
    PlayerMovement playerMovement;
    AudioSource audioSource;
    SpriteRenderer spriteRenderer;

    // Audio Clips
    [Header("Audio Clips")]
    [SerializeField] AudioClip damageAudioFx;
    [SerializeField] AudioClip deathAudioFx;

    [Header("Camera")]
    [SerializeField] CameraFollow cameraFollow;

    void Awake()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
    }

    // Vitamini's Damage Handler 
    public void TakeDamage(int amount)
    {
        // Avoid Executing the method if Vitamini has already dead
        if (anim.GetBool("Hurt") || currentHealth <= 0)        
            return;
        
        // Update's Vitamini's Life and Acorn Life's UI
        currentHealth -= amount;
        acornLife.fillAmount = currentHealth/maxHealth;

        // Set the Hurt animation  & reset the player's velocity
        anim.SetBool("Hurt", true);
        playerMovement.ResetVelocity();        

        // If Health <=0 --> Executes Death Method
        if (currentHealth <= 0)
        {
            Death();
            return;
        }

        // Play the Damage Fx
        audioSource.PlayOneShot(damageAudioFx);
        // Sprite Fading
        StartCoroutine(nameof(SpriteFading));
        // Disable the Player's Collider during the Hurt Animation & reset the Hurt animation after 1s.
        StartCoroutine(nameof(DisableDamage));
        Invoke("HurtToFalse", 1); 
    }
    private void HurtToFalse()
    {
        anim.SetBool("Hurt", false);
    }
    private IEnumerator DisableDamage()
    {
        // Set the Vitamini's Rb as Kinematics
        playerMovement.SetRbAsKinematics();
        // Disable the Vitamini's Circle Collider
        GetComponent<CircleCollider2D>().enabled = false;

        yield return new WaitUntil(() => (fadingTimer >= fadingTotalDuration));        

        // Re-enable the Vitamini's Damage
        EnableDamage();
    }  
    private IEnumerator SpriteFading()
    {                
        Color targetColor = spriteRenderer.color;
        targetColor.a = 0f;

        fadingTimer = 0f;        

        while (fadingTimer < fadingTotalDuration)
        {
            // Reset the Timer
            fadingOutTimer = 0f;

            // Inverse the Alpha Channel of the Target Color
            if (spriteRenderer.color == targetColor)            
                targetColor.a = targetColor.a == 0f ? 1f : 0f;            

            // Set the Start Color
            Color startColor = spriteRenderer.color;

            
            while (fadingOutTimer < fadeOutDuration)
            {
                // Color fading
                float t = fadingOutTimer / fadeOutDuration;
                spriteRenderer.color = Color.Lerp(startColor, targetColor, t);                
                
                // Timers increment
                fadingOutTimer += Time.deltaTime;
                fadingTimer += Time.deltaTime;

                yield return null;
            }
            spriteRenderer.color = targetColor;
        }

        // Assure the the Sprite Renderer is visible when finish the Coroutine
        targetColor.a = 1f;
        spriteRenderer.color = targetColor;
    }
    private void EnableDamage()
    {
        // Reenable the Vitamini's Circle Collider
        GetComponent<CircleCollider2D>().enabled = true;
        // Set the Vitamini's Rb as Dynamics again
        playerMovement.SetRbAsDynamics();
    }
    private void Death()
    {
        // Stop the Camera Following
        cameraFollow.StopCameraFollow();
        // Set the Player isDead Flag
        playerMovement.IsDead = true;

        // Disable the Vitamini's Circle Collider & Apply an upwards impulse
        GetComponent<CircleCollider2D>().enabled = false;
        GetComponent<Rigidbody2D>().AddForce(Vector2.up * forceJumpDeath);

        // Play the Damage Fx
        audioSource.PlayOneShot(deathAudioFx);
        GameManager.Instance.PlayGameOverFx();

        // Set the New Local Scale
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(2.5f, 2.5f, 2.5f));
    }
}
