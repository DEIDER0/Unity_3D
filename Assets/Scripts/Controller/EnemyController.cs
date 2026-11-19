using Dungeon.Data;
using Dungeon.Model;
using Dungeon.View;
using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. IA básica de un enemigo: espera → persigue al jugador → prepara el golpe →
    /// golpea → se recupera. La vida y el daño están en EnemyState (Modelo);
    /// las animaciones en CharacterView y la barra en EnemyHealthBarView (Vistas).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        protected enum Phase { Idle, Chase, Windup, Special, Recover, Dead }

        [SerializeField] protected CharacterView view;
        [SerializeField] protected EnemyHealthBarView healthBar;
        [Tooltip("Ángulo máximo (grados) frente al enemigo en el que su golpe alcanza al jugador.")]
        [SerializeField] protected float hitAngle = 75f;
        [SerializeField] protected float knockbackSpeed = 6f;

        public EnemyState State { get; private set; }
        public bool IsAlive => State != null && !State.IsDead;
        public Vector3 Position => transform.position;

        protected Rigidbody Body { get; private set; }
        protected PlayerController Target { get; private set; }
        protected Phase CurrentPhase { get; set; } = Phase.Idle;
        protected float PhaseEndTime { get; set; }

        private EnemyConfig _config;
        private Cooldown _attackCooldown;
        private float _knockbackUntil;
        private bool _active;

        // Estadísticas: el jefe las sobrescribe con los valores de BossConfig.
        protected virtual float MoveSpeed => _config.moveSpeed;
        protected virtual float DetectionRange => _config.detectionRange;
        protected virtual float AttackRange => _config.attackRange;
        protected virtual float AttackWindup => _config.attackWindup;
        protected virtual float AttackCooldown => _config.attackCooldown;
        protected virtual float KnockbackMultiplier => 1f;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>Lo llama RoomController al iniciar el nivel.</summary>
        public void Init(EnemyState state, EnemyConfig config)
        {
            _config = config;
            Setup(state);
        }

        protected void Setup(EnemyState state)
        {
            if (State != null) State.Died -= OnDied;
            State = state;
            State.Died += OnDied;
            _attackCooldown = new Cooldown(AttackCooldown);
            if (healthBar != null) healthBar.Bind(state);
        }

        /// <summary>El jugador entró a la sala: empieza a vigilarlo.</summary>
        public virtual void Activate(PlayerController player)
        {
            Target = player;
            _active = true;
        }

        protected virtual void OnDestroy()
        {
            if (State != null) State.Died -= OnDied;
        }

        private void FixedUpdate()
        {
            if (!_active || !IsAlive || Target == null)
            {
                if (CurrentPhase != Phase.Dead) Stop();
                return;
            }
            if (Time.time < _knockbackUntil) return;

            Vector3 toTarget = Target.Position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            double now = Time.timeAsDouble;

            switch (CurrentPhase)
            {
                case Phase.Idle:
                    Stop();
                    if (Target.IsAlive && distance <= DetectionRange) CurrentPhase = Phase.Chase;
                    break;

                case Phase.Chase:
                    if (!Target.IsAlive) { Stop(); break; }
                    Face(toTarget);
                    if (TryStartSpecial(distance)) break;
                    if (distance <= AttackRange && _attackCooldown.IsReady(now))
                    {
                        StartWindup();
                        break;
                    }
                    if (distance > AttackRange * 0.8f) MoveTowards(toTarget / Mathf.Max(distance, 0.001f));
                    else Stop();
                    break;

                case Phase.Windup:
                    Stop();
                    Face(toTarget);
                    if (Time.time >= PhaseEndTime)
                    {
                        Strike(toTarget, distance);
                        EnterRecover(0.4f);
                    }
                    break;

                case Phase.Special:
                    Stop();
                    UpdateSpecial(distance);
                    break;

                case Phase.Recover:
                    Stop();
                    if (Time.time >= PhaseEndTime) CurrentPhase = Phase.Chase;
                    break;
            }

            if (view != null) view.SetMoveAmount(CurrentPhase == Phase.Chase ? 1f : 0f);
        }

        // Ganchos para el ataque especial del jefe (el enemigo básico no tiene).
        protected virtual bool TryStartSpecial(float distance) => false;
        protected virtual void UpdateSpecial(float distance) { }

        protected void EnterRecover(float duration)
        {
            CurrentPhase = Phase.Recover;
            PhaseEndTime = Time.time + duration;
        }

        private void StartWindup()
        {
            _attackCooldown.TryUse(Time.timeAsDouble);
            CurrentPhase = Phase.Windup;
            PhaseEndTime = Time.time + AttackWindup;
            if (view != null) view.PlayAttack(AttackWindup, 0.15f);
        }

        private void Strike(Vector3 toTarget, float distance)
        {
            if (!Target.IsAlive || distance > AttackRange + 0.6f) return;
            if (distance > 0.3f && Vector3.Angle(transform.forward, toTarget) > hitAngle) return;
            Target.ReceiveDamage(State.Damage, transform.position);
        }

        protected void MoveTowards(Vector3 direction) =>
            PhysicsCompat.SetHorizontalVelocity(Body, direction * MoveSpeed);

        protected void Stop() => PhysicsCompat.SetHorizontalVelocity(Body, Vector3.zero);

        protected void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, target, 540f * Time.fixedDeltaTime));
        }

        // ------------------------------------------------------------------
        public int ReceiveDamage(int amount, Vector3 sourcePosition)
        {
            if (!IsAlive) return 0;
            int applied = State.TakeDamage(amount);
            if (applied <= 0) return 0;

            if (IsAlive)
            {
                if (view != null) view.PlayHit();
                if (CurrentPhase == Phase.Idle) CurrentPhase = Phase.Chase; // si lo golpean, despierta

                Vector3 push = transform.position - sourcePosition;
                push.y = 0f;
                if (push.sqrMagnitude > 0.001f && KnockbackMultiplier > 0f)
                {
                    PhysicsCompat.SetHorizontalVelocity(Body, push.normalized * knockbackSpeed * KnockbackMultiplier);
                    _knockbackUntil = Time.time + 0.15f;
                }
            }
            return applied;
        }

        protected virtual void OnDied()
        {
            CurrentPhase = Phase.Dead;
            Stop();
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Body.isKinematic = true;
            if (view != null)
            {
                view.SetMoveAmount(0f);
                view.PlayDeath(true);
            }
            Destroy(gameObject, 3f);
        }
    }
}
