using Dungeon.Model;
using Dungeon.View;
using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. Detecta cuando el jugador está cerca del cofre (trigger) y, cuando pulsa E
    /// (PlayerController lee la tecla), abre el ChestModel y entrega el ítem al PlayerState.
    /// Que solo se abra una vez lo decide el Modelo; la animación es de ChestView.
    /// </summary>
    public class ChestController : MonoBehaviour, IInteractable
    {
        [SerializeField] private ChestView view;

        public ChestModel Model { get; private set; }
        public bool CanInteract => Model != null && !Model.IsOpened;

        public void Init(ChestModel model)
        {
            Model = model;
            if (view != null) view.Bind(model);
        }

        public void Interact(PlayerController player)
        {
            if (Model == null || !Model.TryOpen(out Item item)) return;
            player.State.AddItem(item); // RF2: el jugador recibe el ítem y el HUD se actualiza por evento
            player.ClearInteractable(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !CanInteract) return;
            player.SetInteractable(this);
            if (view != null) view.SetPromptVisible(true);
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            player.ClearInteractable(this);
            if (view != null) view.SetPromptVisible(false);
        }
    }
}
