using UnityEngine;

/// <summary>
/// Robot basico de patrulla. Mira al jugador y le dispara cuando esta cerca.
/// Al quedarse sin vida se destruye (antes se ve su animacion de muerte).
/// </summary>
public class RobotEnemigo : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 3;

    [Header("Disparo")]
    public GameObject prefabBala;
    public Transform puntoDisparo;
    public float distanciaDeteccion = 10f;
    public float cadencia = 1.4f;

    [Header("Animaciones")]
    public Sprite[] cuadrosQuieto;
    public Sprite[] cuadrosDisparo;
    public Sprite[] cuadrosMuerte;

    private int vida;
    private float ultimoDisparo;
    private float tiempoPoseDisparo;
    private bool muerto;

    private Transform jugador;
    private SpriteRenderer dibujo;
    private Sprite[] listaActual;
    private int indice;
    private float tiempoFrame;

    void Awake()
    {
        vida = vidaMaxima;
        dibujo = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        JugadorMason encontrado = Object.FindFirstObjectByType<JugadorMason>();
        if (encontrado != null) jugador = encontrado.transform;
    }

    void Update()
    {
        if (muerto || jugador == null) return;
        if (GestorJuego.instancia != null && GestorJuego.instancia.terminado) return;

        // ---------------- Mirar al jugador ----------------
        if (jugador.position.x > transform.position.x) transform.localScale = new Vector3(1f, 1f, 1f);
        else transform.localScale = new Vector3(-1f, 1f, 1f);

        // ---------------- Disparar si esta cerca ----------------
        float distancia = Vector2.Distance(transform.position, jugador.position);
        if (distancia < distanciaDeteccion && Time.time >= ultimoDisparo + cadencia) Disparar();

        if (tiempoPoseDisparo > 0f) tiempoPoseDisparo -= Time.deltaTime;

        ElegirAnimacion();
    }

    private void Disparar()
    {
        ultimoDisparo = Time.time;
        tiempoPoseDisparo = 0.3f;

        if (prefabBala == null || puntoDisparo == null) return;

        Vector2 direccion = (jugador.position - puntoDisparo.position).normalized;

        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.identity);
        Bala script = bala.GetComponent<Bala>();
        if (script != null) script.Configurar(direccion, false, 1);
    }

    private void ElegirAnimacion()
    {
        Sprite[] lista = (tiempoPoseDisparo > 0f) ? cuadrosDisparo : cuadrosQuieto;
        if (lista == null || lista.Length == 0) return;

        if (lista != listaActual)
        {
            listaActual = lista;
            indice = 0;
            tiempoFrame = 0f;
        }

        tiempoFrame += Time.deltaTime;
        if (tiempoFrame >= 0.25f)
        {
            tiempoFrame -= 0.25f;
            indice = (indice + 1) % lista.Length;
        }

        dibujo.sprite = lista[indice];
    }

    public void RecibirDano(int cantidad)
    {
        if (muerto) return;

        vida -= cantidad;
        if (vida > 0) return;

        // ---------------- Muere ----------------
        muerto = true;

        Collider2D colision = GetComponent<Collider2D>();
        if (colision != null) colision.enabled = false;

        Rigidbody2D cuerpo = GetComponent<Rigidbody2D>();
        if (cuerpo != null) cuerpo.simulated = false;

        AnimadorSimple animacion = gameObject.AddComponent<AnimadorSimple>();
        animacion.cuadros = cuadrosMuerte;
        animacion.cuadrosPorSegundo = 14f;
        animacion.repetir = false;

        Destroy(gameObject, 0.8f);
    }
}
