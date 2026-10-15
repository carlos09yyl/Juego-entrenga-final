using UnityEngine;

/// <summary>
/// Las 30 selecciones del juego con los colores de su uniforme (camiseta y pantaloneta).
/// La bandera se carga de Resources/Banderas/{codigo}.png
/// </summary>
public static class Paises
{
    public struct Pais
    {
        public string nombre;
        public string codigo;
        public Color camiseta;
        public Color pantaloneta;
        public Pais(string n, string c, string cam, string pan)
        {
            nombre = n; codigo = c;
            ColorUtility.TryParseHtmlString(cam, out camiseta);
            ColorUtility.TryParseHtmlString(pan, out pantaloneta);
        }
    }

    public static readonly Pais[] Lista =
    {
        new Pais("Colombia",       "co",     "#FCD116", "#1C3F94"),
        new Pais("Argentina",      "ar",     "#75AADB", "#1A1A1A"),
        new Pais("Brasil",         "br",     "#FEDF00", "#1B4FA0"),
        new Pais("Francia",        "fr",     "#1E2A5A", "#FFFFFF"),
        new Pais("Alemania",       "de",     "#FFFFFF", "#1A1A1A"),
        new Pais("España",         "es",     "#C60B1E", "#1E2A5A"),
        new Pais("Inglaterra",     "gb-eng", "#FFFFFF", "#1E2A5A"),
        new Pais("Portugal",       "pt",     "#B5121B", "#0B5D2E"),
        new Pais("Italia",         "it",     "#1F5FAF", "#FFFFFF"),
        new Pais("Países Bajos",   "nl",     "#F36C21", "#1A1A1A"),
        new Pais("Bélgica",        "be",     "#C8102E", "#1A1A1A"),
        new Pais("Uruguay",        "uy",     "#5CBFEB", "#1A1A1A"),
        new Pais("México",         "mx",     "#0B6E4F", "#FFFFFF"),
        new Pais("Estados Unidos", "us",     "#FFFFFF", "#1E2A5A"),
        new Pais("Croacia",        "hr",     "#E21B23", "#FFFFFF"),
        new Pais("Japón",          "jp",     "#0A2A6B", "#FFFFFF"),
        new Pais("Corea del Sur",  "kr",     "#CD2E3A", "#1A1A1A"),
        new Pais("Marruecos",      "ma",     "#C1272D", "#006233"),
        new Pais("Senegal",        "sn",     "#FFFFFF", "#00853F"),
        new Pais("Ecuador",        "ec",     "#FFD100", "#034EA2"),
        new Pais("Perú",           "pe",     "#FFFFFF", "#D91023"),
        new Pais("Chile",          "cl",     "#D52B1E", "#0039A6"),
        new Pais("Paraguay",       "py",     "#D52B1E", "#0038A8"),
        new Pais("Venezuela",      "ve",     "#6E1423", "#FFFFFF"),
        new Pais("Canadá",         "ca",     "#D80621", "#1A1A1A"),
        new Pais("Suiza",          "ch",     "#DA291C", "#FFFFFF"),
        new Pais("Dinamarca",      "dk",     "#C8102E", "#FFFFFF"),
        new Pais("Polonia",        "pl",     "#FFFFFF", "#DC143C"),
        new Pais("Nigeria",        "ng",     "#008751", "#FFFFFF"),
        new Pais("Australia",      "au",     "#FFCD00", "#00843D"),
    };

    const string Clave = "PaisElegido";

    /// <summary>Indice del pais elegido (Colombia por defecto). Queda guardado en el equipo.</summary>
    public static int Elegido
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(Clave, 0), 0, Lista.Length - 1);
        set { PlayerPrefs.SetInt(Clave, value); PlayerPrefs.Save(); }
    }

    public static Pais Actual => Lista[Elegido];

    public static Texture2D Bandera(string codigo) => Resources.Load<Texture2D>("Banderas/" + codigo);

    /// <summary>Pinta camiseta y pantaloneta de un personaje con los colores del pais.</summary>
    public static void Vestir(GameObject personaje, Pais pais, Material materialBase)
    {
        if (personaje == null || materialBase == null) return;
        foreach (var r in personaje.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.name.ToLower();
            Color? color = n.StartsWith("shirt") ? pais.camiseta : n.StartsWith("shorts") ? pais.pantaloneta : (Color?)null;
            if (color == null) continue;
            var m = r.sharedMaterial;
            if (m == null || !m.name.StartsWith("Uniforme"))
            {
                m = new Material(materialBase) { name = "Uniforme" };
                r.sharedMaterial = m;
            }
            m.color = color.Value;
        }
    }
}
