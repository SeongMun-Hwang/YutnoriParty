using UnityEngine;

public class SpinSelf2D : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.back * 200f * Time.deltaTime);
    }
}
