using Unity.Netcode;

public class CharacterInfo : NetworkBehaviour
{
    public NetworkVariable<bool> inIsland = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone);
    public int overlappedCount = 0;
    public NetworkVariable<bool> isReverse = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
}
