using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR (utilidad). En Unity 6, Rigidbody.velocity pasó a llamarse linearVelocity.
    /// Este helper usa la propiedad correcta según la versión.
    /// </summary>
    public static class PhysicsCompat
    {
        public static Vector3 GetVelocity(Rigidbody body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        public static void SetVelocity(Rigidbody body, Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }

        /// <summary>Cambia solo la velocidad horizontal y conserva la vertical (gravedad).</summary>
        public static void SetHorizontalVelocity(Rigidbody body, Vector3 horizontal)
        {
            Vector3 current = GetVelocity(body);
            SetVelocity(body, new Vector3(horizontal.x, current.y, horizontal.z));
        }
    }
}
