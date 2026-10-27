using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Control del protagonista (Mason).
/// Solo teclado:
///   A / flecha izquierda  -> caminar a la izquierda
///   D / flecha derecha    -> caminar a la derecha
///   Espacio o W           -> saltar
///   J o Ctrl izquierdo    -> disparar
/// </summary>
public class JugadorMason : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6.5f;
    public float fuerzaSalto = 9.5f;

    [Header("Disparo")]
    public GameObject prefabBala;
    public Transform puntoDisparo;
    public float cadencia = 0.22f;

    [Header("Municion")]
    public int municionMaxima = 100;

    [Header("Vida")]
    public int vidaMaxima = 5;
    public float alturaDeMuerte = -8.5f;   // si baja de aqui se cayo al vacio y muere

    [Header("Animaciones")]
    public Sprite[] cuadrosQuieto;
    public Sprite[] cuadrosCorrer;
    public Sprite[] cuadrosSalto;
    public Sprite[] cuadrosDisparo;

    [Header("Sonidos")]
    public AudioClip sonidoDisparo;
    public AudioClip sonidoSalto;
    public AudioClip sonidoDano;

    // ------------------- Datos que consultan otros scripts -------------------
    public int Vida { get { return vida; } }
    public int Municion { get { return municion; } }
    public int MunicionMaxima { get { return municionMaxima; } }
    public bool EstaVivo { get { return vida > 0; } }

    private Rigidbody2D cuerpo;
    private SpriteRenderer dibujo;
    private AudioSource altavoz;
    private Collider2D miCollider;

    private int vida;
    private int municion;
    private float ultimoDisparo;
    private float tiempoPoseDisparo;
    private float tiempoArmaMejorada;

    private bool mirandoDerecha = true;
    private bool enSuelo;

    // control interno de la animacion
    private Sprite[] listaActual;
    private int indice;
    private float tiempoFrame;

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        dibujo = GetComponent<SpriteRenderer>();
        altavoz = GetComponent<AudioSource>();
        miCollider = GetComponent<Collider2D>();
        vida = vidaMaxima;
        municion = municionMaxima;
    }

    void Update()
    {
        // Si el nivel termino (muerte o victoria) el jugador ya no responde
        if (GestorJuego.instancia != null && GestorJuego.instancia.terminado) return;
        if (vida <= 0) return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        // ---------------- 1. Leer el teclado ----------------
        float horizontal = 0f;
        if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) horizontal -= 1f;
        if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) horizontal += 1f;

        // ---------------- 2. Mirar hacia donde camina ----------------
        if (horizontal < 0f) mirandoDerecha = false;
        if (horizontal > 0f) mirandoDerecha = true;
        transform.localScale = new Vector3(mirandoDerecha ? 1f : -1f, 1f, 1f);

        // ---------------- 3. Saber si esta tocando el suelo ----------------
        enSuelo = HaySuelo();

        // ---------------- 4. Saltar ----------------
        if ((teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame) && enSuelo)
        {
            cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, fuerzaSalto);
            Sonar(sonidoSalto);
        }

        // ---------------- 5. Disparar (solo con municion) ----------------
        if ((teclado.jKey.isPressed || teclado.leftCtrlKey.isPressed) && municion > 0 && Time.time >= ultimoDisparo + cadencia)
            Disparar();

        // ---------------- 6. Mover ----------------
        cuerpo.linearVelocity = new Vector2(horizontal * velocidad, cuerpo.linearVelocity.y);

        // ---------------- 7. Temporizadores ----------------
        if (tiempoPoseDisparo > 0f) tiempoPoseDisparo -= Time.deltaTime;
        if (tiempoArmaMejorada > 0f) tiempoArmaMejorada -= Time.deltaTime;

        // ---------------- 8. Animacion ----------------
        ElegirAnimacion(horizontal);

        // ---------------- 9. Si se cae al vacio, muerte automatica ----------------
        if (transform.position.y < alturaDeMuerte) MorirDeCaida();
    }

    // Comprueba si hay algo solido justo debajo de los pies
    private bool HaySuelo()
    {
        Vector2 centro = new Vector2(transform.position.x, transform.position.y - 0.6f);
        Collider2D[] choques = Physics2D.OverlapBoxAll(centro, new Vector2(0.5f, 0.15f), 0f);

        foreach (Collider2D c in choques)
        {
            if (c == miCollider) continue;
            if (c.isTrigger) continue;
            return true;
        }
        return false;
    }

    private void Disparar()
    {
        ultimoDisparo = Time.time;
        tiempoPoseDisparo = 0.16f;
        municion--;

        Vector2 direccion = mirandoDerecha ? Vector2.right : Vector2.left;

        CrearBala(direccion);

        // Si cogio una caja de municion, dispara en abanico de tres balas
        if (tiempoArmaMejorada > 0f)
        {
            CrearBala((direccion + Vector2.up * 0.45f).normalized);
            CrearBala((direccion + Vector2.down * 0.45f).normalized);
        }

        Sonar(sonidoDisparo);
    }

    private void CrearBala(Vector2 direccion)
    {
        if (prefabBala == null || puntoDisparo == null) return;

        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.identity);
        Bala script = bala.GetComponent<Bala>();
        if (script != null) script.Configurar(direccion, true, 1);
    }

    private void ElegirAnimacion(float horizontal)
    {
        Sprite[] lista;
        if (!enSuelo) lista = cuadrosSalto;
        else if (tiempoPoseDisparo > 0f) lista = cuadrosDisparo;
        else if (horizontal != 0f) lista = cuadrosCorrer;
        else lista = cuadrosQuieto;

        if (lista == null || lista.Length == 0) return;
        if (lista != listaActual)
        {
            listaActual = lista;
            indice = 0;
            tiempoFrame = 0f;
        }

        tiempoFrame += Time.deltaTime;
        if (tiempoFrame >= 0.1f)
        {
            tiempoFrame -= 0.1f;
            indice = (indice + 1) % lista.Length;
        }

        dibujo.sprite = lista[indice];
    }

    // ---------------- Cosas que llaman otros scripts ----------------

    public void RecibirDano(int cantidad)
    {
        if (vida <= 0) return;

        vida -= cantidad;
        Sonar(sonidoDano);

        if (vida <= 0)
        {
            vida = 0;
            cuerpo.linearVelocity = Vector2.zero;
            if (GestorJuego.instancia != null) GestorJuego.instancia.GameOver();
        }
    }

    // Caer al vacio mata en el acto (no gasta una vida, se acaba la partida)
    private void MorirDeCaida()
    {
        if (vida <= 0) return;

        vida = 0;
        cuerpo.linearVelocity = Vector2.zero;
        Sonar(sonidoDano);

        if (GestorJuego.instancia != null) GestorJuego.instancia.GameOver();
    }

    public void Curar(int cantidad)
    {
        vida = Mathf.Min(vidaMaxima, vida + cantidad);
    }

    public void Recargar(int cantidad)
    {
        municion = Mathf.Min(municionMaxima, municion + cantidad);
    }

    public void MejorarArma(float segundos)
    {
        tiempoArmaMejorada = segundos;
    }

    private void Sonar(AudioClip clip)
    {
        if (altavoz != null && clip != null) altavoz.PlayOneShot(clip);
    }
}
