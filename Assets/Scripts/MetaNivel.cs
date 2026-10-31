using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zona invisible que hay al final de cada nivel.
/// Cuando el jugador la toca se lanza la animacion de salida
/// (Mason se sube a la camioneta y se marcha) y despues se carga el siguiente nivel.
/// </summary>
public class MetaNivel : MonoBehaviour
{
    public string escenaSiguiente = "Nivel2";

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<JugadorMason>() == null) return;

        // Si el nivel tiene la camioneta, ella se encarga de cambiar de escena
        SalidaNivel salida = Object.FindAnyObjectByType<SalidaNivel>();
        if (salida != null)
        {
            salida.Iniciar(escenaSiguiente);
            return;
        }

        if (GestorJuego.instancia != null) GestorJuego.instancia.terminado = true;

        SceneManager.LoadScene(escenaSiguiente);
    }
}
