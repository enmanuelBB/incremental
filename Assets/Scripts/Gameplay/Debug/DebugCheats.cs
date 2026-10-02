using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Atajos de desarrollo. En una build final el Update queda vacío, así un jugador no puede
/// borrar su progreso ni regalarse dinero sin querer.
/// O = dinero, P = borrar progreso, N = saltar oleada, U = desbloquear todos los personajes.
/// </summary>
public class DebugCheats : MonoBehaviour
{
    [SerializeField] private int moneyAmount = 100;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.oKey.wasPressedThisFrame)
        {
            MoneyManager.Instance.AddMoney(moneyAmount);
            Debug.Log("[Debug] Dinero agregado: +" + moneyAmount);
        }

        if (kb.pKey.wasPressedThisFrame)
        {
            SaveSystem.Delete();
            Debug.Log("[Debug] Progreso borrado, reinicia la escena para verlo");
        }

        if (kb.nKey.wasPressedThisFrame)
        {
            WaveManager.Instance.SkipWave();
            Debug.Log("[Debug] Oleada saltada");
        }

        if (kb.uKey.wasPressedThisFrame)
        {
            CharacterManager.Instance.DebugUnlockAll();
            Debug.Log("[Debug] Personajes desbloqueados, reinicia la escena para verlos en el menú");
        }
    }
#endif
}
