using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque del jugador: disparo, recarga, cambio de arma y, con un bastón, maná y habilidad.
/// Los datos de cada arma viven en WeaponDefinition (o StaffDefinition) y su estado en partida
/// (munición, niveles) en WeaponState. El personaje activo decide qué armas hay (ver CharacterManager).
/// </summary>
public class Shooting : MonoBehaviour
{
    public static Shooting Instance { get; private set; }

    private static readonly Color BoltColor = new Color(0.6f, 0.85f, 1f, 0.9f);

    [SerializeField] private Camera cam;
    [SerializeField, Tooltip("Personaje con el que arranca la escena. CharacterManager lo reemplaza por el elegido en el menú.")]
    private CharacterDefinition character;

    private WeaponState[] states = new WeaponState[0];
    private int currentIndex;
    private bool isReloading;
    private float nextFireTime;
    private Coroutine reloadRoutine;

    // Espada (Guts)
    private Coroutine swordRoutine;
    private FuryMeter fury = new FuryMeter(0f);
    private readonly Collider[] meleeBuffer = new Collider[64];
    private readonly List<EnemyAI> meleeTargets = new List<EnemyAI>();
    private const float MeleeSearchMargin = 3f; // holgura para enemigos grandes (el radio real se cuenta en MeleeCone)

    // Bastón
    private ManaPool mana;
    private int lastPublishedMana = -1;
    private BeamVfx boltVfx;
    private BeamVfx beamVfx;
    private ZoltraakCaster zoltraak;
    private PlayerHover hover;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private PlayerAbilities abilities;

    // Nivel de sangrado del personaje (sube el tope de pilas con puntos de personaje). Sale del guardado.
    private int BleedLevel =>
        character == null ? 1 : Mathf.Max(1, SaveSystem.Data.GetCharacter(character.Id).bleedLevel);

    /// <summary>Tope de pilas de sangrado por enemigo con el nivel de sangrado actual.</summary>
    public int BleedCap => BleedStacks.CapForLevel(BleedLevel);

    public int WeaponCount => states.Length;
    public int CurrentWeaponIndex => currentIndex;
    public WeaponState CurrentWeapon => states[currentIndex];
    public CharacterDefinition Character => character;

    /// <summary>De dónde salen los disparos mágicos (la punta del bastón). Si es null, del pecho del jugador.</summary>
    public Transform Muzzle { get; set; }

    /// <summary>Armas que lleva en la mano (por ejemplo las dos pistolas). Null si el personaje no lleva modelo.</summary>
    public HeldGuns HeldGuns { get; set; }

    /// <summary>Cuerpo animado del personaje (null si usa el cilindro).</summary>
    public PlayerBody Body { get; set; }

    public ManaPool Mana => mana;

    /// <summary>Verdadero si el arma activa es un bastón (Frieren): el clic izquierdo carga el Zoltraak y el derecho es el disparo básico.</summary>
    public bool UsesStaff => states.Length > 0 && CurrentWeapon.Staff != null;

    /// <summary>La punta del bastón (o el pecho del jugador si no hay).</summary>
    public Vector3 MuzzlePoint => MuzzlePosition;

    /// <summary>Dibuja el trazo grueso del rayo de maná (habilidades).</summary>
    public void ShowBeam(Vector3 origin, Vector3 end, Color color, float width, float seconds) =>
        beamVfx.Show(origin, end, color, width, seconds);

    /// <summary>Avisa al HUD del maná actual (las habilidades que lo gastan).</summary>
    public void RefreshMana() => PublishMana(true);

    /// <summary>
    /// Punto del suelo al que apunta la mira: el primer golpe del rayo (o el final del alcance) bajado hasta el piso.
    /// Si no hay piso debajo, se queda a la altura de los pies del jugador.
    /// </summary>
    public Vector3 AimGroundPoint(float range)
    {
        Vector3 point = TryGetHit(range, out RaycastHit hit) ? hit.point : AimRay().GetPoint(range);

        if (Physics.Raycast(point + Vector3.up * 50f, Vector3.down, out RaycastHit ground, 120f, ~0, QueryTriggerInteraction.Ignore)
            && !ground.transform.IsChildOf(transform))
            return ground.point;

        return new Vector3(point.x, transform.position.y + PlayerBody.FeetLocalY, point.z);
    }

