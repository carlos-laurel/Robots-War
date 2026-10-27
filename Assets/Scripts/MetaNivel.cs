using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zona invisible que hay al final de cada nivel.
/// Cuando el jugador la toca se carga el siguiente nivel.
/// </summary>
public class MetaNivel : MonoBehaviour
{
    public string escenaSiguiente = "Nivel2";

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<JugadorMason>() == null) return;

        if (GestorJuego.instancia != null) GestorJuego.instancia.terminado = true;

        SceneManager.LoadScene(escenaSiguiente);
    }
}
