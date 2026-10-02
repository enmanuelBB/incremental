/// <summary>
/// Cargadores de un arma con varios cañones (por ejemplo dos pistolas). Cada cañón tiene su propio cargador y los
/// disparos se turnan: izquierda, derecha, izquierda... Si un cañón se queda sin balas, el otro sigue disparando solo.
/// Lógica pura, sin Unity, para poder probarla.
/// </summary>
public class BarrelMagazines
{
    private readonly int[] ammo;
    private readonly int magazineSize;
    private int next;

    public BarrelMagazines(int barrels, int magazineSize)
    {
        ammo = new int[barrels < 1 ? 1 : barrels];
        this.magazineSize = magazineSize;
        Refill();
    }

    public int Barrels => ammo.Length;
    public int MagazineSize => magazineSize;

    /// <summary>Cañón al que le toca disparar (si no tiene balas, dispara el siguiente que tenga).</summary>
    public int NextBarrel => next;

    public int AmmoOf(int barrel) => ammo[barrel];

    public int Total
    {
        get
        {
            int sum = 0;
            foreach (int a in ammo) sum += a;
            return sum;
        }
    }

    public bool IsEmpty => Total == 0;

    public bool IsFull
    {
        get
        {
            foreach (int a in ammo) if (a < magazineSize) return false;
            return true;
        }
    }

    /// <summary>Gasta una bala del cañón que toca (o del siguiente que tenga) y deja el turno al otro. False si no queda ninguna.</summary>
    public bool TryFire(out int barrel)
    {
        for (int step = 0; step < ammo.Length; step++)
        {
            int candidate = (next + step) % ammo.Length;
            if (ammo[candidate] <= 0) continue;

            ammo[candidate]--;
            barrel = candidate;
            next = (candidate + 1) % ammo.Length;
            return true;
        }

        barrel = -1;
        return false;
    }

    /// <summary>Llena todos los cargadores y empieza otra vez por el primer cañón.</summary>
    public void Refill()
    {
        for (int i = 0; i < ammo.Length; i++) ammo[i] = magazineSize;
        next = 0;
    }

    /// <summary>Copia de las balas de cada cañón, para mostrarlas en el HUD.</summary>
    public int[] Snapshot() => (int[])ammo.Clone();
}
