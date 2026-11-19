using System.Collections;
using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Animaciones procedurales de un personaje (jugador, enemigo o jefe):
    /// caminar (balanceo), golpe con el arma, esquive (voltereta), destello al recibir daño,
    /// furia y muerte. No sabe nada de reglas: el controlador le dice qué mostrar.
    /// Los modelos no tienen esqueleto, por eso se animan moviendo pivotes por código.
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("Pivote del cuerpo (a media altura). Se usa para el balanceo, la voltereta y la caída.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Pivote del arma (en la mano). Se rota para el golpe.")]
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField] private float bobFrequency = 9f;
        [Tooltip("Ángulo (grados) del arma al prepararse y al terminar el golpe.")]
        [SerializeField] private float swingStartYaw = 55f;
        [SerializeField] private float swingEndYaw = -100f;
        [SerializeField] private Sfx attackSound = Sfx.Swing;
        [SerializeField] private Sfx deathSound = Sfx.EnemyDeath;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Vector3 _visualRestPosition;
        private Quaternion _visualRestRotation;
        private Vector3 _visualRestScale;
        private Quaternion _weaponRest = Quaternion.identity;
        private float _moveAmount;
        private float _bobTime;
        private bool _busyBody;   // voltereta o muerte en curso: no aplicar balanceo
        private bool _dead;
        private Coroutine _weaponRoutine;
        private Coroutine _flashRoutine;
        private Coroutine _bodyRoutine;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            _visualRestPosition = visualRoot.localPosition;
            _visualRestRotation = visualRoot.localRotation;
            _visualRestScale = visualRoot.localScale;
            if (weaponPivot != null) _weaponRest = weaponPivot.localRotation;
        }

        private void Update()
        {
            if (_dead || _busyBody || visualRoot == transform) return;

            _bobTime += Time.deltaTime * bobFrequency * _moveAmount;
            float bob = Mathf.Abs(Mathf.Sin(_bobTime)) * bobHeight * _moveAmount;
            float lean = 6f * _moveAmount;
            visualRoot.localPosition = _visualRestPosition + Vector3.up * bob;
            visualRoot.localRotation = _visualRestRotation * Quaternion.Euler(lean, 0f, Mathf.Sin(_bobTime) * 3f * _moveAmount);
        }

        /// <summary>0 = quieto, 1 = velocidad máxima.</summary>
        public void SetMoveAmount(float normalized) => _moveAmount = Mathf.Clamp01(normalized);

        /// <summary>Prepara el arma durante 'windup' segundos y luego da el golpe en 'swing' segundos.</summary>
        public void PlayAttack(float windup, float swing)
        {
            if (_dead || weaponPivot == null) return;
            if (_weaponRoutine != null) StopCoroutine(_weaponRoutine);
            _weaponRoutine = StartCoroutine(AttackRoutine(windup, swing));
        }

        public void PlayDodge(float duration)
        {
            if (_dead || visualRoot == transform) return;
            if (_bodyRoutine != null) StopCoroutine(_bodyRoutine);
            _bodyRoutine = StartCoroutine(RollRoutine(duration));
            SfxPlayer.Play(Sfx.Dodge, 0.6f);
        }

        public void PlayHit()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(hitColor, 0.12f));
            SfxPlayer.Play(Sfx.Hit);
        }

        /// <summary>Efecto de furia del jefe: crece un poco y destella.</summary>
        public void PlayEnrage()
        {
            if (_dead) return;
            _visualRestScale *= 1.15f;
            StartCoroutine(ScaleRoutine(visualRoot.localScale, _visualRestScale, 0.4f));
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(new Color(1f, 0.1f, 0.05f), 0.5f));
        }

        /// <param name="sink">Si es true, después de caer se hunde en el piso (enemigos).</param>
        public void PlayDeath(bool sink)
        {
            if (_dead) return;
            _dead = true;
            if (_bodyRoutine != null) StopCoroutine(_bodyRoutine);
            if (_weaponRoutine != null) StopCoroutine(_weaponRoutine);
            _bodyRoutine = StartCoroutine(DeathRoutine(sink));
            SfxPlayer.Play(deathSound);
        }

        // ------------------------------------------------------------------
        private IEnumerator AttackRoutine(float windup, float swing)
        {
            float t = 0f;
            Quaternion rest = _weaponRest;
            Quaternion start = _weaponRest * Quaternion.Euler(-15f, swingStartYaw, 0f);
            Quaternion end = _weaponRest * Quaternion.Euler(10f, swingEndYaw, 0f);

            while (t < windup)
            {
                t += Time.deltaTime;
                weaponPivot.localRotation = Quaternion.Slerp(rest, start, EaseOut(t / windup));
                yield return null;
            }

            SfxPlayer.Play(attackSound, 0.7f);
            t = 0f;
            while (t < swing)
            {
                t += Time.deltaTime;
                weaponPivot.localRotation = Quaternion.Slerp(start, end, EaseOut(t / swing));
                yield return null;
            }

            t = 0f;
            const float back = 0.18f;
            while (t < back)
            {
                t += Time.deltaTime;
                weaponPivot.localRotation = Quaternion.Slerp(end, rest, t / back);
                yield return null;
            }
            weaponPivot.localRotation = rest;
            _weaponRoutine = null;
        }

        private IEnumerator RollRoutine(float duration)
        {
            _busyBody = true;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float angle = 360f * EaseInOut(Mathf.Clamp01(t / duration));
                visualRoot.localRotation = _visualRestRotation * Quaternion.Euler(angle, 0f, 0f);
                visualRoot.localPosition = _visualRestPosition + Vector3.down * 0.25f * Mathf.Sin(Mathf.PI * t / duration);
                yield return null;
            }
            visualRoot.localRotation = _visualRestRotation;
            visualRoot.localPosition = _visualRestPosition;
            _busyBody = false;
            _bodyRoutine = null;
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            SetTint(color);
            yield return new WaitForSeconds(duration);
            ClearTint();
            _flashRoutine = null;
        }

        private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                visualRoot.localScale = Vector3.Lerp(from, to, EaseOut(t / duration));
                yield return null;
            }
            visualRoot.localScale = to;
        }

        private IEnumerator DeathRoutine(bool sink)
        {
            Quaternion from = visualRoot.localRotation;
            Quaternion to = _visualRestRotation * Quaternion.Euler(-85f, 0f, 0f);
            Vector3 startPos = visualRoot.localPosition;
            Vector3 groundPos = _visualRestPosition * 0.35f;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = EaseIn(t / 0.45f);
                visualRoot.localRotation = Quaternion.Slerp(from, to, k);
                visualRoot.localPosition = Vector3.Lerp(startPos, groundPos, k);
                yield return null;
            }
            if (!sink) yield break;

            yield return new WaitForSeconds(0.6f);
            t = 0f;
            Vector3 down = groundPos + Vector3.down * 1.5f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                visualRoot.localPosition = Vector3.Lerp(groundPos, down, t / 1.2f);
                yield return null;
            }
            visualRoot.gameObject.SetActive(false);
        }

        private void SetTint(Color color)
        {
            _block.Clear();
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            foreach (var r in _renderers)
                if (r != null) r.SetPropertyBlock(_block);
        }

        private void ClearTint()
        {
            foreach (var r in _renderers)
                if (r != null) r.SetPropertyBlock(null);
        }

        private static float EaseOut(float x) => 1f - (1f - Mathf.Clamp01(x)) * (1f - Mathf.Clamp01(x));
        private static float EaseIn(float x) => Mathf.Clamp01(x) * Mathf.Clamp01(x);
        private static float EaseInOut(float x) => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
    }
}
