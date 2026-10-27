using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Se encarga del final del nivel.
///  - GameOver(): el jugador murio -> se carga la escena de derrota.
///  - Victoria(): el jefe cayo -> se carga la escena de creditos.
/// En los dos casos la escena final es "Reiniciar", donde se pulsa
/// cualquier tecla para volver al titulo.
/// </summary>
public class GestorJuego : MonoBehaviour
{
    public static GestorJuego instancia;

    // La escena final lee esto para saber que imagen enseñar.
    public static bool ultimaVictoria;

    public string escenaFinal = "Reiniciar";

    // Cuando esto es true el nivel ya termino: el jugador y los enemigos dejan de actuar
    [HideInInspector] public bool terminado;

    private bool termino;
    private float tiempoFin;
    private float espera = 0.9f;

    void Awake()
    {
        instancia = this;
        terminado = false;
        termino = false;
    }

    public void GameOver()
    {
        if (termino) return;

        termino = true;
        terminado = true;
        ultimaVictoria = false;
        tiempoFin = Time.unscaledTime;
        espera = 0.9f;
    }

    public void Victoria()
    {
        if (termino) return;

        termino = true;
        terminado = true;
        ultimaVictoria = true;
        tiempoFin = Time.unscaledTime;
        espera = 2.2f;   // deja ver como cae el jefe
    }

    void Update()
    {
        if (!termino) return;

        // Pequena espera para que se vea la muerte del jefe o la del jugador
        if (Time.unscaledTime < tiempoFin + espera) return;

        SceneManager.LoadScene(escenaFinal);
    }
}
