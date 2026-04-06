using UnityEngine;

public interface I_Interactable
{
    Transform transform { get; }

    string DisplayName { get; }

    bool canInteract();

    void Interact();

    void OnFocusGained();

    void OnFocusLost();
}
