using UnityEngine;

public enum InteractionDisplayMode
{
    Floating,
    Fixed
}

public interface IInteractable
{
    string InteractionPrompt { get; }
    InteractionDisplayMode DisplayMode { get; }
    Vector3 WorldOffset { get; }
    Transform TargetTransform { get; }
    bool CanInteract { get; }

    void Interact(PlayerController player);
}
