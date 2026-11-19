using System.Collections;
using System.Collections.Generic;
using Dungeon.Data;
using Dungeon.Model;
using Dungeon.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. Lee la entrada del jugador (Input System), lo mueve con el Rigidbody,
    /// ejecuta el ataque y el esquive, e interactúa con el cofre.
    /// Las reglas (daño, vida, invulnerabilidad) están en PlayerState; las animaciones en CharacterView.
    ///
    /// Controles: WASD/flechas = mover · Mouse = apuntar · Clic izq./Espacio = atacar ·
    ///            Shift = esquivar · E = abrir cofre.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private CharacterView view;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Capas que puede golpear el ataque.")]
        [SerializeField] private LayerMask hitMask = ~0;
        [Tooltip("Segundos que el jugador sigue apuntando al mouse después de moverlo.")]
        [SerializeField] private float mouseAimMemory = 2f;

        public PlayerState State { get; private set; }
        public bool IsAlive => State != null && !State.IsDead;
        public Vector3 Position => transform.position;

        private readonly Collider[] _hits = new Collider[16];
        private readonly HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();

        private Rigidbody _body;
        private InputActionMap _map;
        private InputAction _move, _look, _attack, _dodge, _interact;
        private Cooldown _attackCooldown, _dodgeCooldown;

        private Vector2 _moveInput;
        private Vector3 _aimDirection = Vector3.forward;
        private Vector2 _lastMousePosition;
        private float _lastMouseMoveTime = -100f;
        private bool _inputEnabled = true;
        private bool _isDodging;
        private float _dodgeEndTime;
        private Vector3 _dodgeDirection;
        private float _knockbackUntil;
        private IInteractable _interactable;

        private void Awake()
        {
            config = PlayerConfig.OrDefault(config, this);
            _body = GetComponent<Rigidbody>();
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (viewCamera == null) viewCamera = Camera.main;

            _map = inputActions.FindActionMap("Player", true);
            _move = _map.FindAction("Move", true);
            _look = _map.FindAction("Look", true);
            _attack = _map.FindAction("Attack", true);
            _dodge = _map.FindAction("Dodge", true);
            _interact = _map.FindAction("Interact", true);
        }

        private void OnEnable() => _map?.Enable();

        private IEnumerator Start()
        {
            // Diagnóstico: si a los 2 s nadie inicializó al jugador, el GameController falló.
            yield return new WaitForSeconds(2f);
            if (State == null)
                Debug.LogError("[Dungeon] El jugador no fue inicializado (por eso no se mueve). " +
                               "Revisa el error anterior del GameController.", this);
        }
        private void OnDisable() => _map?.Disable();

        private void OnDestroy()
        {
            if (State == null) return;
            State.Died -= OnDied;
            State.SetInvulnerable(false); // el estado sobrevive a la escena: no dejarlo invulnerable
        }

        /// <summary>Lo llama GameController al cargar el nivel. El estado viene de la partida (GameSession).</summary>
        public void Init(PlayerState state)
        {
            if (State != null) State.Died -= OnDied;
            State = state;
            State.SetInvulnerable(false);
            _attackCooldown = new Cooldown(config.attackCooldown);
            _dodgeCooldown = new Cooldown(config.dodgeCooldown);
            State.Died += OnDied;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled) _moveInput = Vector2.zero;
        }

        public void SetInteractable(IInteractable interactable) => _interactable = interactable;

        public void ClearInteractable(IInteractable interactable)
        {
            if (_interactable == interactable) _interactable = null;
        }

        // ------------------------------------------------------------------
        // Entrada
        // ------------------------------------------------------------------
        private void Update()
        {
            if (!IsAlive || !_inputEnabled)
            {
                _moveInput = Vector2.zero;
                if (view != null) view.SetMoveAmount(0f);
                return;
            }

            _moveInput = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);

            Vector2 mouse = _look.ReadValue<Vector2>();
            if ((mouse - _lastMousePosition).sqrMagnitude > 4f)
            {
                _lastMousePosition = mouse;
                _lastMouseMoveTime = Time.time;
            }

            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            UpdateAim(useMouse: clicked || Time.time - _lastMouseMoveTime < mouseAimMemory);

            if (_attack.WasPressedThisFrame()) TryAttack();
            if (_dodge.WasPressedThisFrame()) TryDodge();
            if (_interact.WasPressedThisFrame()) TryInteract();

            if (_isDodging && Time.time >= _dodgeEndTime) EndDodge();
            if (view != null) view.SetMoveAmount(_isDodging ? 0f : _moveInput.magnitude);
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                PhysicsCompat.SetHorizontalVelocity(_body, Vector3.zero);
                return;
            }
            if (Time.time < _knockbackUntil) return;

            Vector3 velocity = _isDodging ? _dodgeDirection * config.dodgeSpeed : MoveDirection() * config.moveSpeed;
            PhysicsCompat.SetHorizontalVelocity(_body, velocity);

            Vector3 face = _isDodging ? _dodgeDirection : _aimDirection;
            if (face.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(face, Vector3.up);
                _body.MoveRotation(Quaternion.RotateTowards(_body.rotation, target, config.turnSpeed * Time.fixedDeltaTime));
            }
        }

        /// <summary>Dirección de movimiento en el mundo, relativa a la cámara.</summary>
        private Vector3 MoveDirection()
        {
            if (_moveInput.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 forward = Vector3.forward, right = Vector3.right;
            if (viewCamera != null)
            {
                forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized;
            }
            return Vector3.ClampMagnitude(forward * _moveInput.y + right * _moveInput.x, 1f);
        }

        private void UpdateAim(bool useMouse)
        {
            if (useMouse && viewCamera != null)
            {
                Ray ray = viewCamera.ScreenPointToRay(_lastMousePosition);
                var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y + 1f, 0f));
                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 direction = ray.GetPoint(distance) - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.04f) _aimDirection = direction.normalized;
                    return;
                }
            }

            Vector3 move = MoveDirection();
            if (move.sqrMagnitude > 0.01f) _aimDirection = move.normalized;
        }

        // ------------------------------------------------------------------
        // Acciones
        // ------------------------------------------------------------------
        private void TryAttack()
        {
            if (_isDodging || !_attackCooldown.TryUse(Time.timeAsDouble)) return;

            // Gira al instante hacia donde apunta, para que el golpe vaya donde el jugador espera.
            Quaternion facing = Quaternion.LookRotation(_aimDirection, Vector3.up);
            _body.rotation = facing;
            transform.rotation = facing;

            if (view != null) view.PlayAttack(config.attackHitDelay, 0.12f);
            StartCoroutine(HitAfterDelay(config.attackHitDelay));
        }

        private IEnumerator HitAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!IsAlive) yield break;

            Vector3 center = transform.position + Vector3.up + _aimDirection * config.attackRange;
            int count = Physics.OverlapSphereNonAlloc(center, config.attackRadius, _hits, hitMask, QueryTriggerInteraction.Ignore);

            _alreadyHit.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = _hits[i].GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, this) || !target.IsAlive || !_alreadyHit.Add(target)) continue;
                target.ReceiveDamage(State.Damage, transform.position);
            }
        }

        private void TryDodge()
        {
            if (_isDodging || !_dodgeCooldown.TryUse(Time.timeAsDouble)) return;

            Vector3 move = MoveDirection();
            _dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : _aimDirection;
            _isDodging = true;
            _dodgeEndTime = Time.time + config.dodgeDuration;
            State.SetInvulnerable(true);
            if (view != null) view.PlayDodge(config.dodgeDuration);
        }

        private void EndDodge()
        {
            _isDodging = false;
            State.SetInvulnerable(false);
        }

        private void TryInteract()
        {
            if (_interactable != null && _interactable.CanInteract) _interactable.Interact(this);
        }

        // ------------------------------------------------------------------
        // Recibir daño
        // ------------------------------------------------------------------
        public int ReceiveDamage(int amount, Vector3 sourcePosition)
        {
            if (!IsAlive) return 0;
            int applied = State.TakeDamage(amount);
            if (applied > 0 && IsAlive)
            {
                if (view != null) view.PlayHit();
                Vector3 push = transform.position - sourcePosition;
                push.y = 0f;
                if (push.sqrMagnitude > 0.001f)
                {
                    PhysicsCompat.SetHorizontalVelocity(_body, push.normalized * 7f);
                    _knockbackUntil = Time.time + 0.12f;
                }
            }
            return applied;
        }

        private void OnDied()
        {
            _isDodging = false;
            _moveInput = Vector2.zero;
            PhysicsCompat.SetHorizontalVelocity(_body, Vector3.zero);
            if (view != null) view.PlayDeath(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * config.attackRange, config.attackRadius);
        }
    }
}
