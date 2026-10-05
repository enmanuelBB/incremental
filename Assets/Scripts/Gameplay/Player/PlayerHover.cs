using UnityEngine;

/// <summary>
/// Levitar de Frieren: guarda la altura (con balanceo suave) que sube el cuerpo y la cámara. Solo visual: no toca el collider
/// ni el movimiento. Shooting la crea y la configura con el personaje; PlayerBody y CameraFollow leen 'Offset'.
/// Usa tiempo sin escala para que el balanceo siga igual en la cámara lenta.
/// </summary>
public class PlayerHover : MonoBehaviour
{
    private float height;
    private float bob;
    private float period = 2.5f;

    /// <summary>Metros que sube el cuerpo y la cámara en este instante (0 si el personaje no levita).</summary>
    public float Offset => HoverMath.Offset(Time.unscaledTime, height, bob, period);

    public void Configure(CharacterDefinition character)
    {
        height = character != null ? character.hoverHeight : 0f;
        bob = character != null ? character.hoverBob : 0f;
        period = character != null ? character.hoverPeriod : 2.5f;
    }
}
