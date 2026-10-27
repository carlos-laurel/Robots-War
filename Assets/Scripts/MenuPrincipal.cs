using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de titulo. Al pulsar cualquier tecla empieza el juego.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    public string escenaJuego = "Nivel1";
    public AudioSource altavoz;
    public AudioClip sonidoAceptar;

    private float espera = 0.5f;   // para que no arranque con la tecla anterior

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
            SceneManager.LoadScene(escenaJuego);
        }
    }
}