    /// <summary>Furia de Guts (máximo 0 si el personaje no tiene). Las habilidades futuras usarán la misma barra.</summary>
    public FuryMeter Fury => fury;

    public WeaponState GetWeapon(int index) => states[index];

    private Vector3 MuzzlePosition => Muzzle != null ? Muzzle.position : transform.position + transform.forward * 0.5f;

    private void Awake()
    {
        Instance = this;
        boltVfx = BeamVfx.Create("BoltVfx");
        beamVfx = BeamVfx.Create("BeamVfx");
        abilities = GetComponent<PlayerAbilities>();
        if (abilities == null) abilities = gameObject.AddComponent<PlayerAbilities>();
        hover = GetComponent<PlayerHover>();
        if (hover == null) hover = gameObject.AddComponent<PlayerHover>();
        Build(character);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (boltVfx != null) Destroy(boltVfx.gameObject);
        if (beamVfx != null) Destroy(beamVfx.gameObject);
    }

    private void Start()
    {
        PublishHud();
    }

    /// <summary>Cambia de personaje: reconstruye las armas con su progreso y avisa a la interfaz.</summary>
    public void SetCharacter(CharacterDefinition newCharacter)
    {
        Build(newCharacter);
        PublishHud();
    }

    private void Build(CharacterDefinition def)
    {
        character = def;
        hover.Configure(def);

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
        if (swordRoutine != null)
        {
            StopCoroutine(swordRoutine);
            swordRoutine = null;
        }
        isReloading = false;
        nextFireTime = 0f;
        if (zoltraak != null) zoltraak.Cancel();
        abilities.Configure(def);

        CharacterSave progress = SaveSystem.Data.GetCharacter(def.Id);
        if (Progression.ClampAbilityRanks(progress, def)) SaveSystem.Save();   // rangos guardados por encima del tope de su tipo
        WeaponDefinition[] weapons = def.startingWeapons;

        states = new WeaponState[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
            states[i] = new WeaponState(weapons[i], progress.GetWeapon(weapons[i].Id, i));

        currentIndex = 0;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].Owned)
            {
                currentIndex = i;
                break;
            }
        }

        SwordDefinition furySword = states.Length > 0 ? CurrentWeapon.Sword : null;
        fury = new FuryMeter(furySword != null ? furySword.furyMax : 0f);

        mana = null;
        ConfigureMana();

        // Las habilidades se configuraron antes de tener las armas: el rayo de maná toma su enfriamiento del bastón.
        abilities.RefreshBuild();
    }

    private void Update()
    {
        TickMana();

        // En niebla no se dispara, ni se recarga, ni se cambia de arma.
        if (GameState.InputBlocked || abilities.IsMist) return;

        GameInput input = GameInput.Instance;

        // Un personaje sin armas: el clic solo reproduce el corte, sin daño.
        if (states.Length == 0)
        {
            if (input.FirePressed && Body != null) Body.PlayAttack();
            return;
        }

        for (int i = 0; i < states.Length && i < GameInput.WeaponSlots; i++)
        {
            if (input.WeaponSlotPressed(i)) SwitchWeapon(i);
        }
        if (input.NextWeaponPressed) CycleWeapon(1);
        if (input.PreviousWeaponPressed) CycleWeapon(-1);

        if (CurrentWeapon.Sword != null)
        {
            UpdateSword(CurrentWeapon, input);
            return;
        }

        if (!CurrentWeapon.UsesAmmo)
        {
            UpdateStaff(CurrentWeapon, input);
            return;
        }

        if (isReloading) return;

        WeaponState weapon = CurrentWeapon;

        if (input.ReloadPressed && !weapon.Magazines.IsFull)
        {
            reloadRoutine = StartCoroutine(Reload());
            return;
        }

        bool triggerPressed = weapon.IsAutomatic ? input.FireHeld : input.FirePressed;

        if (triggerPressed && Time.time >= nextFireTime)
        {
            if (weapon.Magazines.IsEmpty)
            {
                reloadRoutine = StartCoroutine(Reload());
                return;
            }

            nextFireTime = Time.time + weapon.FireRate;
            Shoot();
        }
    }

    // Espada: el clic reproduce el corte (rápido) y el daño cae un instante después; el tiempo entre golpes es largo
    // y es lo que mejora la tienda. Mantener el clic repite el golpe al ritmo de la cadencia.
    private void UpdateSword(WeaponState weapon, GameInput input)
    {
        bool triggerPressed = weapon.IsAutomatic ? input.FireHeld : input.FirePressed;
        if (!triggerPressed || Time.time < nextFireTime) return;

        SwordDefinition sword = weapon.Sword;
        nextFireTime = Time.time + weapon.FireRate * BerserkArmor.CadenceMultiplier;

        if (Body != null) Body.PlayAttack(sword.attackAnimSpeed);

        if (swordRoutine != null) StopCoroutine(swordRoutine);
        swordRoutine = StartCoroutine(SwordHit(weapon, sword.hitDelay));
    }

    private IEnumerator SwordHit(WeaponState weapon, float delay)
    {
        yield return new WaitForSeconds(delay);
        swordRoutine = null;

        // Si en este instante ya terminó la partida o está en niebla, el golpe se pierde.
        if (GameState.InputBlocked || abilities.IsMist) yield break;

        SwingSword(weapon);
    }

    // Todos los enemigos vivos dentro del cono reciben el golpe; cada uno tira su propio dado de aturdimiento.
    private void SwingSword(WeaponState weapon)
    {
        SwordDefinition sword = weapon.Sword;
        float arc = BerserkArmor.SwingArc(sword.arcDegrees);   // 360° con la armadura Berserker puesta

        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : transform.forward;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;

        Vector3 origin = transform.position;

        meleeTargets.Clear();
        int count = Physics.OverlapSphereNonAlloc(origin, sword.range + MeleeSearchMargin, meleeBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            // El cubo de inicio de la partida también se activa a espadazos (como a balazos).
            StartTrigger start = meleeBuffer[i].GetComponentInParent<StartTrigger>();
            if (start != null)
            {
                Vector3 nearest = meleeBuffer[i].ClosestPoint(origin);
                if (MeleeCone.Contains(origin, forward, nearest, sword.range, arc)) start.Activate();
                continue;
            }

            EnemyAI enemy = meleeBuffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead || meleeTargets.Contains(enemy)) continue;
            if (!MeleeCone.Contains(origin, forward, enemy.transform.position, sword.range, arc, enemy.BodyRadius)) continue;

            meleeTargets.Add(enemy);
        }

        // Un golpe que no toca a nadie ni carga ni gasta la Furia.
        if (meleeTargets.Count == 0) return;

        // Furia llena: este golpe sale potenciado (daño x2 y aturdimiento seguro) y gasta toda la barra.
        bool empowered = fury.TryConsume();

        int damage = weapon.Damage;
        if (empowered) damage = Mathf.RoundToInt(damage * sword.furyDamageMultiplier);
        float stunChance = weapon.StunChance;
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;

        foreach (EnemyAI enemy in meleeTargets)
        {
            // Más daño a los que ya estaban aturdidos (se mira antes de aturdirlos con este mismo golpe).
            enemy.TakeDamage(tree.ScaleVsStunned(damage, enemy.IsStunned));   // primero el daño: si muere, ApplyStun lo ignora
            // El aturdimiento seguro no depende del dado (Random.value puede valer exactamente 1).
            if (empowered || StunRules.Roll(stunChance, Random.value)) enemy.ApplyStun(sword.stunSeconds);
        }

        // El golpe potenciado no suma Furia; los demás suman por enemigo golpeado.
        if (!empowered) fury.Add(FuryMeter.GainForHits(meleeTargets.Count, sword.furyPerEnemyHit, sword.furyMaxPerSwing)
            * (1f + tree.FuryGainPercent) * BerserkArmor.FuryGainMultiplier);
        PublishFury();
    }

    // Bastón (Frieren): clic izquierdo = Zoltraak cargado, clic derecho = disparo básico gratis, Q/E/F = habilidades con rango.
    private void UpdateStaff(WeaponState weapon, GameInput input)
    {
        if (zoltraak == null)
        {
            zoltraak = GetComponent<ZoltraakCaster>();
            if (zoltraak == null) zoltraak = gameObject.AddComponent<ZoltraakCaster>();
        }

        zoltraak.Tick(weapon, input);

        // Colocando el campo de flores no se dispara; el clic que confirma o cancela tampoco cuenta como disparo.
        if (abilities.BlocksFire || zoltraak.IsCharging) return;

        bool triggerPressed = weapon.IsAutomatic ? input.AimHeld : input.AimPressed;

        if (triggerPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + weapon.FireRate;
            Shoot();
        }
    }

    private void CycleWeapon(int direction)
    {
        for (int step = 1; step < states.Length; step++)
        {
            int candidate = (currentIndex + direction * step + states.Length * step) % states.Length;
            if (states[candidate].Owned)
            {
                SwitchWeapon(candidate);
                return;
            }
        }
    }

    private void SwitchWeapon(int index)
    {
        if (index == currentIndex) return;
        if (!states[index].Owned) return;

        if (isReloading)
        {
            StopCoroutine(reloadRoutine);
            isReloading = false;
        }

        currentIndex = index;
        nextFireTime = 0f;
        ConfigureMana();
        PublishHud();
    }

    private void Shoot()
    {
        WeaponState weapon = CurrentWeapon;

        // Con varios cañones (dos pistolas) los disparos se turnan: cada clic gasta una bala del cañón que toca.
        if (weapon.UsesAmmo)
        {
            if (!weapon.Magazines.TryFire(out int barrel)) return;
            PublishSlot(currentIndex);

            if (HeldGuns != null)
            {
                HeldGuns.Fire(barrel, weapon.Definition.RecoilOf(barrel));
                Muzzle = HeldGuns.MuzzleOf(barrel);
            }
            if (Body != null) Body.PlayShoot(barrel);
        }

        AudioManager.Instance.PlaySFX(weapon.Definition.shootSound);

        FireBullet(weapon);
    }

    private void FireBullet(WeaponState weapon)
    {
        bool found = TryGetHit(weapon.Definition.range, out RaycastHit hit);

        if (!weapon.UsesAmmo)
        {
            Vector3 end = found ? hit.point : AimRay().GetPoint(weapon.Definition.range);
            boltVfx.Show(MuzzlePosition, end, BoltColor, 0.06f, 0.08f);
        }

        if (!found) return;

        EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(weapon.Damage);
            abilities.AfterBulletHit(enemy, hit.point, weapon);
            return;
        }

        StartTrigger start = hit.collider.GetComponentInParent<StartTrigger>();
        if (start != null) start.Activate();
    }

    public Ray AimRay() => cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

    /// <summary>Dibuja un rayo de energía desde el arma hasta el punto dado (habilidades).</summary>
    public void ShowBolt(Vector3 end, Color color, float width, float seconds) =>
        boltVfx.Show(MuzzlePosition, end, color, width, seconds);

    /// <summary>
    /// Lanza el rayo desde el centro de la pantalla y devuelve el impacto más cercano,
    /// ignorando al propio jugador (la cámara en tercera persona queda detrás de él) y los triggers.
    /// </summary>
    public bool TryGetHit(float range, out RaycastHit result)
    {
        Ray ray = AimRay();
        int count = Physics.RaycastNonAlloc(ray, hitBuffer, range, ~0, QueryTriggerInteraction.Ignore);

        result = default;
        float closest = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.distance >= closest) continue;

            closest = hit.distance;
            result = hit;
            found = true;
        }

        return found;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        WeaponState weapon = CurrentWeapon;
        float reloadTime = weapon.ReloadTime;
        float fillPoint = weapon.Definition.ammoFillPoint;

        if (Body != null) Body.PlayReload(reloadTime);
        AudioManager.Instance.PlaySFX(weapon.Definition.reloadSound);
        PublishSlot(currentIndex);

        yield return new WaitForSeconds(reloadTime * fillPoint);
        weapon.Magazines.Refill();

        yield return new WaitForSeconds(reloadTime * (1f - fillPoint));

        isReloading = false;
        PublishSlot(currentIndex);
    }

    // --- Maná y HUD ---

    private void ConfigureMana()
    {
        if (states.Length == 0 || CurrentWeapon.Staff == null)
        {
            mana = null;
            return;
        }

        WeaponState weapon = CurrentWeapon;
        if (mana == null) mana = new ManaPool(weapon.Staff.manaMax, weapon.ManaRegen);
        else mana.Configure(weapon.Staff.manaMax, weapon.ManaRegen);

        lastPublishedMana = -1;
    }

    private void TickMana()
    {
        if (mana == null) return;

        mana.Tick(Time.deltaTime);
        PublishMana(false);
    }

    // Se avisa solo cuando cambia el entero, para no inundar de eventos por frame.
    private void PublishMana(bool force)
    {
        if (mana == null) return;

        int whole = Mathf.FloorToInt(mana.Current);
        if (!force && whole == lastPublishedMana) return;

        lastPublishedMana = whole;
        GameEvents.RaiseManaChanged(mana.Current, mana.Max);
    }

    /// <summary>Reconfigura las habilidades con el rango y los bonos del árbol actuales (cargas, enfriamientos).</summary>
    public void RefreshBuild() => abilities.RefreshBuild();

    /// <summary>Vuelve a dibujar las casillas de habilidad (al aprender una habilidad en la estación de mejoras).</summary>
    public void RefreshAbilityHud()
    {
        abilities.RefreshBuild();
        GameEvents.RaiseAbilitiesChanged(abilities.HudInfo());
    }

    /// <summary>Avisa a la barra de Furia del estado actual (la llamarada también la gasta).</summary>
    public void PublishFury() => GameEvents.RaiseFuryChanged(fury.Current, fury.Max);

    private void PublishHud()
    {
        PublishFury();

        if (states.Length == 0 || (!CurrentWeapon.UsesAmmo && CurrentWeapon.Staff == null))
        {
            // Sin armas, o con una sin munición ni maná (la espada): solo las habilidades del personaje (si tiene).
            GameEvents.RaiseResourceModeChanged(false);
            GameEvents.RaiseAbilitiesChanged(abilities.HudInfo());
            return;
        }

        bool usesMana = !CurrentWeapon.UsesAmmo;
        GameEvents.RaiseResourceModeChanged(usesMana);
        GameEvents.RaiseAbilitiesChanged(abilities.HudInfo());

        if (usesMana) PublishMana(true);
        else PublishSlots();
    }

    // Cada arma con munición tiene su casilla en el HUD: así se ve también la que no está equipada.
    private void PublishSlots()
    {
        for (int i = 0; i < states.Length; i++) PublishSlot(i);
    }

    private void PublishSlot(int index)
    {
        WeaponState weapon = states[index];
        if (!weapon.UsesAmmo) return;

        GameEvents.RaiseWeaponSlotChanged(new WeaponSlotInfo
        {
            Index = index,
            Name = weapon.Name,
            BarrelAmmo = weapon.Magazines.Snapshot(),
            Magazine = weapon.MagazineSize,
            Owned = weapon.Owned,
            Selected = index == currentIndex,
            Reloading = isReloading && index == currentIndex
        });
    }

    public bool BuyWeapon(int index)
    {
        bool bought = states[index].TryBuy();
        if (bought) PublishSlots();   // pasó a estar comprada
        return bought;
    }

    public bool BuyUpgrade(int index, UpgradeType type)
    {
        bool bought = states[index].TryUpgrade(type);

        // Subir "Maná" cambia la regeneración; se aplica en caliente, sin vaciar la reserva.
        if (bought && index == currentIndex)
        {
            ConfigureMana();
            PublishMana(true);
            abilities.RefreshBuild();   // "Maná" también baja el enfriamiento del rayo
        }

        return bought;
    }
}
