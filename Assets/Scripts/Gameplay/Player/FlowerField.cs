using System;
using UnityEngine;

/// <summary>
/// Campo de flores de Frieren (tecla E), en dos pasos. 1) Al pulsar E se abre la colocación: un círculo del tamaño del campo sigue la
/// mira sobre el suelo, sin mantener la tecla. 2) Clic izquierdo o E otra vez lo confirman; clic derecho lo cancela. El maná y el
/// enfriamiento se cobran al confirmar. El campo cura a Frieren (si está dentro) y ralentiza a los enemigos dentro. Sin daño.
/// </summary>
public class FlowerField : MonoBehaviour
{
    private static readonly Color RingColor = new Color(1f, 0.78f, 0.92f, 0.95f);
    private static readonly Color FloorColor = new Color(1f, 0.62f, 0.82f, 0.25f);
    private static readonly Color[] PetalColors =
    {
        new Color(1f, 0.55f, 0.75f), new Color(1f, 0.9f, 0.45f), new Color(0.75f, 0.6f, 1f), new Color(1f, 1f, 1f)
    };
    private const int RingSegments = 64;
    private const int FlowerCount = 48;
    private const float RingHeight = 0.08f;
    private const string PlacingHint = "Clic izquierdo o E: colocar el campo  ·  Clic derecho: cancelar";

    private readonly HealOverTime heal = new HealOverTime();
    private readonly Collider[] buffer = new Collider[128];

    private Shooting shooting;
    private PlayerHealth health;
    private AbilityDefinition ability;
    private int rank = 1;
    private Action onConfirmed;
    private LineRenderer ring;
    private GameObject fieldRoot;
    private Vector3 fieldCenter;
    private float fieldEnd;
    private float nextTick;

    public bool IsPlacing { get; private set; }

    /// <summary>Hay un campo en pie (ya colocado y sin terminar).</summary>
    public bool IsActive => fieldRoot != null;

    /// <summary>Fotograma en que se cerró la colocación (confirmar o cancelar): ese clic no debe disparar ni cargar nada.</summary>
    public int ClosedFrame { get; private set; } = -1;

