using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Único punto de lectura del input. Las teclas y botones se editan en el asset
/// GameControls (Assets/Settings), así que soporta teclado, mouse y gamepad sin tocar código.
/// </summary>
public class GameInput : MonoBehaviour
{
    public const int WeaponSlots = 4;

    public static GameInput Instance { get; private set; }

    [SerializeField] private InputActionAsset actions;

    private InputAction move, look, lookStick, fire, aim, reload, interact, jump, toggleView, nextWeapon, previousWeapon;
    private readonly InputAction[] weaponSlots = new InputAction[WeaponSlots];

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 LookDelta => look.ReadValue<Vector2>();
    public Vector2 LookStick => lookStick.ReadValue<Vector2>();
    public bool FireHeld => fire.IsPressed();
    public bool FirePressed => fire.WasPressedThisFrame();
    public bool AimHeld => aim.IsPressed();
    public bool ReloadPressed => reload.WasPressedThisFrame();
    public bool InteractPressed => interact.WasPressedThisFrame();
    public bool JumpPressed => jump.WasPressedThisFrame();
    public bool ToggleViewPressed => toggleView.WasPressedThisFrame();
    public bool NextWeaponPressed => nextWeapon.WasPressedThisFrame();
    public bool PreviousWeaponPressed => previousWeapon.WasPressedThisFrame();
    public bool WeaponSlotPressed(int slot) => weaponSlots[slot].WasPressedThisFrame();

    private void Awake()
    {
        Instance = this;

        InputActionMap map = actions.FindActionMap("Player", true);
        move = map.FindAction("Move", true);
        look = map.FindAction("Look", true);
        lookStick = map.FindAction("LookStick", true);
        fire = map.FindAction("Fire", true);
        aim = map.FindAction("Aim", true);
        reload = map.FindAction("Reload", true);
        interact = map.FindAction("Interact", true);
        jump = map.FindAction("Jump", true);
        toggleView = map.FindAction("ToggleView", true);
        nextWeapon = map.FindAction("NextWeapon", true);
        previousWeapon = map.FindAction("PreviousWeapon", true);

        for (int i = 0; i < WeaponSlots; i++)
            weaponSlots[i] = map.FindAction("Weapon" + (i + 1), true);
    }

    private void OnEnable() => actions.FindActionMap("Player", true).Enable();
    private void OnDisable() => actions.FindActionMap("Player", true).Disable();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
