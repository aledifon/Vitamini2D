using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyMovement : MonoBehaviour
{
    // Class vars.
    [Header("Patrol points")]
    [SerializeField] Transform [] pointsObjects;    // Points where I want my enemy to patrol
    Vector2[] points;                               // Patrol's points positions

    Vector3 targetPosition;
    int indexTargetPos;

    // Movement vars
    [Header("Movement")]
    [SerializeField] int normalSpeed;             // Ant's normal speed    
    [SerializeField] int boostedSpeed;            // Ant's boosted speed (whenever a player is detected)
    int currentSpeed;                             // Ant's current Speed

    [Header("Raycast")]    
    [SerializeField] LayerMask playerLayer;         // Player Layer
    [SerializeField] float rayLength;               // Raycast Length
    [SerializeField] bool isDetecting;              // Player detection flag
    Vector2 raycastDir;

    // GOs 
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Get component
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Init the points Vector
        points = new Vector2[pointsObjects.Length];

        // Get all the patrol's points positions
        for (int i = 0; i < pointsObjects.Length; i++)        
            points[i] = pointsObjects[i].position;
        // Set the initial Target Pos
        indexTargetPos = 0;
        // Set the initial patrol position
        targetPosition = points[indexTargetPos];

        // Init raycastDir        
        raycastDir = Vector2.left;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        DetectPlayer();
        UpdateTargetPosition();
        Patrol();
    }

    private void Update()
    {
        FlipSprite();
    }
    void DetectPlayer()
    {
        // Update raycastDirection
        if (spriteRenderer.flipX)
            raycastDir = Vector2.right;
        else
            raycastDir = Vector2.left;

        // Raycast Launching
        isDetecting = Physics2D.Raycast(transform.position, raycastDir, rayLength, playerLayer);
        // Raycast Debugging
        Debug.DrawRay(transform.position, raycastDir * rayLength, Color.red);
    }
    void UpdateTargetPosition()
    {
        // Update the patrol target points
        if (Vector2.Distance(transform.position, targetPosition) < Mathf.Epsilon)
        {            
            if (indexTargetPos == points.Length-1)
                indexTargetPos = 0;
            else
                indexTargetPos++;

            targetPosition = points[indexTargetPos];
        }
    }
    void Patrol()
    {
        // Update the currentSpeed in case a player is detected or not
        if (isDetecting)
            currentSpeed = boostedSpeed;
        else
            currentSpeed = normalSpeed;

        // Update the ant's position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.fixedDeltaTime);        
    }
    // Flip the Enemy's sprite in function of its movement
    void FlipSprite()
    {        
        if (targetPosition.x > transform.position.x)        
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
    }
}
