using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central controller for the 5-attempt penalty system.
///
/// Enter       → open / close target selection
/// WASD/Arrows → navigate 3×3 grid
/// Space/Click → confirm shot
/// </summary>
public class PenaltyTargetSelector : MonoBehaviour
{
    // ── Grid constants ────────────────────────────────────────────────
    private const int Columns = 3;
    private const int Rows    = 3;

    // ── Inspector ─────────────────────────────────────────────────────

    [Header("Goal target points (GoalArea children, row-major order)")]
    public Transform[] targetPoints;
    public Transform   goalArea;

    [Header("References")]
    public BallController         ballController;
    public GoalkeeperController   goalkeeperController;
    public PlayerKickController   playerKickController;
    public ParticleSystem         goalParticles;
    public TargetSelectorUI       selectorUI;
    public Transform              playerTransform;
    public PenaltyResetController resetController;
    public GameEndUI              gameEndUI;

    [Header("Scene Transition")]
    [Tooltip("Scene to load when all attempts are done if GameEndUI is not assigned.")]
    public string scoreSceneName = "ScoreScene";

    [Header("3D World indicator")]
    public Transform worldIndicator;

    [Header("Attempt Configuration  (index 0 = attempt 1)")]
    public int maxAttempts = 5;

    [Tooltip("Ball launch speed for each attempt.")]
    public float[] ballSpeedsPerAttempt = { 4f, 5f, 6f, 6.5f, 7f };

    [Tooltip("Goalkeeper move speed for each attempt.")]
    public float[] keeperSpeedsPerAttempt = { 2f, 2.5f, 3.5f, 4.5f, 5f };

    [Tooltip("Probability [0-1] that the keeper guesses the correct column per attempt.")]
    public float[] keeperAccuracyPerAttempt = { 0.2f, 0.3f, 0.4f, 0.45f, 0.5f };

    [Header("Timing")]
    [Tooltip("Seconds to wait for the kick animation before forcing ball launch.")]
    public float shotAnimDelay = 1.2f;

    [Header("Ball Out-of-Play Timeout")]
    [Tooltip("Seconds after ball launch before forcing a miss if no trigger fired.")]
    public float ballTimeoutSeconds = 5f;

    [Header("Dynamic Speed (based on selection time)")]
    [Tooltip("Select within this many seconds → full max speed.")]
    public float fastSelectionThreshold = 1.5f;
    [Tooltip("Take this many seconds or more → minimum speed.")]
    public float slowSelectionThreshold = 5f;
    [Tooltip("Minimum ball speed regardless of selection time.")]
    public float minBallSpeed = 3f;

    [Header("Potencia (mantener Espacio o clic)")]
    [Tooltip("Segundos que tarda la barra en llenarse. Si se sigue manteniendo, baja y vuelve a subir.")]
    public float tiempoCargaCompleta = 0.9f;
    [Tooltip("Zona ideal de la barra: entre estos dos valores el tiro va fuerte y exacto.")]
    public float potenciaPerfectaMin = 0.62f;
    public float potenciaPerfectaMax = 0.82f;
    [Tooltip("Con la barra al maximo el tiro sube esta cantidad de metros (se puede ir por encima).")]
    public float errorAlturaMaximo = 1.4f;
    [Tooltip("Desvio lateral maximo (m) cuando se pasa o se queda corto de potencia.")]
    public float errorLateralMaximo = 0.7f;
    [Tooltip("Efecto maximo del tiro en m/s² (curva lateral).")]
    public float efectoMaximo = 4f;
    public BarraPotencia barraPotencia;

    [Header("Tiro libre")]
    [Tooltip("Barrera de dos jugadores que cambia de posicion en cada intento.")]
    public Barrera barrera;

    /// <summary>Avisa cuando termina cada tiro (true = gol). Lo escuchan el publico y la celebracion.</summary>
    public static event System.Action<bool> TiroTerminado;

