using Dungeon.Data;
using Dungeon.Model;
using Dungeon.View;
using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. Jefe: reutiliza la IA del enemigo y agrega
    ///  - un golpe de área que se anuncia con un círculo rojo (TelegraphView) y se puede esquivar,
    ///  - una fase de furia (la decide BossState) que lo hace más rápido.
    /// </summary>
    public class BossController : EnemyController
    {
        [SerializeField] private TelegraphView telegraph;
        [SerializeField] private CameraFollowView cameraShake;

        private BossConfig _config;
        private Cooldown _slamCooldown;
        private float _speedMultiplier = 1f;

        public BossState BossState { get; private set; }

        protected override float MoveSpeed => _config.moveSpeed * _speedMultiplier;
        protected override float DetectionRange => 100f; // en su sala siempre ve al jugador
        protected override float AttackRange => _config.attackRange;
        protected override float AttackWindup => _config.attackWindup / _speedMultiplier;
        protected override float AttackCooldown => _config.attackCooldown;
        protected override float KnockbackMultiplier => 0f; // el jefe no retrocede con los golpes

        public void Init(BossState state, BossConfig config, CameraFollowView cameraView)
        {
            _config = config;
            cameraShake = cameraView;
            BossState = state;
            _slamCooldown = new Cooldown(config.slamCooldown);
            Setup(state);
            state.Enraged += OnEnraged;
        }

        public override void Activate(PlayerController player)
        {
            base.Activate(player);
            CurrentPhase = Phase.Chase;
            // Primer golpe de área un poco después de entrar, no al instante.
            _slamCooldown.TryUse(Time.timeAsDouble - _config.slamCooldown + 3.0);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (BossState != null) BossState.Enraged -= OnEnraged;
        }

        protected override bool TryStartSpecial(float distance)
        {
            if (distance > _config.slamRadius * 1.3f || !_slamCooldown.TryUse(Time.timeAsDouble)) return false;

            CurrentPhase = Phase.Special;
            float windup = _config.slamWindup / _speedMultiplier;
            PhaseEndTime = Time.time + windup;
            if (telegraph != null) telegraph.Show(_config.slamRadius, windup);
            if (view != null) view.PlayAttack(windup, 0.2f);
            return true;
        }

        protected override void UpdateSpecial(float distance)
        {
            if (Time.time < PhaseEndTime) return;

            if (telegraph != null) telegraph.Hide(impact: true);
            if (cameraShake != null) cameraShake.Shake(0.25f, 0.35f);
            if (Target.IsAlive && distance <= _config.slamRadius)
                Target.ReceiveDamage(BossState.SpecialDamage, transform.position);

            EnterRecover(0.9f / _speedMultiplier);
        }

        private void OnEnraged()
        {
            _speedMultiplier = _config.enragedSpeedMultiplier;
            if (view != null) view.PlayEnrage();
        }

        protected override void OnDied()
        {
            if (telegraph != null) telegraph.Hide(impact: false);
            CurrentPhase = Phase.Dead;
            Stop();
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Body.isKinematic = true;
            if (view != null)
            {
                view.SetMoveAmount(0f);
                view.PlayDeath(true);
            }
            if (cameraShake != null) cameraShake.Shake(0.3f, 0.6f);
            // El jefe no se destruye: su cuerpo queda en la sala hasta cambiar de escena.
        }
    }
}
