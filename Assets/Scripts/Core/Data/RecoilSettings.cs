using System;
using UnityEngine;

/// <summary>Retroceso visual de un cañón en cada disparo. Un arma con dos cañones tiene uno por cañón.</summary>
[Serializable]
public struct RecoilSettings
{
    [Tooltip("Cuánto retrocede el arma entera hacia atrás, en metros")]
    public float kickBack;

    [Tooltip("Cuánto levanta el cañón, en grados")]
    public float kickUp;

    [Tooltip("Cuánto retrocede la corredera (solo se ve de cerca), en metros")]
    public float slideBack;

    [Min(0.01f)]
    [Tooltip("Segundos que tarda el arma en volver a su sitio")]
    public float recoverTime;

    /// <summary>Valores usados cuando el arma no define retroceso para ese cañón.</summary>
    public static RecoilSettings None => new RecoilSettings { recoverTime = 0.1f };
}