    [Header("Dificultad por racha (indice = goles seguidos, de 0 a 4 o mas)")]
    [Tooltip("Probabilidad de que el arquero adivine el lado del tiro.")]
    public float[] precisionPorRacha = { 0.35f, 0.5f, 0.65f, 0.8f, 0.9f };
    [Tooltip("Si adivina el lado, probabilidad de adivinar tambien la altura.")]
    public float[] alturaCorrectaPorRacha = { 0.3f, 0.45f, 0.6f, 0.75f, 0.85f };
    [Tooltip("Segundos que tarda en reaccionar despues de la patada.")]
    public float[] reaccionPorRacha = { 0.18f, 0.13f, 0.09f, 0.06f, 0.04f };
    [Tooltip("Velocidad con la que se desplaza ademas de la animacion.")]
    public float[] velocidadArqueroPorRacha = { 3.5f, 4.5f, 5.5f, 6.5f, 7.5f };
    [Tooltip("Cuanto puede elevarse en los tiros altos.")]
    public float[] elevacionPorRacha = { 1.0f, 1.1f, 1.25f, 1.4f, 1.55f };

    [Header("Puntos")]
    public int puntosGol = 100;
    public int bonusEsquina = 50;
    public int bonusPotenciaPerfecta = 30;
    public int bonusRoscaPorNivel = 25;
    [Tooltip("Cada gol seguido suma esto al multiplicador (x1, x1.5, x2...).")]
    public float multiplicadorPorRacha = 0.5f;

    [Header("Reaccion del arquero (segundos despues de la patada, por intento)")]
    public float[] keeperReactionDelayPerAttempt = { 0.2f, 0.16f, 0.12f, 0.09f, 0.06f };

    [Header("Boca del arco (para saber cuando el tiro se fue afuera)")]
    [Tooltip("Esquina inferior izquierda de la boca del arco (x, y) en el mundo.")]
    public Vector3 bocaArcoMin;
    [Tooltip("Esquina superior derecha de la boca del arco (x, y) en el mundo.")]
    public Vector3 bocaArcoMax;
    [Tooltip("Z de la linea de gol. El arco esta hacia +Z.")]
    public float lineaArcoZ;

    // ── Turn state machine ────────────────────────────────────────────
    // The game is always in exactly one of these phases.
    private enum TurnPhase
    {
        WaitingForInput,   // player can press Enter to open the grid
        Selecting,         // grid is open; player navigates and confirms
        BallInFlight,      // ball has been launched; waiting for trigger or timeout
        ResettingForNext,  // reset coroutine running; all input locked
        GameOver           // no more turns
    }

    private TurnPhase phase = TurnPhase.WaitingForInput;

    // ── Runtime state ─────────────────────────────────────────────────
    private int   selectedIndex     = 4;
    private int   currentAttempt    = 0;   // 0-based
    private int   goalsScored       = 0;
    private float reactionAccuracy  = 0.2f;
    private float selectionStartTime = 0f;
    private float currentMaxBallSpeed = 6f;

    private Transform pendingTarget;
    private CelebracionGol celebracion;
    private CamaraTiro camaraTiro;
    private bool ultimoFueGol;
    private Vector3   pendingAim;
    private float     pendingSpeed;
    private float     pendingEffect;
    private bool      cargando;
    private float     tiempoCarga;
    private float     potencia;
    private bool      fueraMarcado;
    private float     tiempoQuieto;
    private int       efectoNivel;      // -2 mucha curva a la izquierda ... +2 mucha curva a la derecha
    private bool      tocoBarrera;
    private int       racha;            // goles seguidos
    private int       puntos;
    private int       tiroIndice;
    private float     tiroPotencia;
    private int       tiroEfecto;
    private Coroutine fallbackCoroutine;
    private LineRenderer lineaTiro;
    private Coroutine ballTimeoutCoroutine;
    private Coroutine resetCoroutine;

    // Player reset data (fallback when no PenaltyResetController is wired)
    private Vector3    playerStartPos;
    private Quaternion playerStartRot;

    private int Row => selectedIndex / Columns;
    private int Col => selectedIndex % Columns;

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if ((targetPoints == null || targetPoints.Length == 0) && goalArea != null)
        {
            int count = goalArea.childCount;
            targetPoints = new Transform[count];
            for (int i = 0; i < count; i++)
                targetPoints[i] = goalArea.GetChild(i);
        }

        if (playerKickController != null)
            playerKickController.OnKickImpact += OnAnimationKickImpact;

        if (barrera != null) barrera.OnGolpe += OnBarreraGolpeada;

