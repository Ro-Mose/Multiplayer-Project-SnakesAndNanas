using System;
using Unity.Netcode;
using UnityEngine;

namespace SnakesAndNanas
{
    public class PlayerController : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
        public NetworkVariable<Quaternion> Rotation = new NetworkVariable<Quaternion>();

        //movement speed
        [SerializeField] private float speed = 3f;
        private Vector2 movementInput;
        private PlayerLength playersLength;

        private readonly ulong[] targetClientsArray = new ulong[1];

        public static event System.Action GameOverEvent;

        //sets up player length
        private void Initialize()
        {
            playersLength = GetComponent<PlayerLength>();
        }

        public override void OnNetworkSpawn()
        {
            Initialize();
            if (IsServer)
            {
                Position.Value = transform.position;
                Rotation.Value = transform.rotation;
            }
            Position.OnValueChanged += OnStateChanged;
            Rotation.OnValueChanged += OnRotationChanged;
        }

        public override void OnNetworkDespawn()
        {
            Position.OnValueChanged -= OnStateChanged;
            Rotation.OnValueChanged -= OnRotationChanged;
        }

        private void Update()
        {
            if (!IsOwner || !Application.isFocused) return;

            // Get movement input from the player
            movementInput.x = Input.GetAxisRaw("Horizontal");
            movementInput.y = Input.GetAxisRaw("Vertical");

            Vector2 moveDirection = movementInput.normalized;

            if (moveDirection != Vector2.zero)
            {
                //moves player to show on the server
                RequestMoveServerRpc(moveDirection);
            }
        }

        [ServerRpc]
        private void RequestMoveServerRpc(Vector2 moveDirection, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerController player = GetPlayerByClientId(clientId);
            if (player == null) return;

            // Calculate new position and rotation
            Vector3 newPosition = player.transform.position + (Vector3)(moveDirection * speed * Time.deltaTime);
            player.Position.Value = newPosition;

            if (moveDirection != Vector2.zero)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                Quaternion newRotation = Quaternion.Euler(0, 0, angle);
                player.Rotation.Value = newRotation;
            }

            player.Move(newPosition, player.Rotation.Value);
        }

        private void Move(Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;
        }

        // Update position on all clients
        private void OnStateChanged(Vector3 previous, Vector3 current)
        {
            transform.position = current;
        }

        // Update rotation on all clients
        private void OnRotationChanged(Quaternion previous, Quaternion current)
        {
            transform.rotation = current;
        }

        private PlayerController GetPlayerByClientId(ulong clientId)
        {
            foreach (var obj in FindObjectsOfType<PlayerController>())
            {
                if (obj.OwnerClientId == clientId) return obj;
            }
            return null;
        }

        [ServerRpc(RequireOwnership = false)]
        private void DeterminedCollisionWinnerServerRpc(PlayerData player1, PlayerData player2)
        {
            if (player1.Length > player2.Length)
            {
                WinInformationServerRpc(player1.Id, player2.Id);
            }
            else
            {
                WinInformationServerRpc(player2.Id, player1.Id);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void WinInformationServerRpc(ulong winner, ulong loser)
        {
            targetClientsArray[0] = winner;
            ClientRpcParams clientRpcParamsWinner = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = targetClientsArray }
            };
            AtePlayerClientRpc(clientRpcParamsWinner);

            targetClientsArray[0] = loser;
            ClientRpcParams clientRpcParamsLoser = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = targetClientsArray }
            };
            GameOverClientRpc(clientRpcParamsLoser);
        }

        [ClientRpc]
        private void AtePlayerClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;
            Debug.Log("You ate a player!");
        }

        [ClientRpc]
        private void GameOverClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;
            Debug.Log("You got eaten!");
            GameOverEvent?.Invoke();
            NetworkManager.Singleton.Shutdown();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            Debug.Log("Player Collision");
            if (!col.gameObject.CompareTag("Player")) return;
            if (!IsOwner) return;

            if (col.gameObject.TryGetComponent(out PlayerLength playerLength))
            {
                Debug.Log("head collision");
                var player1 = new PlayerData()
                {
                    Id = OwnerClientId,
                    Length = playersLength.length.Value
                };

                var player2 = new PlayerData()
                {
                    Id = playerLength.OwnerClientId,
                    Length = playerLength.length.Value
                };
                DeterminedCollisionWinnerServerRpc(player1, player2);
            }
            else if (col.gameObject.TryGetComponent(out Tail tail))
            {
                Debug.Log("tail collision");
                if (tail.networkedOwner != null && tail.networkedOwner.GetComponent<PlayerController>() != null)
                {
                    WinInformationServerRpc(tail.networkedOwner.GetComponent<PlayerController>().OwnerClientId, OwnerClientId);
                }
                else
                {
                    Debug.LogWarning("Tail owner is null or missing PlayerController component.");
                }
            }
        }

        struct PlayerData : INetworkSerializable
        {
            public ulong Id;
            public ushort Length;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Id);
                serializer.SerializeValue(ref Length);
            }
        }
    }
}
