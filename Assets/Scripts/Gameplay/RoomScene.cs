using UnityEngine;

namespace EquationShift
{
    // Runs before room actors and URP lights enable during additive loading.
    [DefaultExecutionOrder(-32000), DisallowMultipleComponent]
    public sealed class RoomScene : MonoBehaviour
    {
        private void Awake() => SceneTransition.Controller?.PrepareDestination(gameObject.scene);
    }
}
