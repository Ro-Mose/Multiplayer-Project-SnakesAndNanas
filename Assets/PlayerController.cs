using System;
using Unity.Netcode;
using UnityEngine;

namespace SnakesAndNanas
{
    public class PlayerController : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

        [SerializeField] private float speed = 3f; // Adjusted speed
        private Vector2 movementInput;
        private PlayerLength playerLength;

        private readonly ulong[] targetClientsArray = new ulong[1];

        public static event System.Action GameOverEvent;

        private void Initialize()
        {
            playerLength = GetComponent<PlayerLength>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Position.Value = transform.position; // Ensure correct initial sync
            }

            Position.OnValueChanged += OnStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            Position.OnValueChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (!IsOwner || !Application.isFocused) return;

            movementInput.x = Input.GetAxisRaw("Horizontal");
            movementInput.y = Input.GetAxisRaw("Vertical");

            Vector2 moveDirection = movementInput.normalized;

            if (moveDirection != Vector2.zero)
            {
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

            Vector3 newPosition = player.transform.position + (Vector3)(moveDirection * speed * Time.deltaTime);
            player.Position.Value = newPosition;

            player.Move(newPosition, moveDirection);
        }

        private void Move(Vector3 newPosition, Vector2 moveDirection)
        {
            transform.position = newPosition;

            if (moveDirection != Vector2.zero)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            }
        }

        private void OnStateChanged(Vector3 previous, Vector3 current)
        {
            if (!IsOwner)
            {
                transform.position = Position.Value;
            }
        }

        private PlayerController GetPlayerByClientId(ulong clientId)
        {
            foreach (var obj in FindObjectsOfType<PlayerController>())
            {
                if (obj.OwnerClientId == clientId) return obj;
            }
            return null;
        }

        [ServerRpc]
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

        [ServerRpc]
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
                    Length = this.playerLength?.length.Value ?? 0
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
