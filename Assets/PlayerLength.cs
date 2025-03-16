using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLength : NetworkBehaviour
{
    [SerializeField] private GameObject ballTail;
    public NetworkVariable<ushort> length = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<GameObject> tails;
    private Transform lastTail;
    private Collider2D collider2d;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        tails = new List<GameObject>();
        lastTail = transform;
        collider2d = GetComponent<Collider2D>();
        if (!IsServer) length.OnValueChanged += LengthChanged;
    }

    [ContextMenu("Add Length")]
    public void AddLength()
    {
        length.Value += 1;
        InstantiateTail();
    }

    private void LengthChanged(ushort previousValue, ushort newValue)
    {
        Debug.Log("Length Changed.");
        InstantiateTail();
    }

    private void InstantiateTail()
    {
        GameObject tailObject = Instantiate(ballTail, transform.position, Quaternion.identity);
        tailObject.GetComponent<SpriteRenderer>().sortingOrder = -length.Value;

        if (tailObject.TryGetComponent(out Tail tail))
        {
            tail.networkedOwner = transform;
            tail.followTransform = lastTail;
            lastTail = tailObject.transform; // Update to use the instantiated tailObject's transform
            Physics2D.IgnoreCollision(tailObject.GetComponent<Collider2D>(), collider2d);
        }

        tails.Add(tailObject);
    }
}
