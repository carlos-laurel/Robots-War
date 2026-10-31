using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pequena animacion del final de nivel:
/// Mason corre hasta la camioneta, se sube y la camioneta se marcha.
/// Cuando la camioneta sale de pantalla se carga la siguiente escena.
/// En el nivel del jefe la camioneta entra a buscarlo (vieneABuscarte)
/// y al final se carga la pantalla de creditos.
/// La coloca y la configura la herramienta "Aura / 4. Aplicar mejoras a las escenas".
/// </summary>
public class SalidaNivel : MonoBehaviour
{
    [Header("Quien participa")]
    public JugadorMason jugador;
    public Transform camioneta;

    [Header("Marcha de la camioneta")]
    public float velocidadCamioneta = 13f;
    public float segundosAcelerando = 0.9f;
    public float limiteDerecho = 96f;

    [Header("Final del nivel del jefe")]
    public bool vieneABuscarte = false;    // la camioneta entra y se para al lado de Mason
    public float esperaInicial = 0f;       // deja ver la caida del jefe antes de arrancar
    public bool esVictoriaFinal = false;   // la escena final tiene que mostrar los creditos
    public string escenaFinal = "Reiniciar";

    private bool empezado;

    public void Iniciar(string escenaSiguiente)
    {
        if (empezado) return;
        empezado = true;

        if (string.IsNullOrEmpty(escenaSiguiente)) escenaSiguiente = escenaFinal;

        // El nivel ya termino: los robots y el jugador dejan de actuar
        if (GestorJuego.instancia != null) GestorJuego.instancia.terminado = true;
        if (esVictoriaFinal) GestorJuego.ultimaVictoria = true;

        StartCoroutine(Secuencia(escenaSiguiente));
    }

    private IEnumerator Secuencia(string escenaSiguiente)
    {
        // En el nivel del jefe se espera un poco para ver como cae
        if (esperaInicial > 0f) yield return new WaitForSeconds(esperaInicial);

        if (jugador == null || camioneta == null)
        {
            SceneManager.LoadScene(escenaSiguiente);
            yield break;
        }

        Rigidbody2D cuerpo = jugador.GetComponent<Rigidbody2D>();
        Collider2D choque = jugador.GetComponent<Collider2D>();
        SpriteRenderer dibujo = jugador.GetComponent<SpriteRenderer>();
        Sprite[] correr = jugador.cuadrosCorrer;

        // ---------------- 0. La camioneta entra a buscar a Mason ----------------
        if (vieneABuscarte)
        {
            float destinoCamioneta = jugador.transform.position.x + 2.4f;
            float tiempoLlegada = 0f;

            while (camioneta.position.x > destinoCamioneta && tiempoLlegada < 6f)
            {
                tiempoLlegada += Time.deltaTime;

                Vector3 pos = camioneta.position;
                pos.x = Mathf.MoveTowards(pos.x, destinoCamioneta, velocidadCamioneta * Time.deltaTime);
                camioneta.position = pos;

                if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;

                yield return null;
            }
        }

        // ---------------- 1. Mason corre hasta la camioneta ----------------
        float destinoX = vieneABuscarte ? camioneta.position.x - 1.4f : camioneta.position.x + 1.2f;

        // Mira hacia la camioneta: a la derecha si viene a buscarlo, a la izquierda si esta aparcada
        jugador.transform.localScale = new Vector3(vieneABuscarte ? 1f : -1f, 1f, 1f);

        // El tiempo de la carrerita se adapta a lo lejos que este la camioneta
        float distancia = Mathf.Abs(jugador.transform.position.x - destinoX);
        float tiempoMaximo = Mathf.Max(2.5f, distancia / 4.5f + 0.8f);

        float tiempo = 0f;
        while (Mathf.Abs(jugador.transform.position.x - destinoX) > 0.1f && tiempo < tiempoMaximo)
        {
            tiempo += Time.deltaTime;

            Vector3 pos = jugador.transform.position;
            pos.x = Mathf.MoveTowards(pos.x, destinoX, 4.5f * Time.deltaTime);
            jugador.transform.position = pos;

            if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;

            if (dibujo != null && correr != null && correr.Length > 0)
                dibujo.sprite = correr[(int)(tiempo * 12f) % correr.Length];

            yield return null;
        }

        // ---------------- 2. Se sube: desaparece dentro de la camioneta ----------------
        if (choque != null) choque.enabled = false;
        if (cuerpo != null) cuerpo.simulated = false;
        if (dibujo != null) dibujo.enabled = false;

        yield return new WaitForSeconds(0.35f);

        // ---------------- 3. La camioneta arranca y se va ----------------
        float recorrido = 0f;
        while (camioneta.position.x < limiteDerecho + 8f && recorrido < 6f)
        {
            recorrido += Time.deltaTime;

            float velocidad = Mathf.Lerp(0f, velocidadCamioneta, Mathf.Clamp01(recorrido / Mathf.Max(0.01f, segundosAcelerando)));
            camioneta.position += Vector3.right * velocidad * Time.deltaTime;

            yield return null;
        }

        // ---------------- 4. Siguiente escena ----------------
        SceneManager.LoadScene(escenaSiguiente);
    }
}
