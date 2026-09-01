using UnityEngine;

public class SkeleDance : MonoBehaviour
{
    public float height = 2f;
    public float speed = 2f;

    private Vector3 startPosition;
    private float randomOffset;

    void Start()
    {
        startPosition = transform.position;
        randomOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        float noise =
            Mathf.PerlinNoise(
                randomOffset,
                Time.time * speed
            );
        float yOffset =
            Mathf.Lerp(
                -height,
                height,
                noise
            );
        transform.position =
            startPosition +
            Vector3.up * yOffset;
    }
}
