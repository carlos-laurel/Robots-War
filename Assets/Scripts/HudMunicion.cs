using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de municion del HUD. El relleno amarillo se encoge
/// segun las balas que le quedan al jugador.
/// </summary>
public class HudMunicion : MonoBehaviour
{
    public JugadorMason jugador;
    public RectTransform barra;      // la parte amarilla que se encoge
    public float anchoTotal = 58f;

    void Update()
    {
        if (jugador == null || barra == null) return;

        float parte = (jugador.MunicionMaxima > 0) ? (float)jugador.Municion / jugador.MunicionMaxima : 0f;
        barra.sizeDelta = new Vector2(anchoTotal * Mathf.Clamp01(parte), barra.sizeDelta.y);
    }
}
