using UnityEngine;

// Телефон: когда звонит — ЛКМ, чтобы ответить. Вешается сам из IntroSequence на трубку и аппарат.
public class PhoneInteractable : MonoBehaviour, IInteractable {
    IntroSequence story;

    public static void Attach(IntroSequence story, Transform target) {
        if (target == null) return;
        var p = target.GetComponent<PhoneInteractable>();
        if (p == null) p = target.gameObject.AddComponent<PhoneInteractable>();
        p.story = story;
        // Луч взгляда должен во что-то попасть
        if (target.GetComponent<Collider>() == null) target.gameObject.AddComponent<BoxCollider>();
    }

    // null — телефон молчит, подсказки нет и взаимодействовать нельзя
    public string PromptText => story != null && story.IsRinging ? Loc.T("ЛКМ — ответить", "LMB — answer") : null;

    public void Interact(Vector3 viewerPosition) {
        if (story != null) story.AnswerCall();
    }
}