    private float Radius => ability != null ? ability.radius : 0f;

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        GameEvents.GameOver += OnGameOver;
        GameEvents.CharacterChanged += OnCharacterChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= OnGameOver;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        ForceEnd();
    }

    private void OnGameOver(string message, GameOverCause cause) => ForceEnd();
    private void OnCharacterChanged(CharacterDefinition character) => ForceEnd();

    /// <summary>Abre la colocación. False si ya estaba abierta o no alcanza el maná.</summary>
    public bool Begin(AbilityDefinition definition, int abilityRank, Action confirmed)
    {
        if (IsPlacing || shooting.Mana == null || shooting.Mana.Current < definition.manaCost) return false;

        ability = definition;
        rank = abilityRank;
        onConfirmed = confirmed;
        IsPlacing = true;
        if (shooting.Body != null) shooting.Body.HoldCast(true); // eligiendo el círculo, el gesto se queda en su máximo

        if (ring == null) ring = CreateRing();
        ring.gameObject.SetActive(true);
        StaffHud.SetHint(PlacingHint);
        return true;
    }

    /// <summary>Cierra la colocación y crea el campo en el círculo. Cobra el maná y empieza el enfriamiento.</summary>
    public void Confirm()
    {
        if (!IsPlacing) return;

        Vector3 center = PlacementPoint();
        if (!shooting.Mana.TrySpend(ability.manaCost))
        {
            Cancel();
            return;
        }

        shooting.RefreshMana();
        CloseRing();
        StartField(center);
        if (shooting.Body != null) shooting.Body.ReleaseCast(); // al poner el círculo, el gesto vuelve
        onConfirmed?.Invoke();
    }

    /// <summary>Cierra la colocación sin gastar nada ni empezar el enfriamiento.</summary>
    public void Cancel()
    {
        if (!IsPlacing) return;

        CloseRing();
    }

    /// <summary>Corta todo: la colocación y el campo activo (cambio de personaje, fin de partida).</summary>
    public void ForceEnd()
    {
        if (IsPlacing && shooting != null && shooting.Body != null) shooting.Body.HoldCast(false);
        IsPlacing = false;
        StaffHud.SetHint(null);
        if (ring != null) ring.gameObject.SetActive(false);
        EndField();
    }

    private void CloseRing()
    {
        if (shooting.Body != null) shooting.Body.HoldCast(false);
        IsPlacing = false;
        ClosedFrame = Time.frameCount;
        StaffHud.SetHint(null);
        if (ring != null) ring.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsPlacing)
        {
            if (GameState.InputBlocked || health == null || health.IsDead)
            {
                Cancel();
            }
            else
            {
                DrawRing(PlacementPoint());

                GameInput input = GameInput.Instance;
                if (input.FirePressed) Confirm();
                else if (input.AimPressed) Cancel();
            }
        }

        if (fieldRoot == null) return;

        if (Time.time >= fieldEnd)
        {
            EndField();
            return;
        }

        if (Time.time >= nextTick) TickField();
    }

    // Donde mira la mira, en el suelo y a no más de 'fieldPlaceRange' de Frieren.
    private Vector3 PlacementPoint()
    {
        Vector3 point = shooting.AimGroundPoint(ability.fieldPlaceRange + 20f);
        return FlowerFieldRules.ClampPoint(transform.position, point, ability.fieldPlaceRange);
    }

    private void StartField(Vector3 center)
    {
        EndField();

        fieldCenter = center;
        fieldEnd = Time.time + ability.DurationAt(rank);
        nextTick = Time.time;
        heal.Reset();
        fieldRoot = BuildFlowers(center, Radius);
    }

    private void EndField()
    {
        if (fieldRoot != null) Destroy(fieldRoot);
        fieldRoot = null;
    }

    private void TickField()
    {
        float step = ability.fieldTickSeconds;
        nextTick = Time.time + step;

        // Cura a Frieren si está dentro del círculo.
        if (health != null && !health.IsDead && FlowerFieldRules.Contains(fieldCenter, Radius, transform.position))
        {
            int amount = heal.Add(health.MaxHealth, ability.fieldHealFractionPerSecond, step);
            if (amount > 0) health.Heal(amount);
        }

        // Ralentiza a cada enemigo dentro. La ralentización dura un poco más que el tick: al salir se recuperan casi enseguida.
        int count = Physics.OverlapSphereNonAlloc(fieldCenter, Radius + 3f, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead) continue;
            if (!FlowerFieldRules.Contains(fieldCenter, Radius, enemy.transform.position, enemy.BodyRadius)) continue;

            enemy.ApplySlow(ability.fieldSlowFraction, step * 2f);
        }
    }

    // --- Aspecto provisional (sin arte): anillo de línea, disco rosado y flores de colores ---

    private LineRenderer CreateRing()
    {
        var go = new GameObject("FlowerFieldRing");
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = RingSegments;
        line.startWidth = line.endWidth = 0.3f;   // grueso: el círculo se coloca a hasta 25 m y una línea fina no se vería
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = new Material(Shader.Find("Sprites/Default")) { color = RingColor };
        line.startColor = line.endColor = RingColor;
        go.SetActive(false);
        return line;
    }

    private void DrawRing(Vector3 center)
    {
        float radius = Radius;
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, RingHeight, Mathf.Sin(angle) * radius));
        }
    }

    private GameObject BuildFlowers(Vector3 center, float radius)
    {
        var root = new GameObject("FlowerField");
        root.transform.position = center;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Ground";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(root.transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        disc.GetComponent<Renderer>().sharedMaterial = NewMaterial(FloorColor);
        disc.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int i = 0; i < FlowerCount; i++)
        {
            // Reparto uniforme en el disco (raíz cuadrada para que no se junten en el centro).
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float distance = Mathf.Sqrt(UnityEngine.Random.value) * (radius - 0.3f);

            GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flower.name = "Flower";
            Destroy(flower.GetComponent<Collider>());
            flower.transform.SetParent(root.transform, false);
            flower.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance, 0.14f, Mathf.Sin(angle) * distance);
            flower.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.22f, 0.4f);

            Renderer r = flower.GetComponent<Renderer>();
            r.sharedMaterial = NewMaterial(PetalColors[i % PetalColors.Length]);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return root;
    }

    private static Material NewMaterial(Color color) => new Material(Shader.Find("Sprites/Default")) { color = color };
}
