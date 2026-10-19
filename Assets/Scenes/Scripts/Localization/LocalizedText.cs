using TMPro;
using UnityEngine;

// Вешается рядом с TextMeshPro: текст сам меняется при смене языка
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour {
    [TextArea] public string russian;
    [TextArea] public string english;

    TMP_Text text;

    void Awake() => text = GetComponent<TMP_Text>();

    void OnEnable() {
        Loc.Changed += Apply;
        Apply();
    }

    void OnDisable() => Loc.Changed -= Apply;

    public void Set(string ru, string en) {
        russian = ru;
        english = en;
        Apply();
    }

    void Apply() {
        if (text == null) text = GetComponent<TMP_Text>();
        text.text = Loc.T(russian, english);
    }
}
