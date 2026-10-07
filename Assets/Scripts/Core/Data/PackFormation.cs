using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cómo sale una manada: en filas de frente a la base, cada una con un ancho al azar, separados <c>spacing</c> metros
/// y todo centrado en el punto de la manada (cada fila centrada por su cuenta). Lógica pura.
/// </summary>
public static class PackFormation
{
    /// <summary>
    /// Cuántos enemigos lleva cada fila: <paramref name="rows"/> filas de <paramref name="min"/> a <paramref name="max"/>
    /// (al azar con <paramref name="random01"/>), sin pasar de los <paramref name="left"/> que quedan en la oleada.
    /// </summary>
    public static int[] RowWidths(int rows, int min, int max, int left, Func<float> random01)
    {
        rows = Mathf.Max(1, rows);
        min = Mathf.Max(1, min);
        max = Mathf.Max(min, max);

        var widths = new List<int>(rows);
        for (int r = 0; r < rows && left > 0; r++)
        {
            int width = Mathf.Min(max, min + (int)(random01() * (max - min + 1)));
            width = Mathf.Min(width, left);
            widths.Add(width);
            left -= width;
        }

        return widths.ToArray();
    }

    /// <summary>
    /// Posición del enemigo <paramref name="index"/> de la manada, relativa a su centro: x = de lado (derecha
    /// positiva), y = hacia atrás (la primera fila es la más cercana a la base).
    /// </summary>
    public static Vector2 Slot(int index, int[] rowWidths, float spacing)
    {
        int row = 0;
        while (row < rowWidths.Length - 1 && index >= rowWidths[row])
        {
            index -= rowWidths[row];
            row++;
        }

        return new Vector2(
            (index - (rowWidths[row] - 1) * 0.5f) * spacing,
            (row - (rowWidths.Length - 1) * 0.5f) * spacing);
    }
}
