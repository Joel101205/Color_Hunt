using UnityEngine;

public class BlobBehaviour : MonoBehaviour
{
    public float speed = 0.5f;
    public float range = 30f;
    private Vector3 startPos;

    void Start() => startPos = transform.localPosition;

    void Update()
    {
        float x = Mathf.Sin(Time.time * speed) * range;
        float y = Mathf.Cos(Time.time * speed * 0.7f) * range;
        transform.localPosition = startPos + new Vector3(x, y, 0);
    }
}
