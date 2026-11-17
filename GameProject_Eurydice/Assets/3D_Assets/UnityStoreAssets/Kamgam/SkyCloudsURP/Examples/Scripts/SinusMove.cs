using UnityEngine;

namespace Kamgam.SkyClouds
{
    public class SinusMove : MonoBehaviour
    {
        public float Speed = 1f;
        public float SinDelta = 1f;
        public Vector3 Axis = new Vector3(0f, 1f, 0f);

        [System.NonSerialized]
        Vector3 _initialPosition;

        [System.NonSerialized]
        float _angle = 0f;

        void Start()
        {
            _angle = 0f;
            _initialPosition = transform.localPosition;
        }

        void Update()
        {
            _angle += Speed * Time.deltaTime;
            _angle %= 360f;

            var multiplier = (Mathf.Sin(_angle * Mathf.Deg2Rad) + SinDelta) * 0.5f;
            transform.localPosition = _initialPosition + Axis * multiplier;
        }
    }
}
