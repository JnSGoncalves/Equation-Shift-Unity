using UnityEngine;

namespace EquationShift
{
    [DisallowMultipleComponent]
    public sealed class VillageNpc : MonoBehaviour
    {
        [SerializeField, TextArea] private string dialogue;
        [SerializeField, TextArea] private string aftermathDialogue;

        internal string GetDialogue(bool aftermath) =>
            aftermath && !string.IsNullOrWhiteSpace(aftermathDialogue) ? aftermathDialogue : dialogue;
    }
}
