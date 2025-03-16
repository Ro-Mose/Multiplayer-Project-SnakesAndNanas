using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformTest : NetworkBehaviour
{
    void Update()
    {
        if (!IsServer) return;

        // The object no longer moves automatically.
        // You can manually update its position from other scripts or via player input.
    }
}
