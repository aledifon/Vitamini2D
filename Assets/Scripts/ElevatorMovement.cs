using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ElevatorMovement : MonoBehaviour
{
    // Class vars.
    [Header("Patrol points")]
    [SerializeField] Transform[] pointsObjects;     // Points where I want my elevator to patrol
    Vector2[] points;                               // Patrol's points positions

    Vector3 targetPosition;
    int indexTargetPos;

    // Movement vars
    [Header("Movement")]
    [SerializeField] int normalSpeed;             // Elevator's normal speed                    

    void Awake()
    {        
        // Init the points Vector
        points = new Vector2[pointsObjects.Length];

        // Get all the patrol's points positions
        for (int i = 0; i < pointsObjects.Length; i++)
            points[i] = pointsObjects[i].position;
        // Set the initial Target Pos
        indexTargetPos = 0;
        // Set the initial patrol position
        targetPosition = points[indexTargetPos];        
    }    
    private void Update()
    {
        UpdateTargetPosition();
        Patrol();
    }    
    void UpdateTargetPosition()
    {
        // Update the patrol target points
        if (Vector2.Distance(transform.position, targetPosition) < Mathf.Epsilon)
        {
            if (indexTargetPos == points.Length - 1)
                indexTargetPos = 0;
            else
                indexTargetPos++;

            targetPosition = points[indexTargetPos];
        }
    }
    void Patrol()
    {        
        // Update the ant's position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, normalSpeed * Time.deltaTime);
    }    
}
