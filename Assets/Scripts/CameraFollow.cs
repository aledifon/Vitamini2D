using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{        
    [SerializeField] Transform player;
    Vector3 offset;    // Initial distance between the camera and the player
    [SerializeField] float smoothTargetTime;
        
    Vector3 smoothDampVelocity;            // Player's current speed storage (Velocity Movement type through r        

    // Start is called before the first frame update
    void Start()
    {
        offset = transform.position - player.position;  // Calculate the initial distance between the camera and the player
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        transform.position = Vector3.SmoothDamp(transform.position, player.position + offset, 
                                                ref smoothDampVelocity, smoothTargetTime);
    }
}
