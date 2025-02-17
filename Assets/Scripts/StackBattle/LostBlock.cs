using UnityEngine;

public class LostBlock : MonoBehaviour
{
	private void Start()
	{
		Destroy(gameObject, 5f);
	}

	private void Update()
    {
		if (transform.position.y < -5f)
			Destroy(gameObject);
	}
}
