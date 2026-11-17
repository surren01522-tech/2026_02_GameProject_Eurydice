using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.SkyClouds
{
    public class SkyCloud : MonoBehaviour
    {
        public float ObserverFadeStartDistance = 0.5f;
        public float ObserverFadeEndDistance = 0.1f;
        public Color ObserverFadeColor = Color.white;
        public float ObserverMaskScale = 1f;
        public float ObserverMaskPadding = 0.2f;

        public static List<SkyCloud> Clouds = new List<SkyCloud>();

        public static float GetMinDistance(Vector3 position, out Vector3 closestPosition, out SkyCloud closestCloud, float distanceLimit = 10f)
        {
            float distance = distanceLimit;
            Vector3 tmpClosestPosition = new Vector3(0f, -999_999f, 0f);
            closestPosition = tmpClosestPosition;
            closestCloud = null;
            foreach (var cloud in Clouds)
            {
                if (cloud == null || cloud.gameObject == null)
                    continue;

                float d = cloud.GetDistance(position, out tmpClosestPosition, distanceLimit);
                if (d < distance)
                {
                    distance = d;
                    closestPosition = tmpClosestPosition;
                    closestCloud = cloud;
                }
            }
            return distance;
        }

        private RaycastHit[] _hitsUp;

        protected Collider[] _colliders;
        public Collider[] Colliders
        {
            get
            {
                if (_colliders == null)
                {
                    _colliders = this.GetComponents<Collider>();
                }
                return _colliders;
            }
        }

        protected Material _couldMaterial;
        public Material CloudMaterial
        {
            get
            {
                if (_couldMaterial == null)
                {
                    var renderer = this.GetComponent<MeshRenderer>();
                    if (renderer != null)
                        _couldMaterial = renderer.sharedMaterial;
                }
                return _couldMaterial;
            }
        }

        public void OnEnable()
        {
            Clouds.Add(this);
        }

        public void OnDisable()
        {
            Clouds.Remove(this);
        }

        //Vector3 _closestPointForGizmo;

        public float GetDistance(Vector3 position, out Vector3 closestPosition, float distanceLimit = 10f)
        {
            closestPosition = transform.position;

            if (Colliders == null)
            {
                Debug.LogWarning("SkyCloud does not have a collider. No distance calculations can be done.", this.gameObject);
                return distanceLimit;
            }

            bool isInBounds = false;
            foreach (var collider in Colliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                var closestPoint = collider.ClosestPointOnBounds(position);
                var sqrDistance = Vector3.SqrMagnitude(closestPoint - position);
                if (sqrDistance < distanceLimit * distanceLimit)
                {
                    isInBounds = true;
                    break;
                }
            }

            float minDistance = distanceLimit;
            if (isInBounds)
            {
                foreach (var collider in Colliders)
                {
                    if (collider == null || !collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPointOnBounds(position);
                    var sqrDistance = Vector3.SqrMagnitude(closestPoint - position);
                    if (sqrDistance < distanceLimit * distanceLimit)
                    {
                        // Shortcut for when the observer is inside one of the colliders.
                        if (IsInsideMesh(position))
                        {
                            return 0f;
                        }

                        if (collider.isTrigger)
                        {
                            closestPoint = collider.ClosestPoint(position);
                            //_closestPointForGizmo = closestPoint;
                            float distance = Vector3.Magnitude(closestPoint - position);
                            if (distance < minDistance)
                            {
                                closestPosition = closestPoint;
                                minDistance = distance;
                            }
                        }
                    }
                }
            }

            return minDistance;
        }

        //private void OnDrawGizmos()
        //{
        //    Gizmos.DrawSphere(_closestPointForGizmo, 0.1f);
        //}

        public bool IsInsideMesh(Vector3 point)
        {
            Physics.queriesHitBackfaces = true;
            int hitsUp = Physics.RaycastNonAlloc(new Ray(point, Vector3.up), _hitsUp);
            Physics.queriesHitBackfaces = false;

            // If the hit normal y value is positive then this means we have hit a back face from the inside.
            for (var i = 0; i < hitsUp; i++)
                if (_hitsUp[i].normal.y > 0)
                    return true;

            // For planes we would have to another test but we skip this because we assume it's always a mesh collider with a volume > 0.

            return false;
        }

        public static bool CheckSphereIntersection(Collider targetCollider, SphereCollider sphereCollider, out Vector3 closestPoint, out Vector3 surfaceNormal)
        {
            closestPoint = Vector3.zero;
            surfaceNormal = Vector3.zero;
            float penetrationDepth = 0f;

            Vector3 sphere_pos = sphereCollider.transform.position;
            if (Physics.ComputePenetration(targetCollider, targetCollider.transform.position, targetCollider.transform.rotation, sphereCollider, sphere_pos, Quaternion.identity, out surfaceNormal, out penetrationDepth))
            {
                closestPoint = sphere_pos + (surfaceNormal * (sphereCollider.radius - penetrationDepth));
                surfaceNormal = -surfaceNormal;
                return true;
            }

            return false;
        }
    }
}
