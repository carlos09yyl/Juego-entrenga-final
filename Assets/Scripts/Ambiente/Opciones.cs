using UnityEngine;

/// <summary>Opciones del jugador, guardadas en el equipo.</summary>
public static class Opciones
{
    public static float VolEfectos
    {
        get => PlayerPrefs.GetFloat("VolEfectos", 1f);
        set { PlayerPrefs.SetFloat("VolEfectos", Mathf.Clamp01(value)); SonidosPenales.AplicarVolumen(); }
    }

    public static float VolPublico
    {
        get => PlayerPrefs.GetFloat("VolPublico", 0.7f);
        set { PlayerPrefs.SetFloat("VolPublico", Mathf.Clamp01(value)); SonidosPenales.AplicarVolumen(); }
    }

    /// <summary>Repeticion en camara lenta despues de cada gol.</summary>
    public static bool Repeticion
    {
        get => PlayerPrefs.GetInt("Repeticion", 1) == 1;
        set => PlayerPrefs.SetInt("Repeticion", value ? 1 : 0);
    }

    /// <summary>Linea punteada que muestra por donde va el tiro.</summary>
    public static bool LineaTiro
    {
        get => PlayerPrefs.GetInt("LineaTiro", 1) == 1;
        set => PlayerPrefs.SetInt("LineaTiro", value ? 1 : 0);
    }

    /// <summary>Publico en las tribunas (apagarlo ayuda en celulares sencillos).</summary>
    public static bool PublicoVisible
    {
        get => PlayerPrefs.GetInt("PublicoVisible", 1) == 1;
        set => PlayerPrefs.SetInt("PublicoVisible", value ? 1 : 0);
    }

    public static void Guardar() => PlayerPrefs.Save();
}
