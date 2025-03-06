using Unity.Netcode;

public class CharacterInfo : NetworkBehaviour
{
    public bool canMove = true;
    public int overlappedCount = 0;
}
