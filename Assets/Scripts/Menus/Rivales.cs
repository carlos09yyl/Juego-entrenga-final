using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ranking local contra rivales simulados. Los nombres son inventados y cambian en cada visita;
/// el jugador entra con su puntaje real y la bandera de su seleccion.
/// </summary>
public static class Rivales
{
    public struct Fila
    {
        public string nombre;
        public string codigo;
        public int puntos;
        public bool esJugador;
    }

    static readonly string[] Nombres =
    {
        "Mateo", "Santiago", "Thiago", "Lucas", "Joaquín", "Emiliano", "Kenji", "Luca", "Noah", "Ibrahim",
        "Rafael", "Tomás", "Aarón", "Bruno", "Iker", "Hugo", "Dante", "Malik", "Yusuf", "Felipe",
        "Andrés", "Oliver", "Enzo", "Gael", "Jonas", "Kofi", "Samuel", "Nico", "Elías", "Tobías",
    };

    static readonly string[] Apellidos =
    {
        "Rivera", "Kowalski", "Okafor", "Moreau", "Tanaka", "Ferreira", "Lindqvist", "Navarro", "Castaño", "Bianchi",
        "Novak", "Mendoza", "Herrera", "Duarte", "Vargas", "Okoye", "Becker", "Alvarado", "Quintero", "Petrov",
        "Haddad", "Ortega", "Cárdenas", "Lemaire", "Jensen", "Molina", "Arango", "Sato", "Varela", "Brandt",
    };

    /// <summary>
    /// Arma la tabla con <paramref name="total"/> filas (rivales + jugador), ordenada de mayor a menor.
    /// <paramref name="posicionJugador"/> devuelve el indice (0 = primero) donde quedo el jugador.
    /// </summary>
    public static List<Fila> Tabla(int puntosJugador, Paises.Pais pais, out int posicionJugador, int total = 10)
    {
        var azar = new System.Random(System.Environment.TickCount);
        var filas = new List<Fila>();
        var usados = new HashSet<string>();

        // el lider rival anda entre 1.400 y 1.900 puntos: un partido muy bueno (con rosca,
        // esquinas y racha) lo puede superar, asi que llegar al #1 es posible pero dificil
        int techo = 1400 + azar.Next(0, 101) * 5;
        for (int i = 0; i < total - 1; i++)
        {
            string nombre;
            do nombre = Nombres[azar.Next(Nombres.Length)] + " " + Apellidos[azar.Next(Apellidos.Length)];
            while (!usados.Add(nombre));

            float baja = 1f - i * 0.1f + (float)(azar.NextDouble() * 0.06 - 0.03);
            int puntos = Mathf.Max(50, Mathf.RoundToInt(techo * baja / 5f) * 5);
            var otroPais = Paises.Lista[azar.Next(Paises.Lista.Length)];
            filas.Add(new Fila { nombre = nombre, codigo = otroPais.codigo, puntos = puntos });
        }

        filas.Add(new Fila { nombre = "TÚ", codigo = pais.codigo, puntos = puntosJugador, esJugador = true });
        // en caso de empate el jugador queda por encima
        filas.Sort((a, b) => b.puntos != a.puntos ? b.puntos.CompareTo(a.puntos) : b.esJugador.CompareTo(a.esJugador));
        posicionJugador = filas.FindIndex(f => f.esJugador);
        return filas;
    }
}
