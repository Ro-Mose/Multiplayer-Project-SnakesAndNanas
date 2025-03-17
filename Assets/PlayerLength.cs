using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLength : NetworkBehaviour
{
    [SerializeField] private GameObject ballTail;
    public NetworkVariable<ushort> length = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // event for when the length changes
    public static event System.Action<ushort> ChangedLengthEvent;

    private List<GameObject> tails;
    private Transform lastTail;
    private Collider2D collider2d;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        tails = new List<GameObject>();
        lastTail = transform;
        collider2d = GetComponent<Collider2D>();

        if (!IsServer)
        {
            length.OnValueChanged += LengthChangedEvent;
        }
    }

    //adds player's length
    [ContextMenu("Add Length")]
    public void AddLength()
    {
        length.Value += 1;
        LengthChanged();
    }

    //notifies when length is changed.
    private void LengthChanged()
    {
        InstantiateTail();

        if (!IsOwner)
        {
            return;
        }
        ChangedLengthEvent?.Invoke(length.Value);
    }

    // changes tail length
    private void LengthChangedEvent(ushort previousValue, ushort newValue)
    {
        Debug.Log("Length Changed.");
        LengthChanged();
    }

    // Makes another tail
    private void InstantiateTail()
    {
        GameObject tailObject = Instantiate(ballTail, transform.position, Quaternion.identity);
        tailObject.GetComponent<SpriteRenderer>().sortingOrder = -length.Value;

        if (tailObject.TryGetComponent(out Tail tail))
        {
            tail.networkedOwner = transform;
            tail.followTransform = lastTail;
            lastTail = tailObject.transform;
            Physics2D.IgnoreCollision(tailObject.GetComponent<Collider2D>(), collider2d);
        }

        tails.Add(tailObject);
    }
}
