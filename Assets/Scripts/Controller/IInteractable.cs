namespace Dungeon.Controller
{
    /// <summary>CONTROLADOR (contrato). Algo con lo que el jugador interactúa pulsando E (el cofre).</summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        void Interact(PlayerController player);
    }
}
