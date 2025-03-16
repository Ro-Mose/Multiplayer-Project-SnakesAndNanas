using Unity.Netcode;
using UnityEngine;

public class Food : NetworkBehaviour
{
    [SerializeField] private AudioClip bananaEatSound; // The sound to play when the banana is eaten
    private AudioSource audioSource; // AudioSource component for playing the sound

    private void Start()
    {
        // Dynamically add an AudioSource if it's missing
        audioSource = GetComponent<AudioSource>();

        // If no AudioSource is found, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Optionally, configure the AudioSource (like setting volume, loop, etc.)
        audioSource.playOnAwake = false;  // Prevent it from playing right away
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

        if (col.TryGetComponent(out PlayerLength playerLength))
        {
            playerLength.AddLength();
        }

        // Call the ClientRpc to play the sound on the client
        PlayEatSoundClientRpc();

        // Now despawn the banana object
        NetworkObject.Despawn();
    }

    // ClientRpc to play sound on the client who interacted with the food
    [ClientRpc]
    private void PlayEatSoundClientRpc()
    {
        if (audioSource != null && bananaEatSound != null)
        {
            audioSource.PlayOneShot(bananaEatSound);
        }
        else
        {
            Debug.LogError("AudioSource or AudioClip is missing in PlayEatSoundClientRpc.");
        }
    }
}
