using UnityEngine;

public class Tail : MonoBehaviour
{
    public Transform networkedOwner; 
    public Transform followTransform;

    [SerializeField] private float delayTime = 0.1f;
    [SerializeField] private float distance = 0.5f;
    [SerializeField] private float moveStep = 10f;

    private Vector3 targetPosition;

    private void Update()
    {
        if (followTransform != null)
        {
            targetPosition = followTransform.position - followTransform.forward * distance;
            targetPosition += (transform.position - targetPosition) * delayTime;
            targetPosition.z = 0f;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveStep);
        }
    }

    public void SetFollowTransform(Transform newFollowTransform)
    {
        if (newFollowTransform != null)
        {
            followTransform = newFollowTransform;
        }
    }
}
