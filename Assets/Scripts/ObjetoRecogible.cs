using UnityEngine;

/// <summary>
/// Objetos que el jugador recoge al tocarlos.
///  - Vida     -> recupera vida (botiquin).
///  - Municion -> recarga balas y mejora el arma durante unos segundos.
/// </summary>
public class ObjetoRecogible : MonoBehaviour
{
    public enum Tipo
    {
        Vida,
        Municion
    }

    public Tipo tipo = Tipo.Vida;
    public int cantidadVida = 1;
    public int cantidadMunicion = 50;
    public float segundosDeMejora = 10f;
    public AudioClip sonido;

    void OnTriggerEnter2D(Collider2D otro)
    {
        JugadorMason jugador = otro.GetComponent<JugadorMason>();
        if (jugador == null) return;

        if (tipo == Tipo.Vida) jugador.Curar(cantidadVida);
        else
        {
            jugador.Recargar(cantidadMunicion);
            jugador.MejorarArma(segundosDeMejora);
        }

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);

        Destroy(gameObject);
    }
}
