using System.Collections.Generic;
using UnityEngine;

public static class ScoreData
{
    public static int WhiteScore = 0;
    public static int BlackScore = 0;

    /// <summary>Puntos de la partida actual.</summary>
    public static int Puntos = 0;

    /// <summary>Mejor puntaje que habia antes de empezar esta partida (para saber si hubo record).</summary>
    public static int MejorAlEmpezar = 0;

    /// <summary>Como termino cada tiro: Gol, Atajada, Afuera, Palo, Fallo o Barrera.</summary>
    public static readonly List<string> Jugadas = new List<string>();

    /// <summary>Mejor puntaje del jugador, guardado en el equipo.</summary>
    public static int MejorPuntaje
    {
        get => PlayerPrefs.GetInt("MejorPuntaje", 0);
        set { PlayerPrefs.SetInt("MejorPuntaje", value); PlayerPrefs.Save(); }
    }

    /// <summary>Deja todo listo para una partida nueva.</summary>
    public static void NuevaPartida()
    {
        WhiteScore = 0;
        BlackScore = 0;
        Puntos = 0;
        MejorAlEmpezar = MejorPuntaje;
        Jugadas.Clear();
    }
}
