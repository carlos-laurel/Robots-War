using UnityEngine;

/// <summary>
/// Jefe final "El Nucleo".
/// Se balancea arriba y abajo y dispara un abanico de tres proyectiles al jugador.
/// Cuando se queda sin vida se muestra la pantalla de creditos.
/// </summary>
public class JefeNucleo : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 40;

    [Header("Disparo")]
    public GameObject prefabBala;
    public Transform puntoDisparo;
    public float cadencia = 1.3f;
    public float aperturaAbanico = 12f;
    public float distanciaDeteccion = 18f;   // no dispara si el jugador esta lejos (fuera de pantalla)

    [Header("Movimiento")]
    public float amplitud = 1.5f;
    public float velocidadBalanceo = 1.2f;

    private int vida;
    private float ultimoDisparo;
    private float tiempo;
    private Vector3 posicionInicial;
    private Transform jugador;
    private bool enfadado;   // segunda fase: dispara mas rapido y mas abierto

    // Lo consulta la barra de vida del HUD
    public int Vida { get { return vida; } }
    public int VidaMaxima { get { return vidaMaxima; } }

    void Awake()
    {
        vida = vidaMaxima;
        posicionInicial = transform.position;
    }

    void Start()
    {
        JugadorMason encontrado = Object.FindFirstObjectByType<JugadorMason>();
        if (encontrado != null) jugador = encontrado.transform;
    }

    void Update()
    {
        if (vida <= 0) return;

        // ---------------- Balanceo arriba / abajo ----------------
        tiempo += Time.deltaTime;
        transform.position = posicionInicial + Vector3.up * Mathf.Sin(tiempo * velocidadBalanceo) * amplitud;

        // ---------------- Disparar (solo cuando el jugador esta cerca) ----------------
        if (jugador == null) return;
        if (Vector2.Distance(transform.position, jugador.position) > distanciaDeteccion) return;
        if (Time.time >= ultimoDisparo + cadencia) Disparar();
    }

    private void Disparar()
    {
        ultimoDisparo = Time.time;
        if (prefabBala == null || puntoDisparo == null || jugador == null) return;

        Vector2 direccion = (jugador.position - puntoDisparo.position).normalized;

        // Primera fase: 3 balas. Segunda fase (media vida): 5 balas mas abiertas
        int cuantas = enfadado ? 5 : 3;
        float apertura = enfadado ? 14f : aperturaAbanico;
        int centro = cuantas / 2;

        for (int i = 0; i < cuantas; i++)
        {
            float grados = (i - centro) * apertura;
            Vector2 desviada = Quaternion.Euler(0f, 0f, grados) * direccion;

            GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.identity);
            Bala script = bala.GetComponent<Bala>();
            if (script != null) script.Configurar(desviada, false, 1);
        }
    }

    public void RecibirDano(int cantidad)
    {
        if (vida <= 0) return;

        vida -= cantidad;
        if (vida > 0)
        {
            // Al bajar de la mitad de la vida se enfada (segunda fase)
            if (!enfadado && vida <= vidaMaxima / 2)
            {
                enfadado = true;
                cadencia = cadencia * 0.65f;
                velocidadBalanceo = velocidadBalanceo * 1.6f;
            }
            return;
        }

        // ---------------- El jefe ha caido ----------------
        if (GestorJuego.instancia != null) GestorJuego.instancia.Victoria();
        Destroy(gameObject, 1.5f);
    }
}
