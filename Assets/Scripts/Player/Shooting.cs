using System.Collections;
using UnityEngine;

/// <summary>
/// Disparo, recarga y cambio de arma del jugador. Los datos de cada arma viven en
/// WeaponDefinition y su estado en partida (munición, niveles) en WeaponState.
/// </summary>
public class Shooting : MonoBehaviour
{
    public static Shooting Instance { get; private set; }

    [SerializeField] private Camera cam;
    [SerializeField] private WeaponDefinition[] weapons;

    private WeaponState[] states;
    private int currentIndex;
    private bool isReloading;
    private float nextFireTime;
    private Coroutine reloadRoutine;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public int WeaponCount => states.Length;
    public int CurrentWeaponIndex => currentIndex;
    public WeaponState CurrentWeapon => states[currentIndex];

    public WeaponState GetWeapon(int index) => states[index];

    private void Awake()
    {
        Instance = this;

        states = new WeaponState[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
            states[i] = new WeaponState(weapons[i], i);

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].Owned)
            {
                currentIndex = i;
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        PublishAmmo();
    }

    private void Update()
    {
        if (GameState.InputBlocked) return;

        GameInput input = GameInput.Instance;

        for (int i = 0; i < states.Length && i < GameInput.WeaponSlots; i++)
        {
            if (input.WeaponSlotPressed(i)) SwitchWeapon(i);
        }
        if (input.NextWeaponPressed) CycleWeapon(1);
        if (input.PreviousWeaponPressed) CycleWeapon(-1);

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
        PublishAmmo();
    }

    private void Shoot()
    {
        WeaponState weapon = CurrentWeapon;
        weapon.Ammo--;
        PublishAmmo();

        AudioManager.Instance.PlaySFX(weapon.Definition.shootSound);

        if (!TryGetHit(weapon.Definition.range, out RaycastHit hit)) return;

        EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(weapon.Damage);
            return;
        }

        StartTrigger start = hit.collider.GetComponentInParent<StartTrigger>();
        if (start != null) start.Activate();
    }

    /// <summary>
    /// Lanza el rayo desde el centro de la pantalla y devuelve el impacto más cercano,
    /// ignorando al propio jugador (la cámara en tercera persona queda detrás de él) y los triggers.
    /// </summary>
    private bool TryGetHit(float range, out RaycastHit result)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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

    private void PublishAmmo()
    {
        WeaponState weapon = CurrentWeapon;
        GameEvents.RaiseAmmoChanged(weapon.Ammo, weapon.MagazineSize, isReloading);
    }

    public bool BuyWeapon(int index) => states[index].TryBuy();

    public bool BuyUpgrade(int index, UpgradeType type) => states[index].TryUpgrade(type);
}
