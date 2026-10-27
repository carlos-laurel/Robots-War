using UnityEngine;

/// <summary>
/// Bala generica. Sirve tanto para Mason como para los robots.
/// Se mueve sola en linea recta y hace dano a quien corresponda.
/// </summary>
public class Bala : MonoBehaviour
{
    public float velocidad = 14f;
    public float duracion = 3f;

    private Vector2 direccion = Vector2.right;
    private bool esDelJugador = true;
    private int dano = 1;

    /// <summary>
    /// Se llama justo despues de crear la bala para decirle hacia donde va.
    /// </summary>
    public void Configurar(Vector2 nuevaDireccion, bool delJugador, int cantidadDano)
    {
        direccion = nuevaDireccion.normalized;
        esDelJugador = delJugador;
        dano = cantidadDano;
    }

    void Start()
    {
        // Para que no queden balas eternas por el escenario
        Destroy(gameObject, duracion);
    }

    void Update()
    {
        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (esDelJugador)
        {
            RobotEnemigo robot = otro.GetComponent<RobotEnemigo>();
            if (robot != null)
            {
                robot.RecibirDano(dano);
                Destroy(gameObject);
                return;
            }

            JefeNucleo jefe = otro.GetComponent<JefeNucleo>();
            if (jefe != null)
            {
                jefe.RecibirDano(dano);
                Destroy(gameObject);
                return;
            }

            // Si toca una plataforma, una caja o el suelo, desaparece
            if (!otro.isTrigger && otro.GetComponent<JugadorMason>() == null)
                Destroy(gameObject);
        }
        else
        {
            JugadorMason jugador = otro.GetComponent<JugadorMason>();
            if (jugador != null)
            {
                jugador.RecibirDano(dano);
                Destroy(gameObject);
                return;
            }

            if (!otro.isTrigger && otro.GetComponent<RobotEnemigo>() == null && otro.GetComponent<JefeNucleo>() == null)
                Destroy(gameObject);
        }
    }
}
