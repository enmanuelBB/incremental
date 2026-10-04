using UnityEngine;

/// <summary>
/// Cambia cómo va la espada en la mano según lo que hace el personaje. Las animaciones de Mixamo se retargetean a un
/// cuerpo de otras proporciones, y un mismo ángulo de la espada no sirve para todo: al moverse va mejor alineada con
/// las dos manos y, al atacar, pasando por el puño (el ángulo original del modelo). La espada no tiene curvas en los
/// clips, así que su rotación local se queda donde la deja este componente.
/// </summary>
public class SwordGrip : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("Hueso de la espada (hijo de la mano)")] private Transform weapon;
    [SerializeField, Tooltip("Rotación local de la espada al moverse (idle, caminar, correr, saltar)")]
    private Quaternion moveRotation = Quaternion.identity;
    [SerializeField, Tooltip("Rotación local de la espada mientras ataca")]
    private Quaternion attackRotation = Quaternion.identity;
    [SerializeField, Tooltip("Nombre del estado de ataque en la capa base del Animator")]
    private string attackState = "Attack";

    private int attackHash;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        attackHash = Animator.StringToHash(attackState);
    }

    private void LateUpdate()
    {
        if (weapon == null || animator == null) return;

        weapon.localRotation = Quaternion.Slerp(moveRotation, attackRotation, AttackWeight());
    }

    // 0 = moviéndose, 1 = atacando; durante las transiciones sigue el avance de la mezcla del Animator.
    private float AttackWeight()
    {
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        bool inAttack = current.shortNameHash == attackHash;

        if (!animator.IsInTransition(0)) return inAttack ? 1f : 0f;

        float progress = animator.GetAnimatorTransitionInfo(0).normalizedTime;
        bool toAttack = animator.GetNextAnimatorStateInfo(0).shortNameHash == attackHash;

        if (inAttack && !toAttack) return 1f - progress;
        if (!inAttack && toAttack) return progress;
        return inAttack ? 1f : 0f;
    }
}
