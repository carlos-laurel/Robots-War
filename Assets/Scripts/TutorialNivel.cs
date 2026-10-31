using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mini tutorial con los controles del juego.
/// Aparece al empezar el nivel, se esconde solo y se vuelve a abrir con la tecla H.
/// </summary>
public class TutorialNivel : MonoBehaviour
{
    public GameObject panel;
    public float segundosVisibles = 8f;
    public bool mostrarAlEmpezar = true;

    private float tiempoMostrado;

    void Start()
    {
        if (panel == null) return;

        panel.SetActive(mostrarAlEmpezar);
        tiempoMostrado = 0f;
    }

    void Update()
    {
        if (panel == null) return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        // H abre o cierra la ayuda de controles
        if (teclado.hKey.wasPressedThisFrame)
        {
            panel.SetActive(!panel.activeSelf);
            tiempoMostrado = 0f;
            return;
        }

        if (!panel.activeSelf) return;

        tiempoMostrado += Time.deltaTime;
        if (tiempoMostrado > segundosVisibles) panel.SetActive(false);
    }
}
