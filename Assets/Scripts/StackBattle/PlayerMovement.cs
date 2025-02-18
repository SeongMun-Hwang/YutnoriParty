using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerMovement : NetworkBehaviour
{
	NetworkVariable<FixedString32Bytes> nickName = new NetworkVariable<FixedString32Bytes>();

	private bool w;
	private bool a;
	private bool s;
	private bool d;

	public override void OnNetworkSpawn()
	{
		if (!IsOwner) { return; }

		SendNickNameServerRpc($"Player {OwnerClientId}");
	}

	private void FixedUpdate()
	{
		if (!IsOwner) { return; }

		w = Input.GetKey(KeyCode.W);
		a = Input.GetKey(KeyCode.A);
		s = Input.GetKey(KeyCode.S);
		d = Input.GetKey(KeyCode.D);

		
		SendInputServerRpc(w, a, s, d);
	}

	[ServerRpc]
	public void SendInputServerRpc(bool w, bool a, bool s, bool d)
	{
		Vector3 input = Vector3.zero;

		if (w)
		{
			input += Vector3.forward;
		}
		if (a)
		{
			input += Vector3.left;
		}
		if (s)
		{
			input += Vector3.back;
		}
		if (d)
		{
			input += Vector3.right;
		}

		transform.Translate(input * 0.06f, Space.World);
	}

	[ServerRpc]
	public void SendNickNameServerRpc(string s)
	{
		nickName.Value = s;
	}
}
