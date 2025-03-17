using Unity.Netcode;
using UnityEngine;

public class Food : NetworkBehaviour
{
    [SerializeField] private AudioClip bananaEatSound; 
    private AudioSource audioSource; 

    private void Start()
    {
        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // If there's no AudioSource, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player"))
        {
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        //Player eats food increase length
        if (col.TryGetComponent(out PlayerLength playerLength))
        {
            playerLength.AddLength(); 
        }

        //Why isnt this working???
        PlayEatSoundClientRpc();

        //Removes banana
        NetworkObject.Despawn();
    }

    [ClientRpc]
    private void PlayEatSoundClientRpc()
    {
        // Play the banana eating sound for all clients
        if (audioSource != null && bananaEatSound != null)
        {
            Debug.Log("Banana eaten sound play");
            audioSource.clip = bananaEatSound;  
            audioSource.Play();  // Play the sound (Why is it not working???)
        }
    }
}
