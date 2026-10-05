using System;
using UnityEngine;

/// <summary>
/// Alma que deja un enemigo al morir con Guts: flota con un vaivén y, si Guts pasa cerca, lo cura y desaparece.
/// Si nadie la recoge, se va sola al terminar su duración. SoulSpawner la crea y la recicla (pool).
/// </summary>
public class SoulPickup : MonoBehaviour
{
    private const float HoverHeight = 0.6f;
    private const float BobAmplitude = 0.12f;
    private const float BobSpeed = 3f;

    private Transform player;
    private PlayerHealth health;
    private Action<SoulPickup> release;
    private float timeLeft;
    private float radius;
    private int healAmount;
    private float baseY;

    public void Init(Vector3 position, int heal, float lifetime, float pickupRadius, float scale,
        Transform playerTransform, PlayerHealth playerHealth, Action<SoulPickup> releaseToPool)
    {
        transform.position = new Vector3(position.x, position.y + HoverHeight, position.z);
        transform.localScale = Vector3.one * scale;
        baseY = transform.position.y;
        healAmount = heal;
        timeLeft = lifetime;
        radius = pickupRadius;
        player = playerTransform;
        health = playerHealth;
        release = releaseToPool;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            release(this);
            return;
        }

        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * BobSpeed) * BobAmplitude;
        transform.position = p;

        if (player == null || health == null || health.IsDead) return;
        if (!SoulRules.IsWithinReach(player.position, transform.position, radius)) return;

        health.Heal(healAmount);   // Health.Heal no pasa de la vida máxima; el alma se consume igual
        release(this);
    }
}
