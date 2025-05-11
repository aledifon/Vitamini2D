using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton instance of GameManager
    private static GameManager instance;
    public static GameManager Instance
    {
        get 
        { 
            if (instance == null)
            {
                GameManager instance = FindAnyObjectByType<GameManager>();

                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance; 
        }
    }

    [SerializeField] AudioClip gameOverClip;
    AudioSource generalAudioSource;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance != null)
            Destroy(gameObject);

        instance = this;

        generalAudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    public void PlayGameOverFx()
    {
        if(generalAudioSource.isPlaying)
            generalAudioSource.Stop();

        generalAudioSource.PlayOneShot(gameOverClip);
    }
}
