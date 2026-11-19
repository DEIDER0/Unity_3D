using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Cámara isométrica que sigue al jugador con suavizado.
    /// Es inclinada (≈68°) para que las paredes no tapen al jugador.
    /// </summary>
    public class CameraFollowView : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float pitch = 68f;
        [SerializeField] private float distance = 16f;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f);

        private Vector3 _velocity;
        private float _shakeTime;
        private float _shakeStrength;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        /// <summary>Sacudida breve (golpes fuertes del jefe).</summary>
        public void Shake(float strength, float duration)
        {
            _shakeStrength = strength;
            _shakeTime = duration;
        }

        private void Start() => SnapToTarget();

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = DesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);

            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                transform.position += Random.insideUnitSphere * _shakeStrength;
            }
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void SnapToTarget()
        {
            if (target == null) return;
            transform.position = DesiredPosition();
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private Vector3 DesiredPosition()
        {
            Vector3 offset = Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -distance);
            return target.position + lookOffset + offset;
        }
    }
}