        // celebracion nueva del gol (confeti + chispas); el sistema de particulas viejo se apaga
        celebracion = GetComponent<CelebracionGol>();
        if (celebracion == null) celebracion = gameObject.AddComponent<CelebracionGol>();
        celebracion.bocaMin = bocaArcoMin;
        celebracion.bocaMax = bocaArcoMax;
        if (goalParticles != null)
        {
            var r = goalParticles.GetComponent<ParticleSystemRenderer>();
            if (celebracion.materialBase == null && r != null) celebracion.materialBase = r.sharedMaterial;
            goalParticles.gameObject.SetActive(false);
        }

        // camara que acompaña el tiro y repite los goles (va en la camara que se ve en pantalla)
        Camera camJuego = null;
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.isActiveAndEnabled && (camJuego == null || c.depth > camJuego.depth)) camJuego = c;
        if (camJuego != null)
        {
            camaraTiro = camJuego.GetComponent<CamaraTiro>();
            if (camaraTiro == null) camaraTiro = camJuego.gameObject.AddComponent<CamaraTiro>();
            camaraTiro.balon = ballController != null ? ballController.transform : null;
            camaraTiro.arquero = goalkeeperController != null ? goalkeeperController.transform : null;
            camaraTiro.pateador = playerTransform;
            camaraTiro.bocaMin = bocaArcoMin;
            camaraTiro.bocaMax = bocaArcoMax;
        }

        SetWorldIndicator(false, Vector3.zero);
    }

    void Start()
    {
        ScoreData.NuevaPartida();

        if (playerTransform != null && resetController == null)
        {
            playerStartPos = playerTransform.position;
            playerStartRot = playerTransform.rotation;
        }

        ApplyAttemptSettings();
        phase = TurnPhase.WaitingForInput;
        selectorUI?.ShowIdleHint();
    }

    void OnDestroy()
    {
        if (playerKickController != null)
            playerKickController.OnKickImpact -= OnAnimationKickImpact;
    }

    void Update()
    {
        if (PausaUI.Pausado) return;
        switch (phase)
        {
            case TurnPhase.WaitingForInput:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OpenSelectionGrid();
                break;

            case TurnPhase.Selecting:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    CloseSelectionGrid();
                    break;
                }
                HandleNavigation();
                CambiarEfecto();
                ActualizarCarga();
                ActualizarLinea();
                break;

            case TurnPhase.BallInFlight:
                ComprobarFuera();
                break;

            // BallInFlight, ResettingForNext, GameOver → no input accepted
        }
    }

    // ── Per-attempt configuration ─────────────────────────────────────

    /// <summary>Applies speeds / accuracy for currentAttempt and refreshes the always-visible HUD.</summary>
    void ApplyAttemptSettings()
    {
        int idx = Mathf.Clamp(currentAttempt, 0, maxAttempts - 1);

        if (ballSpeedsPerAttempt.Length > idx)
            currentMaxBallSpeed = ballSpeedsPerAttempt[idx];

        // el arquero depende de la racha: entre mas goles seguidos, mas dificil
        int nivel = NivelArquero;
        reactionAccuracy = Valor(precisionPorRacha, nivel, 0.5f);
        if (goalkeeperController != null)
        {
            goalkeeperController.moveSpeed = Valor(velocidadArqueroPorRacha, nivel, 4f);
            goalkeeperController.elevacionMaxima = Valor(elevacionPorRacha, nivel, 1f);
        }

        // la barrera cambia de lugar y el arquero se corre al lado que queda libre
        if (barrera != null)
        {
            barrera.Reubicar();
            goalkeeperController?.Acomodar(barrera.DesplazamientoArquero());
        }

        // Always update the persistent counters (attempt + goals) — these should never disappear.
        RefreshPersistentHUD();

        Debug.Log($"[PTS] Intento {currentAttempt + 1}/{maxAttempts} " +
                  $"| maxVel={currentMaxBallSpeed} | portero={goalkeeperController?.moveSpeed} " +
                  $"| precisión={reactionAccuracy:P0} | racha={racha} (nivel {NivelArquero}) | puntos={puntos}");
    }

    int NivelArquero => Mathf.Clamp(racha, 0, 4);

    static float Valor(float[] tabla, int i, float porDefecto)
    {
        if (tabla == null || tabla.Length == 0) return porDefecto;
        return tabla[Mathf.Clamp(i, 0, tabla.Length - 1)];
    }

    /// <summary>
    /// Updates only the always-visible attempt / goals counters.
    /// Safe to call at any time — these labels must never be hidden during gameplay.
    /// </summary>
    void RefreshPersistentHUD()
    {
        selectorUI?.UpdatePuntos(puntos, racha);
        selectorUI?.UpdateAttemptDisplay(currentAttempt + 1, maxAttempts, goalsScored);
    }

    // ── Selection grid ────────────────────────────────────────────────

    void OpenSelectionGrid()
    {
        SonidosPenales.Reproducir("silbato", 0.5f);       // el arbitro da la orden
        phase = TurnPhase.Selecting;
        selectionStartTime = Time.time;
        selectedIndex = 4;
        selectorUI?.ShowUI(true);
        selectorUI?.UpdateSelection(selectedIndex, targetPoints[selectedIndex].name);
        selectorUI?.UpdateEfecto(efectoNivel);
        SetWorldIndicator(true, targetPoints[selectedIndex].position);
    }

    /// <summary>Q y E eligen el efecto: hacia donde se curva el balon y cuanto.</summary>
    void CambiarEfecto()
    {
        int antes = efectoNivel;
        if (Input.GetKeyDown(KeyCode.Q)) efectoNivel = Mathf.Max(-2, efectoNivel - 1);
        if (Input.GetKeyDown(KeyCode.E)) efectoNivel = Mathf.Min(2, efectoNivel + 1);
        if (efectoNivel != antes) selectorUI?.UpdateEfecto(efectoNivel);
    }

    void OnBarreraGolpeada()
    {
        if (phase != TurnPhase.BallInFlight || tocoBarrera) return;
        tocoBarrera = true;
        SonidosPenales.Reproducir("atajada", 0.7f);
    }

    void CloseSelectionGrid()
    {
        OcultarLinea();
        cargando = false;
        barraPotencia?.Mostrar(false);
        phase = TurnPhase.WaitingForInput;
        selectorUI?.ShowUI(false);
        SetWorldIndicator(false, Vector3.zero);
        selectorUI?.ShowIdleHint();
    }

    // ── Navigation ────────────────────────────────────────────────────

    void HandleNavigation()
    {
        int newRow = Row;
        int newCol = Col;

        if      (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    newRow = Mathf.Max(0, Row - 1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  newRow = Mathf.Min(Rows - 1, Row + 1);

        if      (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  newCol = Mathf.Max(0, Col - 1);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) newCol = Mathf.Min(Columns - 1, Col + 1);

        int newIndex = newRow * Columns + newCol;
        if (newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            selectorUI?.UpdateSelection(selectedIndex, targetPoints[selectedIndex].name);
            SetWorldIndicator(true, targetPoints[selectedIndex].position);
        }
    }

    // ── Shot ──────────────────────────────────────────────────────────

    // ── Potencia ──────────────────────────────────────────────────────

    /// <summary>Mantener Espacio (o clic) llena la barra; al soltar se patea con esa potencia.</summary>
    void ActualizarCarga()
    {
        bool empezar = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        bool sostenido = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

        if (!cargando)
        {
            if (!empezar) return;
            cargando = true;
            tiempoCarga = 0f;
            barraPotencia?.Mostrar(true);
        }

        tiempoCarga += Time.deltaTime;
        potencia = Mathf.PingPong(tiempoCarga / Mathf.Max(0.1f, tiempoCargaCompleta), 1f);
        barraPotencia?.Actualizar(potencia, potenciaPerfectaMin, potenciaPerfectaMax);

        if (!sostenido)
        {
            cargando = false;
            barraPotencia?.Soltar(potencia);
            ConfirmShot(potencia);
        }
    }

    // ── Shot ──────────────────────────────────────────────────────────

    // ── Linea de tiro ─────────────────────────────────────────────────

    /// <summary>
    /// Dibuja el comienzo de la trayectoria (con la curva elegida) para que el jugador
    /// aprenda como se mueve el balon. Solo muestra una parte: el final hay que imaginarlo.
    /// </summary>
    void ActualizarLinea()
    {
        if (ballController == null || targetPoints == null) return;
        if (phase != TurnPhase.Selecting || !Opciones.LineaTiro) { OcultarLinea(); return; }   // ya se pateo en este mismo cuadro
        if (lineaTiro == null)
        {
            var go = new GameObject("Linea de Tiro");
            lineaTiro = go.AddComponent<LineRenderer>();
            lineaTiro.material = new Material(Shader.Find("Sprites/Default"));
            lineaTiro.widthCurve = AnimationCurve.Linear(0f, 0.07f, 1f, 0.02f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.77f, 0.19f), 1f) },
                      new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            lineaTiro.colorGradient = g;
            lineaTiro.numCapVertices = 4;
            lineaTiro.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineaTiro.receiveShadows = false;
        }
        lineaTiro.enabled = true;
        float efecto = (efectoNivel / 2f) * efectoMaximo;
        var puntos = ballController.Trayectoria(targetPoints[selectedIndex].position, currentMaxBallSpeed, efecto, 24, 0.45f);
        lineaTiro.positionCount = puntos.Length;
        lineaTiro.SetPositions(puntos);
    }

    void OcultarLinea()
    {
        if (lineaTiro != null) lineaTiro.enabled = false;
    }

    void ConfirmShot(float p)
    {
        OcultarLinea();
        // Hide selection grid and world indicator immediately
        selectorUI?.ShowUI(false);
        SetWorldIndicator(false, Vector3.zero);
        // Keep attempt/goals labels visible — do NOT call HideAllUI here
        selectorUI?.HideSelectionOnlyUI();

        Transform punto = targetPoints[selectedIndex];
        Vector3 derechaArco = goalArea != null ? goalArea.right : Vector3.right;

        // velocidad: sube con la potencia hasta la zona ideal y ahi se queda en el maximo
        float t = Mathf.Clamp01(p / Mathf.Max(0.01f, potenciaPerfectaMin));
        float velocidad = Mathf.Lerp(minBallSpeed, currentMaxBallSpeed, t * t * (3f - 2f * t));

        // precision: pasarse de potencia levanta el tiro; quedarse corto lo desvia un poco
        Vector3 apuntado = punto.position;
        if (p > potenciaPerfectaMax)
        {
            float exceso = (p - potenciaPerfectaMax) / Mathf.Max(0.01f, 1f - potenciaPerfectaMax);
            apuntado += Vector3.up * errorAlturaMaximo * exceso;
            apuntado += derechaArco * Random.Range(-1f, 1f) * errorLateralMaximo * exceso;
        }
        else if (p < potenciaPerfectaMin)
        {
            float falta = 1f - p / Mathf.Max(0.01f, potenciaPerfectaMin);
            apuntado += derechaArco * Random.Range(-1f, 1f) * errorLateralMaximo * 0.5f * falta;
        }

        // efecto elegido con Q / E (+ curva hacia la derecha, - hacia la izquierda), con un poquito de variacion
        pendingEffect = (efectoNivel / 2f) * efectoMaximo * Random.Range(0.9f, 1.1f);
        pendingAim = apuntado;
        tiroIndice = selectedIndex;
        tiroPotencia = p;
        tiroEfecto = efectoNivel;
        pendingSpeed = velocidad;

        Debug.Log($"[PTS] Potencia {p:P0} → vel={velocidad:F1} m/s, efecto={pendingEffect:F1}");

        pendingTarget = punto;
        fueraMarcado = false;
        tiempoQuieto = 0f;
        tocoBarrera = false;

        camaraTiro?.EmpezarJugada();

        int keeperIndex = ChooseGoalkeeperTarget(selectedIndex);
        goalkeeperController?.PrepareReaction(targetPoints[keeperIndex]);

        // Transition to BallInFlight BEFORE starting coroutines
        phase = TurnPhase.BallInFlight;

        playerKickController?.PlayKick();
        fallbackCoroutine = StartCoroutine(FallbackLaunch());
    }

    IEnumerator FallbackLaunch()
    {
        yield return new WaitForSeconds(shotAnimDelay);
        LaunchBall();
    }

    // Called by PlayerKickController animation event
    private void OnAnimationKickImpact()
    {
        if (phase == TurnPhase.BallInFlight)
            LaunchBall();
    }

    /// <summary>Launches the ball. Idempotent — safe to call from animation event AND fallback.</summary>
    private void LaunchBall()
    {
        if (pendingTarget == null || ballController == null) return;

        // Cancel the fallback coroutine if the animation event already fired
        if (fallbackCoroutine != null)
        {
            StopCoroutine(fallbackCoroutine);
            fallbackCoroutine = null;
        }

        pendingTarget = null;
        float tiempoVuelo = ballController.LaunchToward(pendingAim, pendingSpeed, pendingEffect);
        camaraTiro?.SeguirBalon();
        SonidosPenales.Reproducir("patada", 0.9f);
        barrera?.Saltar(NivelArquero);

        // el arquero reacciona al golpe, con un pequeño retraso segun el intento
        int idx = Mathf.Clamp(currentAttempt, 0, maxAttempts - 1);
        float reaccion = Valor(reaccionPorRacha, NivelArquero, 0.15f);
        goalkeeperController?.ReaccionarEn(reaccion);
        Debug.Log($"[PTS] Tiro: llega en {tiempoVuelo:F2}s, arquero reacciona en {reaccion:F2}s");

        // Cancel any stale timeout from a previous turn (safety net)
        if (ballTimeoutCoroutine != null)
        {
            StopCoroutine(ballTimeoutCoroutine);
            ballTimeoutCoroutine = null;
        }
        ballTimeoutCoroutine = StartCoroutine(BallTimeout());
    }

    /// <summary>Forces a miss if the ball leaves the field without hitting any trigger.</summary>
    IEnumerator BallTimeout()
    {
        yield return new WaitForSeconds(ballTimeoutSeconds);

        // Only fire if we are still waiting for an outcome
        if (phase == TurnPhase.BallInFlight)
        {
            Debug.Log("[PTS] Timeout: balón fuera sin resultado → fallo");
            Anotar(tocoBarrera ? TargetSelectorUI.Resultado.Barrera : TargetSelectorUI.Resultado.Fallo);
            SonidosPenales.Reproducir("fallo", 0.8f);
            ProcessOutcome(isGoal: false);
        }
    }

    // ── Goalkeeper target selection ───────────────────────────────────

    private int ChooseGoalkeeperTarget(int shotIndex)
    {
        int shotCol = shotIndex % Columns;

        int keeperCol;
        if (Random.value < reactionAccuracy)
        {
            keeperCol = shotCol;
        }
        else
        {
            int rand  = Random.Range(0, Columns - 1);
            keeperCol = rand >= shotCol ? rand + 1 : rand;
        }

        // si adivino el lado, segun la racha tambien adivina la altura
        int shotRow = shotIndex / Columns;
        int keeperRow = (keeperCol == shotCol && Random.value < Valor(alturaCorrectaPorRacha, NivelArquero, 0.4f))
            ? shotRow
            : Random.Range(0, Rows);
        return keeperRow * Columns + keeperCol;
    }

    // ── Outcome callbacks ─────────────────────────────────────────────

    /// <summary>
    /// Mientras el balon vuela revisa si se fue por fuera del arco (por los lados o por encima)
    /// o si rebota en un palo y vuelve a la cancha; asi el intento termina sin esperar el tiempo limite.
    /// </summary>
    void ComprobarFuera()
    {
        if (fueraMarcado || ballController == null || !ballController.Lanzado) return;
        if (bocaArcoMax == bocaArcoMin) return;

        Vector3 p = ballController.transform.position;
        Vector3 v = ballController.rb.linearVelocity;
        bool paso = p.z > lineaArcoZ + 0.4f;
        bool afuera = p.x < bocaArcoMin.x || p.x > bocaArcoMax.x || p.y > bocaArcoMax.y;

        if (paso && afuera)
        {
            MarcarFuera("se fue por fuera");
            return;
        }

        // rebote en un palo o en el arquero que devuelve el balon hacia la cancha
        if (!ballController.EnVuelo && p.z < lineaArcoZ && v.z < -0.5f)
        {
            MarcarFuera("rebote hacia afuera");
            return;
        }

        // balon quieto fuera del arco
        if (!ballController.EnVuelo && v.magnitude < 0.3f)
        {
            tiempoQuieto += Time.deltaTime;
            if (tiempoQuieto > 1f) MarcarFuera("balon quieto");
        }
        else tiempoQuieto = 0f;
    }

    void MarcarFuera(string motivo)
    {
        fueraMarcado = true;
        StartCoroutine(FueraConEspera(motivo));
    }

    IEnumerator FueraConEspera(string motivo)
    {
        yield return new WaitForSeconds(0.6f);      // se alcanza a ver el rebote
        if (phase == TurnPhase.BallInFlight)
        {
            Debug.Log("[PTS] Fallo: " + motivo);
            Anotar(tocoBarrera ? TargetSelectorUI.Resultado.Barrera
                : motivo == "rebote hacia afuera" ? TargetSelectorUI.Resultado.Palo : TargetSelectorUI.Resultado.Afuera);
            SonidosPenales.Reproducir("fallo", 0.8f);
            ProcessOutcome(isGoal: false);
        }
    }

    /// <summary>Called by GoalLineTrigger when the ball crosses the goal line.</summary>
    public void OnGoalScored()
    {
        if (phase != TurnPhase.BallInFlight) return;
        Debug.Log("[PTS] GOL confirmado");
        Anotar(TargetSelectorUI.Resultado.Gol);
        SonidosPenales.Reproducir("red", 0.8f);
        SonidosPenales.Reproducir("gol_publico", 1f, 0.05f);
        camaraTiro?.Gol();
        ProcessOutcome(isGoal: true);
    }

    /// <summary>Called by GoalKeeperSaveTrigger when the keeper contacts the ball.</summary>
    public void OnGoalkeeperSaved()
    {
        if (phase != TurnPhase.BallInFlight) return;
        Debug.Log("[PTS] ATAJADA confirmada");
        Anotar(TargetSelectorUI.Resultado.Atajada);
        SonidosPenales.Reproducir("atajada", 1f);
        SonidosPenales.Reproducir("fallo", 0.8f, 0.35f);
        camaraTiro?.Golpe(0.06f);
        ProcessOutcome(isGoal: false);
    }

    // ── Central outcome handler ───────────────────────────────────────

    /// <summary>
    /// Single entry point for all outcomes (goal, save, timeout).
    /// Only accepted while phase == BallInFlight — the phase check is the gate.
    /// </summary>
    /// <summary>
    /// Puntos del gol: base + bonus (esquina, potencia perfecta, rosca) y todo multiplicado por la racha.
    /// </summary>
    void SumarPuntos()
    {
        int baseGol = puntosGol;
        var detalle = new System.Collections.Generic.List<string>();

        bool esquina = tiroIndice == 0 || tiroIndice == 2 || tiroIndice == 6 || tiroIndice == 8;
        if (esquina) { baseGol += bonusEsquina; detalle.Add("ESQUINA"); }
        if (tiroPotencia >= potenciaPerfectaMin && tiroPotencia <= potenciaPerfectaMax) { baseGol += bonusPotenciaPerfecta; detalle.Add("POTENCIA PERFECTA"); }
        if (tiroEfecto != 0) { baseGol += bonusRoscaPorNivel * Mathf.Abs(tiroEfecto); detalle.Add("CON ROSCA"); }

        float multiplicador = 1f + multiplicadorPorRacha * (racha - 1);
        if (racha >= 2) detalle.Add($"RACHA x{multiplicador:0.#}");

        int ganados = Mathf.RoundToInt(baseGol * multiplicador);
        puntos += ganados;
        ScoreData.Puntos = puntos;
        if (puntos > ScoreData.MejorPuntaje) ScoreData.MejorPuntaje = puntos;

        selectorUI?.MostrarPuntosGanados(ganados, string.Join("  ·  ", detalle));
        Debug.Log($"[PTS] +{ganados} puntos ({string.Join(", ", detalle)}) → total {puntos}, racha {racha}");
    }

    private string ultimaJugada;

    /// <summary>Muestra el resultado en pantalla y lo guarda para la pantalla final.</summary>
    private void Anotar(TargetSelectorUI.Resultado r)
    {
        ultimaJugada = r.ToString();
        selectorUI?.MostrarResultado(r);
    }

    private void ProcessOutcome(bool isGoal)
    {
        // Immediately move out of BallInFlight so no second outcome can sneak in
        phase = TurnPhase.ResettingForNext;

        // Stop the timeout coroutine
        if (ballTimeoutCoroutine != null)
        {
            StopCoroutine(ballTimeoutCoroutine);
            ballTimeoutCoroutine = null;
        }

        goalkeeperController?.Disarm();

        ultimoFueGol = isGoal;
        ScoreData.Jugadas.Add(isGoal ? "Gol" : (ultimaJugada ?? "Fallo"));
        TiroTerminado?.Invoke(isGoal);
        ultimaJugada = null;
        if (!isGoal) camaraTiro?.Golpe(0f);

        if (isGoal)
        {
            goalsScored++;
            ScoreData.WhiteScore = goalsScored;
            racha++;
            SumarPuntos();

            if (celebracion != null)
                celebracion.Lanzar();
        }
        else
        {
            ScoreData.BlackScore++;
            racha = 0;          // al fallar, el arquero se relaja
        }

        // Update HUD immediately so the player sees the result
        currentAttempt++;
        RefreshPersistentHUD();

        Debug.Log($"[PTS] Intento {currentAttempt}/{maxAttempts} completo — Goles: {goalsScored}");

        if (currentAttempt >= maxAttempts)
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            resetCoroutine = StartCoroutine(FinishGame());
        }
        else
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            resetCoroutine = StartCoroutine(ResetForNextAttempt());
        }
    }

    // ── Reset flow ────────────────────────────────────────────────────

    private const float ResetDelay  = 3.5f;
    private const float FinishDelay = 3.5f;

    /// <summary>Espera antes de reiniciar; si fue gol, primero muestra la repeticion en camara lenta.</summary>
    IEnumerator EsperaConRepeticion(float espera)
    {
        if (ultimoFueGol && camaraTiro != null && camaraTiro.repetirGoles && Opciones.Repeticion && camaraTiro.HayJugada)
        {
            yield return new WaitForSeconds(2.1f);          // se ve el GOL y la celebracion
            selectorUI?.HideSelectionOnlyUI();
            yield return camaraTiro.Repetir(v => selectorUI?.MostrarRepeticion(v));
            StopParticles();
        }
        else
        {
            yield return new WaitForSeconds(espera - 0.5f);
            StopParticles();
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator ResetForNextAttempt()
    {
        yield return EsperaConRepeticion(ResetDelay);
        camaraTiro?.Volver();

        // Restore all actor positions
        if (resetController != null)
        {
            resetController.FullReset();
        }
        else
        {
            ballController?.ResetBall();
            goalkeeperController?.ResetPosition();

            if (playerTransform != null)
            {
                playerTransform.position = playerStartPos;
                playerTransform.rotation = playerStartRot;
            }
            playerKickController?.ResetAnimator();
        }

        // Apply new attempt settings BEFORE showing feedback
        ApplyAttemptSettings();

        SetWorldIndicator(false, Vector3.zero);

        // Show "Intento X de Y" flash, then restore idle hint
        selectorUI?.ShowNewAttemptFeedback(currentAttempt + 1, maxAttempts);

        // Allow input again
        phase = TurnPhase.WaitingForInput;

        resetCoroutine = null;
    }

    IEnumerator FinishGame()
    {
        yield return EsperaConRepeticion(FinishDelay);
        camaraTiro?.Volver();

        phase = TurnPhase.GameOver;
        SetWorldIndicator(false, Vector3.zero);
        selectorUI?.HideSelectionOnlyUI();

        if (gameEndUI != null)
        {
            gameEndUI.ShowResults(goalsScored, maxAttempts);
        }
        else
        {
            Debug.Log($"[PTS] GameEndUI no asignado → cargando '{scoreSceneName}'");
            SceneManager.LoadScene(scoreSceneName);
        }

        resetCoroutine = null;
    }

    // ── Restart (called by GameEndUI) ─────────────────────────────────

    /// <summary>Resets the full game to attempt 1. Called from GameEndUI.PlayAgain().</summary>
    public void RestartGame()
    {
        currentAttempt = 0;
        goalsScored    = 0;
        racha          = 0;
        puntos         = 0;
        ScoreData.NuevaPartida();

        resetController?.FullReset();
        ApplyAttemptSettings();

        SetWorldIndicator(false, Vector3.zero);
        phase = TurnPhase.WaitingForInput;
        selectorUI?.ShowIdleHint();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void StopParticles()
    {
        if (goalParticles != null && goalParticles.isPlaying)
            goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetWorldIndicator(bool visible, Vector3 worldPosition)
    {
        if (worldIndicator == null) return;
        worldIndicator.gameObject.SetActive(visible);
        if (visible) worldIndicator.position = worldPosition;
    }
}
