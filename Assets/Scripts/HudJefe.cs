using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del jefe final. Solo se usa en el nivel del Nucleo.
/// La parte roja se encoge segun la vida que le queda.
/// </summary>
public class HudJefe : MonoBehaviour
{
    public JefeNucleo jefe;
    public RectTransform barra;      // la parte roja que se encoge
    public float anchoTotal = 156f;

    void Update()
    {
        if (jefe == null || barra == null) return;

        float parte = (jefe.VidaMaxima > 0) ? (float)jefe.Vida / jefe.VidaMaxima : 0f;
        parte = Mathf.Clamp01(parte);

        barra.sizeDelta = new Vector2(anchoTotal * parte, barra.sizeDelta.y);
    }
}
