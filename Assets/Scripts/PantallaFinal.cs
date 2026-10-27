using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de fin de partida (escena "Reiniciar").
/// Si el jugador gano al jefe enseña los creditos; si perdio, la pantalla de derrota.
/// Al pulsar cualquier tecla vuelve al titulo.
/// </summary>
public class PantallaFinal : MonoBehaviour
{
    public GameObject panelDerrota;
    public GameObject panelVictoria;

    public string escenaSiguiente = "MenuPrincipal";
    public AudioSource altavoz;
    public AudioClip sonidoAceptar;

    private float espera = 0.8f;   // para no salir con la misma tecla que te mato

    void Start()
    {
        bool gano = GestorJuego.ultimaVictoria;

        if (panelDerrota != null) panelDerrota.SetActive(!gano);
        if (panelVictoria != null) panelVictoria.SetActive(gano);
    }

    void Update()
    {
        if (espera > 0f)
        {
            espera -= Time.deltaTime;
            return;
        }

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclado.anyKey.wasPressedThisFrame)
        {
            if (altavoz != null && sonidoAceptar != null) altavoz.PlayOneShot(sonidoAceptar);
            SceneManager.LoadScene(escenaSiguiente);
        }
    }
}
