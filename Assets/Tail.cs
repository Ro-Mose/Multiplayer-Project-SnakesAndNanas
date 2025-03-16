using UnityEngine;

public class Tail : MonoBehaviour
{
    public Transform networkedOwner;    // Reference to the player's transform (or head)
    public Transform followTransform;   // The tail that this tail follows

    [SerializeField] private float delayTime = 0.1f;
    [SerializeField] private float distance = 0.5f;
    [SerializeField] private float moveStep = 10f;

    private Vector3 targetPosition;

    private void Update()
    {
        // Make sure the followTransform is updated properly
        if (followTransform != null)
        {
            targetPosition = followTransform.position - followTransform.forward * distance;
            targetPosition += (transform.position - targetPosition) * delayTime;
            targetPosition.z = 0f;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveStep);
        }
    }

    // Method to set up the tail following behavior (for new tails)
    public void SetFollowTransform(Transform newFollowTransform)
    {
        followTransform = newFollowTransform;
    }
}
