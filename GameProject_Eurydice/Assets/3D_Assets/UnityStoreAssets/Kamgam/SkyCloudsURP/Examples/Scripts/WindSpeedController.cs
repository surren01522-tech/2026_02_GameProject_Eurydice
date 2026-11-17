using UnityEngine;

namespace Kamgam.SkyClouds
{
    public class WindSpeedController : MonoBehaviour
    {
        public float MaxDistance = 10f;
        public float MaxSpeed = 100f;
        public Material[] Materials;

        private Vector3 _initialPosition;
        private bool _isDragging = false;
        private Vector3 _offset;

        void Start()
        {
            _initialPosition = transform.position;
        }

        void Update()
        {
            if (InputUtils.WasLeftMouseButtonPressed())
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(InputUtils.MousePosition());

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        _isDragging = true;
                        _offset = transform.position - hit.point;
                    }
                }
            }

            if (InputUtils.WasLeftMouseButtonReleased())
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                Ray ray = Camera.main.ScreenPointToRay(InputUtils.MousePosition());
                Plane dragPlane = new Plane(Vector3.up, _initialPosition);

                if (dragPlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance) + _offset;
                    float clampedX = Mathf.Clamp(hitPoint.x, _initialPosition.x, _initialPosition.x + MaxDistance);
                    transform.position = new Vector3(clampedX, _initialPosition.y, _initialPosition.z);
                }
            }

            foreach (var material in Materials)
            {
                if (material == null)
                    continue;

                material.SetFloat("_WindSpeed", GetNormalizedValue() * MaxSpeed);
            }
        }

        private float GetNormalizedValue()
        {
            float distanceMoved = transform.position.x - _initialPosition.x;
            return Mathf.InverseLerp(0, MaxDistance, distanceMoved);
        }
    }
}
