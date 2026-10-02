using System.Collections;
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
    private static readonly Color BeamColor = new Color(0.75f, 0.92f, 1f, 1f);

    [SerializeField] private Camera cam;
    [SerializeField, Tooltip("Personaje con el que arranca la escena. CharacterManager lo reemplaza por el elegido en el menú.")]
    private CharacterDefinition character;

    private WeaponState[] states = new WeaponState[0];
    private int currentIndex;
    private bool isReloading;
    private float nextFireTime;
    private Coroutine reloadRoutine;

    // Bastón
    private ManaPool mana;
    private readonly AbilityCooldown abilityCooldown = new AbilityCooldown();
    private int lastPublishedMana = -1;
    private BeamVfx boltVfx;
    private BeamVfx beamVfx;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public int WeaponCount => states.Length;
    public int CurrentWeaponIndex => currentIndex;
    public WeaponState CurrentWeapon => states[currentIndex];
    public CharacterDefinition Character => character;

    /// <summary>De dónde salen los disparos mágicos (la punta del bastón). Si es null, del pecho del jugador.</summary>
    public Transform Muzzle { get; set; }

    public ManaPool Mana => mana;
    public AbilityCooldown AbilityCooldown => abilityCooldown;

    public WeaponState GetWeapon(int index) => states[index];

    private Vector3 MuzzlePosition => Muzzle != null ? Muzzle.position : transform.position + transform.forward * 0.5f;

    private void Awake()
    {
        Instance = this;
        boltVfx = BeamVfx.Create("BoltVfx");
        beamVfx = BeamVfx.Create("BeamVfx");
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

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
        isReloading = false;
        nextFireTime = 0f;
        abilityCooldown.Reset();

        CharacterSave progress = SaveSystem.Data.GetCharacter(def.Id);
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

        mana = null;
        ConfigureMana();
    }

    private void Update()
    {
        TickMana();

        if (GameState.InputBlocked) return;

        GameInput input = GameInput.Instance;

        for (int i = 0; i < states.Length && i < GameInput.WeaponSlots; i++)
        {
            if (input.WeaponSlotPressed(i)) SwitchWeapon(i);
        }
        if (input.NextWeaponPressed) CycleWeapon(1);
        if (input.PreviousWeaponPressed) CycleWeapon(-1);

        if (!CurrentWeapon.UsesAmmo)
        {
            UpdateStaff(CurrentWeapon, input);
            return;
        }

        if (isReloading) return;

        WeaponState weapon = CurrentWeapon;

        if (input.ReloadPressed && weapon.Ammo < weapon.MagazineSize)
        {
            reloadRoutine = StartCoroutine(Reload());
            return;
        }

        bool triggerPressed = weapon.IsAutomatic ? input.FireHeld : input.FirePressed;

        if (triggerPressed && Time.time >= nextFireTime)
        {
            if (weapon.Ammo <= 0)
            {
                reloadRoutine = StartCoroutine(Reload());
                return;
            }

            nextFireTime = Time.time + weapon.FireRate;
            Shoot();
        }
    }

    // Bastón: el disparo básico es gratis y la habilidad gasta maná y tiene enfriamiento.
    private void UpdateStaff(WeaponState weapon, GameInput input)
    {
        if (input.Ability1Pressed) TryCastAbility(weapon);

        bool triggerPressed = weapon.IsAutomatic ? input.FireHeld : input.FirePressed;

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

        if (weapon.UsesAmmo)
        {
            weapon.Ammo--;
            PublishAmmo();
        }

        AudioManager.Instance.PlaySFX(weapon.Definition.shootSound);

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
            return;
        }

        StartTrigger start = hit.collider.GetComponentInParent<StartTrigger>();
        if (start != null) start.Activate();
    }

    private void TryCastAbility(WeaponState weapon)
    {
        StaffDefinition staff = weapon.Staff;
        if (staff == null || mana == null) return;
        if (!abilityCooldown.IsReady(Time.time)) return;
        if (!mana.TrySpend(staff.abilityManaCost)) return;

        float cooldown = weapon.AbilityCooldownTime;
        abilityCooldown.Start(Time.time, cooldown);

        Vector3 origin = MuzzlePosition;
        Vector3 end = PiercingBeam.Cast(new Ray(origin, BeamDirection(origin, staff.abilityRange)),
            staff.abilityRange, staff.abilityBeamRadius, weapon.AbilityDamage, transform);

        beamVfx.Show(origin, end, BeamColor, staff.abilityBeamRadius * 1.5f, 0.25f);

        GameEvents.RaiseAbilityUsed(staff.abilityName, Time.time + cooldown);
        PublishMana(true);
    }

    /// <summary>
    /// El rayo sale horizontal, a la altura del bastón, hacia donde apunta la mira. Así recorre el campo a la
    /// altura de los enemigos en vez de clavarse en el suelo, y atraviesa filas enteras.
    /// </summary>
    private Vector3 BeamDirection(Vector3 origin, float range)
    {
        Ray aim = AimRay();
        Vector3 target = TryGetHit(range, out RaycastHit hit) ? hit.point : aim.GetPoint(range);

        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = aim.direction;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    private Ray AimRay() => cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

    /// <summary>
    /// Lanza el rayo desde el centro de la pantalla y devuelve el impacto más cercano,
    /// ignorando al propio jugador (la cámara en tercera persona queda detrás de él) y los triggers.
    /// </summary>
    private bool TryGetHit(float range, out RaycastHit result)
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

        AudioManager.Instance.PlaySFX(weapon.Definition.reloadSound);
        GameEvents.RaiseAmmoChanged(0, weapon.MagazineSize, true);

        yield return new WaitForSeconds(reloadTime * fillPoint);
        weapon.Ammo = weapon.MagazineSize;

        yield return new WaitForSeconds(reloadTime * (1f - fillPoint));

        isReloading = false;
        PublishAmmo();
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

    private void PublishHud()
    {
        if (states.Length == 0) return;

        bool usesMana = !CurrentWeapon.UsesAmmo;
        StaffDefinition staff = CurrentWeapon.Staff;
        GameEvents.RaiseResourceModeChanged(usesMana, staff != null ? staff.abilityName : null, staff != null ? staff.abilityIcon : null);

        if (usesMana) PublishMana(true);
        else PublishAmmo();
    }

    private void PublishAmmo()
    {
        WeaponState weapon = CurrentWeapon;
        if (!weapon.UsesAmmo) return;

        GameEvents.RaiseAmmoChanged(weapon.Ammo, weapon.MagazineSize, isReloading);
    }

    public bool BuyWeapon(int index) => states[index].TryBuy();

    public bool BuyUpgrade(int index, UpgradeType type)
    {
        bool bought = states[index].TryUpgrade(type);

        // Subir "Maná" cambia la regeneración; se aplica en caliente, sin vaciar la reserva.
        if (bought && index == currentIndex)
        {
            ConfigureMana();
            PublishMana(true);
        }

        return bought;
    }
}
