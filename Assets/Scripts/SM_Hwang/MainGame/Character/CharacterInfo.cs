using Unity.Netcode;

public class CharacterInfo : NetworkBehaviour
{
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(true);
    public int overlappedCount = 0;
}
