using System.Text;

/// <summary>Textos del HUD que se arman a partir de datos. Lógica pura para poder probarla.</summary>
public static class HudFormat
{
    /// <summary>9230 → "9.230" (con puntos de miles, sin depender de la cultura del equipo).</summary>
    public static string Money(int amount)
    {
        string digits = (amount < 0 ? -(long)amount : amount).ToString();
        var sb = new StringBuilder();

        if (amount < 0) sb.Append('-');

        for (int i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0) sb.Append('.');
            sb.Append(digits[i]);
        }

        return sb.ToString();
    }

    /// <summary>Primera letra en mayúscula: "pistola" → "Pistola"; "M16" se queda igual.</summary>
    public static string DisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public static string WaveLabel(int wave) => "Oleada " + wave;
}
