using System;
using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR (entrada física). Zona invisible justo después de una puerta.
    /// Avisa a su RoomController cuando el jugador la cruza.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomExitTrigger : MonoBehaviour
    {
        public event Action PlayerEntered;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) PlayerEntered?.Invoke();
        }
    }
}
