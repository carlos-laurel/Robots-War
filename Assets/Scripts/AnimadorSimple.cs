using UnityEngine;

/// <summary>
/// Cambia los sprites de un SpriteRenderer uno detras de otro.
/// Sirve para animar cosas sin usar el sistema Animator, que es mas complicado.
/// Se puede usar desde el inspector o desde otro script.
/// </summary>
public class AnimadorSimple : MonoBehaviour
{
    [Header("Animacion")]
    public Sprite[] cuadros;               // Lista de imagenes que se van mostrando
    public float cuadrosPorSegundo = 10f;  // Velocidad de la animacion
    public bool repetir = true;            // Si vuelve a empezar al terminar

    private SpriteRenderer dibujo;
    private float tiempo;
    private int indice;

    void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (dibujo == null) return;
        if (cuadros == null || cuadros.Length == 0) return;

        tiempo += Time.deltaTime;
        if (tiempo < 1f / cuadrosPorSegundo) return;

        tiempo = 0f;
        indice++;

        if (indice >= cuadros.Length)
        {
            if (repetir) indice = 0;
            else
            {
                indice = cuadros.Length - 1;
                enabled = false;   // ya termino, se queda en el ultimo cuadro
            }
        }

        dibujo.sprite = cuadros[indice];
    }
}
