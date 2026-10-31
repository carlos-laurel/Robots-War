using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Control del protagonista (Mason).
/// Solo teclado:
///   A / flecha izquierda  -> caminar a la izquierda
///   D / flecha derecha    -> caminar a la derecha
///   Espacio               -> saltar
///   W / flecha arriba     -> apuntar hacia arriba
///   S / flecha abajo      -> apuntar hacia abajo
///   J o Ctrl izquierdo    -> disparar
/// </summary>
public class JugadorMason : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6.5f;
    public float fuerzaSalto = 9.5f;
    public float tiempoCoyote = 0.12f;        // margen para saltar justo despues de dejar el borde
    public float tiempoBufferSalto = 0.12f;   // margen si se pulsa salto un poco antes de tocar el suelo

    [Header("Disparo")]
    public GameObject prefabBala;
    public Transform puntoDisparo;            // referencia de editor: el origen real se calcula con la direccion
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

    private float tiempoSinSuelo;
    private float tiempoSaltoPedido;

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

        // El jugador no tiene rozamiento: asi no se queda pegado a las paredes,
        // a las cajas ni a los bordes de las plataformas al saltar.
        if (miCollider != null)
        {
            PhysicsMaterial2D sinRoce = new PhysicsMaterial2D("MasonSinRozamiento");
            sinRoce.friction = 0f;
            sinRoce.bounciness = 0f;
            miCollider.sharedMaterial = sinRoce;
        }
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
        if (enSuelo) tiempoSinSuelo = 0f;
        else tiempoSinSuelo += Time.deltaTime;

        // ---------------- 4. Saltar ----------------
        if (teclado.spaceKey.wasPressedThisFrame) tiempoSaltoPedido = tiempoBufferSalto;
        else if (tiempoSaltoPedido > 0f) tiempoSaltoPedido -= Time.deltaTime;

        if (tiempoSaltoPedido > 0f && tiempoSinSuelo <= tiempoCoyote)
        {
            tiempoSaltoPedido = 0f;
            tiempoSinSuelo = tiempoCoyote + 1f;   // ya no puede volver a saltar en el aire
            cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, fuerzaSalto);
            Sonar(sonidoSalto);
        }

        // ---------------- 5. Disparar (solo con municion) ----------------
        if ((teclado.jKey.isPressed || teclado.leftCtrlKey.isPressed) && municion > 0 && Time.time >= ultimoDisparo + cadencia)
            Disparar(teclado);

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

    // Comprueba si hay una superficie solida justo debajo de los pies.
    // Solo cuenta como suelo lo que tiene la cara hacia arriba: las paredes
    // y las cajas pegadas de lado ya no cuentan y el salto no se queda trabado.
    private bool HaySuelo()
    {
        Vector2 centro = new Vector2(transform.position.x, transform.position.y - 0.5f);
        Vector2 tamano = new Vector2(0.4f, 0.1f);

        RaycastHit2D[] golpes = Physics2D.BoxCastAll(centro, tamano, 0f, Vector2.down, 0.14f);

        foreach (RaycastHit2D golpe in golpes)
        {
            if (golpe.collider == null) continue;
            if (golpe.collider.isTrigger) continue;
            if (golpe.collider == miCollider) continue;
            if (golpe.rigidbody == cuerpo) continue;
            if (golpe.normal.y < 0.5f) continue;   // una pared no es suelo

            return true;
        }

        return false;
    }

    // Direccion del disparo: arriba, abajo, izquierda o derecha
    private Vector2 DireccionApuntado(Keyboard teclado)
    {
        bool arriba = teclado.wKey.isPressed || teclado.upArrowKey.isPressed;
        bool abajo = teclado.sKey.isPressed || teclado.downArrowKey.isPressed;

        if (arriba) return Vector2.up;
        if (abajo) return Vector2.down;

        return mirandoDerecha ? Vector2.right : Vector2.left;
    }

    private void Disparar(Keyboard teclado)
    {
        ultimoDisparo = Time.time;
        tiempoPoseDisparo = 0.16f;
        municion--;

        Vector2 direccion = DireccionApuntado(teclado);

        CrearBala(direccion);

        // Si cogio una caja de municion, dispara en abanico de tres balas
        if (tiempoArmaMejorada > 0f)
        {
            Vector2 lado = new Vector2(-direccion.y, direccion.x);
            CrearBala((direccion + lado * 0.45f).normalized);
            CrearBala((direccion - lado * 0.45f).normalized);
        }

        Sonar(sonidoDisparo);
    }

    private void CrearBala(Vector2 direccion)
    {
        if (prefabBala == null) return;

        // La bala nace delante de Mason en la direccion apuntada
        Vector2 origen = (Vector2)transform.position + direccion * 0.7f;
        float giro = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        GameObject bala = Instantiate(prefabBala, origen, Quaternion.Euler(0f, 0f, giro));
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
        //return;

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
