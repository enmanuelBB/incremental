using UnityEngine;

/// <summary>
/// Base de los datos del juego (armas, enemigos, personajes, oleadas). El id es lo que se guarda
/// en el progreso, así se puede renombrar o mover el asset sin romper los guardados.
/// </summary>
public abstract class GameDefinition : ScriptableObject
{
    [SerializeField, Tooltip("Identificador estable que usa el guardado. No lo cambies una vez que existan partidas guardadas. Si queda vacío se usa el nombre del asset.")]
    private string id;

    public string Id => string.IsNullOrEmpty(id) ? name : id;
}
