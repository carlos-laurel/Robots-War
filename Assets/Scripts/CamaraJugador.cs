using UnityEngine;

/// <summary>
/// La camara sigue al jugador de izquierda a derecha
/// y se detiene en los bordes del nivel (limiteIzquierdo / limiteDerecho).
/// Los limites son los bordes del nivel: la camara se separa de ellos media
/// pantalla, calculada con el tamaño ortografico y el aspecto actual.
/// </summary>
public class CamaraJugador : MonoBehaviour
{
    public Transform objetivo;
    public float limiteIzquierdo = -16f;   // borde izquierdo del nivel
    public float limiteDerecho = 96f;      // borde derecho del nivel
    public float altura = 0f;
    public float suavizado = 8f;

    private Camera camara;

    void Awake()
    {
        camara = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        float medioAncho = 10f;
        if (camara != null) medioAncho = camara.orthographicSize * camara.aspect;

        float minimo = limiteIzquierdo + medioAncho;
        float maximo = limiteDerecho - medioAncho;
        if (minimo > maximo) minimo = maximo = (limiteIzquierdo + limiteDerecho) * 0.5f;

        float x = Mathf.Clamp(objetivo.position.x, minimo, maximo);
        Vector3 destino = new Vector3(x, altura, transform.position.z);

        transform.position = Vector3.Lerp(transform.position, destino, suavizado * Time.deltaTime);
    }
}
