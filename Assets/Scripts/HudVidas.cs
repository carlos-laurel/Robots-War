using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja los iconos de vida en la esquina de la pantalla.
/// Se le pasa el jugador y los dos sprites (vida llena / vida vacia).
/// </summary>
public class HudVidas : MonoBehaviour
{
    public JugadorMason jugador;
    public Image[] iconos;
    public Sprite iconoLleno;
    public Sprite iconoVacio;

    void Update()
    {
        if (jugador == null) return;

        for (int i = 0; i < iconos.Length; i++)
        {
            if (iconos[i] == null) continue;
            iconos[i].sprite = (i < jugador.Vida) ? iconoLleno : iconoVacio;
        }
    }
}
