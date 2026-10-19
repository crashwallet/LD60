using UnityEngine;

// Предмет, с которым можно взаимодействовать взглядом + ЛКМ (ящики, записки)
public interface IInteractable {
    // Подсказка вверху экрана, уже на нужном языке
    string PromptText { get; }
    void Interact(Vector3 viewerPosition);
}
