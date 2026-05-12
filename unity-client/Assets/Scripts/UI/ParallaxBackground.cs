using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public SpriteRenderer sprite;
        public float scrollSpeed = -1f;
        public float maxOffset = 3f; // Distance max avant demi-tour
    }

    [SerializeField] private ParallaxLayer[] layers;

    private Vector3[] _startPositions;
    private float[] _directions;

    private void Start()
    {
        _startPositions = new Vector3[layers.Length];
        _directions = new float[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].sprite == null) continue;
            _startPositions[i] = layers[i].sprite.transform.position;
            _directions[i] = 1f;
        }
    }

    private void Update()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].sprite == null) continue;

            var t = layers[i].sprite.transform;
            float dist = t.position.x - _startPositions[i].x;

            // Demi-tour si on atteint la limite
            if (dist > layers[i].maxOffset) _directions[i] = -1f;
            if (dist < -layers[i].maxOffset) _directions[i] = 1f;

            t.position += new Vector3(
                layers[i].scrollSpeed * _directions[i] * Time.deltaTime, 0f, 0f);
        }
    }
}