using System.Collections;
using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Puerta abierta/cerrada. Al abrirse, la hoja tiembla y se hunde bajo el piso.
    /// No decide CUÁNDO abrirse: eso lo decide el Modelo (LevelState) y lo ordena RoomController.
    /// </summary>
    public class DoorView : MonoBehaviour
    {
        [SerializeField] private Transform doorLeaf;
        [SerializeField] private float openDepth = 2.9f;
        [SerializeField] private float openDuration = 1.3f;

        public bool IsOpen { get; private set; }

        private Vector3 _closedPosition;

        private void Awake()
        {
            if (doorLeaf == null) doorLeaf = transform;
            _closedPosition = doorLeaf.localPosition;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            StartCoroutine(OpenRoutine());
            SfxPlayer.Play(Sfx.DoorOpen);
        }

        private IEnumerator OpenRoutine()
        {
            // Temblor breve antes de bajar
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                doorLeaf.localPosition = _closedPosition + new Vector3(Mathf.Sin(t * 90f) * 0.03f, 0f, 0f);
                yield return null;
            }

            t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / openDuration);
                doorLeaf.localPosition = _closedPosition + Vector3.down * openDepth * k
                                         + new Vector3(Mathf.Sin(t * 60f) * 0.015f, 0f, 0f);
                yield return null;
            }
            doorLeaf.localPosition = _closedPosition + Vector3.down * openDepth;
            doorLeaf.gameObject.SetActive(false);
        }
    }
}
