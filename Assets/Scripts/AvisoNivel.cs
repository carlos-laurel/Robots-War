using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Cartel grande con el nombre del nivel. Sale al empezar la escena,
/// se queda unos segundos y se desvanece solo.
/// </summary>
public class AvisoNivel : MonoBehaviour
{
    public TextMeshProUGUI texto;
    public float segundosVisibles = 2.5f;
    public float segundosDesvanecido = 0.7f;

    void Start()
    {
        if (texto == null) return;
        StartCoroutine(Desvanecer());
    }

    private IEnumerator Desvanecer()
    {
        Color color = texto.color;

        yield return new WaitForSeconds(segundosVisibles);

        float tiempo = 0f;
        while (tiempo < segundosDesvanecido)
        {
            tiempo += Time.deltaTime;

            color.a = Mathf.Lerp(1f, 0f, tiempo / segundosDesvanecido);
            texto.color = color;

            yield return null;
        }

        texto.gameObject.SetActive(false);
    }
}
