using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Aplica a las escenas que YA existen los cambios pedidos, sin reconstruirlas:
///  - camara ortografica a 5.625
///  - dificultad mas baja (menos cadencia y menos vida en robots y jefe, bala enemiga mas lenta)
///  - mini tutorial con los controles (tecla H para volver a verlo)
///  - texto permanente con el nivel actual + cartel grande al empezar
///  - camioneta del final (Ranger Open) y su animacion de salida
/// Menu: Aura -> 4.
/// </summary>
public static class MejorasJuego
{
    public const float TAMANO_CAMARA = 5.625f;
    public const float ALTO_SUELO = -4f;

    const string R_CAMIONETA = "Assets/VOiD1 Gaming - Pixel Cars/Pixel Cars/Ranger Open.png";
    const string R_FUENTE = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string R_BALA_ENEMIGA = "Assets/Prefabs/BalaEnemiga.prefab";

    // =====================================================================
    //  ENTRADA PRINCIPAL
    // =====================================================================
    [MenuItem("Aura/4. Aplicar mejoras a las escenas")]
    public static void AplicarTodo()
    {
        PrepararCamioneta();
        BajarDificultadBalaEnemiga();

        ConfigNivel n1 = new ConfigNivel("Assets/Scenes/Nivel1.unity", 1, "EL BORDE DE LA SELVA", "Nivel2", true, true);
        ConfigNivel n2 = new ConfigNivel("Assets/Scenes/Nivel2.unity", 2, "LAS PROFUNDIDADES DE LA JUNGLA", "Nivel3", false, true);
        ConfigNivel n3 = new ConfigNivel("Assets/Scenes/Nivel3.unity", 3, "EL CLARO DEL NUCLEO", "", false, false);

        AplicarNivel(n1);
        AplicarNivel(n2);
        AplicarNivel(n3);

        // Menu principal: la camara tambien a 5.625
        Scene menu = EditorSceneManager.OpenScene("Assets/Scenes/MenuPrincipal.unity", OpenSceneMode.Single);
        foreach (Camera cam in Object.FindObjectsByType<Camera>())
            cam.orthographicSize = TAMANO_CAMARA;

        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Mejoras] Escenas actualizadas: camara 5.625, disparo en 4 direcciones, dificultad mas baja, " +
                  "tutorial de controles, texto de nivel y camioneta de salida.");
    }

    class ConfigNivel
    {
        public string ruta;
        public int numero;
        public string nombre;
        public string escenaSiguiente;
        public bool tutorialVisible;
        public bool conCamioneta;

        public ConfigNivel(string ruta, int numero, string nombre, string escenaSiguiente, bool tutorialVisible, bool conCamioneta)
        {
            this.ruta = ruta;
            this.numero = numero;
            this.nombre = nombre;
            this.escenaSiguiente = escenaSiguiente;
            this.tutorialVisible = tutorialVisible;
            this.conCamioneta = conCamioneta;
        }
    }

    static void AplicarNivel(ConfigNivel cfg)
    {
        Scene escena = EditorSceneManager.OpenScene(cfg.ruta, OpenSceneMode.Single);

        Transform raiz = null;
        foreach (GameObject go in escena.GetRootGameObjects())
            if (go.name == "Nivel") raiz = go.transform;

        if (raiz == null)
        {
            Debug.LogWarning("[Mejoras] No encontre la raiz 'Nivel' en " + cfg.ruta);
            return;
        }

        // 1. Camara
        Camera camara = Object.FindAnyObjectByType<Camera>();
        foreach (Camera cam in Object.FindObjectsByType<Camera>())
            cam.orthographicSize = TAMANO_CAMARA;

        // 2. HUD, dificultad, camioneta
        Canvas canvas = raiz.GetComponentInChildren<Canvas>(true);
        GameObject jugador = Buscar(raiz, "Mason");

        DecorarNivel(raiz, canvas, jugador, cfg.numero, cfg.nombre, cfg.escenaSiguiente,
            cfg.tutorialVisible, cfg.conCamioneta, LimiteDerechoDe(camara));

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    static float LimiteDerechoDe(Camera cam)
    {
        if (cam == null) return 96f;

        CamaraJugador seguimiento = cam.GetComponent<CamaraJugador>();
        if (seguimiento != null) return seguimiento.limiteDerecho;

        return 96f;
    }

    // =====================================================================
    //  LO QUE SE APLICA A UN NIVEL (lo usa tambien ConstructorJuego)
    // =====================================================================
    public static void DecorarNivel(Transform raiz, Canvas canvas, GameObject jugador, int numero, string nombre,
        string escenaSiguiente, bool tutorialVisible, bool conCamioneta, float limiteDerecho)
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(R_FUENTE);
        Sprite camioneta = AssetDatabase.LoadAssetAtPath<Sprite>(R_CAMIONETA);
        if (camioneta == null)
        {
            PrepararCamioneta();
            camioneta = AssetDatabase.LoadAssetAtPath<Sprite>(R_CAMIONETA);
        }

        BajarDificultad(raiz);

        if (canvas != null)
        {
            CrearTextoNivel(canvas.transform, numero, fuente);
            CrearAviso(canvas.transform, numero, nombre, fuente);
            CrearTutorial(canvas.transform, fuente, tutorialVisible);
        }

        GameObject meta = Buscar(raiz, "Meta");
        if (meta != null && !string.IsNullOrEmpty(escenaSiguiente))
        {
            MetaNivel metaScript = meta.GetComponent<MetaNivel>();
            if (metaScript != null) metaScript.escenaSiguiente = escenaSiguiente;
        }

        if (conCamioneta && jugador != null && camioneta != null)
            CrearCamioneta(raiz, camioneta, jugador, escenaSiguiente, limiteDerecho, meta);
    }

    // =====================================================================
    //  DIFICULTAD
    // =====================================================================
    public static void BajarDificultad(Transform raiz)
    {
        if (raiz == null) return;

        // Robots: menos vida, disparan mas lento y desde menos lejos
        foreach (RobotEnemigo robot in raiz.GetComponentsInChildren<RobotEnemigo>(true))
        {
            robot.vidaMaxima = 2;
            robot.distanciaDeteccion = 7f;
            robot.cadencia = 2.4f;
        }

        // Jefe: menos vida, dispara mas lento y con el abanico mas cerrado
        foreach (JefeNucleo jefe in raiz.GetComponentsInChildren<JefeNucleo>(true))
        {
            jefe.vidaMaxima = 26;
            jefe.cadencia = 1.8f;
            jefe.aperturaAbanico = 10f;
            jefe.distanciaDeteccion = 14f;
        }
    }

    static void BajarDificultadBalaEnemiga()
    {
        GameObject contenido = PrefabUtility.LoadPrefabContents(R_BALA_ENEMIGA);
        if (contenido == null) return;

        Bala bala = contenido.GetComponent<Bala>();
        if (bala != null) bala.velocidad = 7f;

        PrefabUtility.SaveAsPrefabAsset(contenido, R_BALA_ENEMIGA);
        PrefabUtility.UnloadPrefabContents(contenido);
    }

    // =====================================================================
    //  CAMIONETA DEL FINAL
    // =====================================================================
    public static void PrepararCamioneta()
    {
        TextureImporter imp = AssetImporter.GetAtPath(R_CAMIONETA) as TextureImporter;
        if (imp == null)
        {
            Debug.LogWarning("[Mejoras] No encontre " + R_CAMIONETA);
            return;
        }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.isReadable = true;
        imp.SaveAndReimport();

        // El dibujo de la camioneta solo ocupa la parte de abajo de la imagen:
        // se pone el pivote en el centro de abajo del dibujo para que quede apoyada en el suelo.
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(R_CAMIONETA);
        if (textura == null) return;

        Color32[] pixeles = textura.GetPixels32();
        int ancho = textura.width;
        int alto = textura.height;

        int minX = ancho, maxX = -1, minY = alto, maxY = -1;
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                if (pixeles[y * ancho + x].a <= 16) continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0) return;

        float pivoteX = ((minX + maxX) * 0.5f + 0.5f) / ancho;
        float pivoteY = minY / (float)alto;

        TextureImporterSettings ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
        ajustes.spritePivot = new Vector2(pivoteX, pivoteY);
        imp.SetTextureSettings(ajustes);
        imp.SaveAndReimport();
    }

    static void CrearCamioneta(Transform raiz, Sprite sprite, GameObject jugador, string escenaSiguiente,
        float limiteDerecho, GameObject meta)
    {
        GameObject existente = Buscar(raiz, "Camioneta");
        GameObject go = existente != null ? existente : new GameObject("Camioneta");
        if (existente == null) go.transform.SetParent(raiz, false);

        SpriteRenderer dibujo = go.GetComponent<SpriteRenderer>();
        if (dibujo == null) dibujo = go.AddComponent<SpriteRenderer>();
        dibujo.sprite = sprite;
        dibujo.sortingOrder = 4;

        // Se aparca un poco antes de la meta, sobre el suelo
        float x = (meta != null) ? meta.transform.position.x - 6f : limiteDerecho - 6f;
        go.transform.position = new Vector3(x, ALTO_SUELO, 0f);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        SalidaNivel salida = go.GetComponent<SalidaNivel>();
        if (salida == null) salida = go.AddComponent<SalidaNivel>();

        salida.jugador = jugador.GetComponent<JugadorMason>();
        salida.camioneta = go.transform;
        salida.limiteDerecho = limiteDerecho;
        salida.velocidadCamioneta = 13f;
        salida.segundosAcelerando = 0.9f;
    }

    // =====================================================================
    //  TEXTOS DEL HUD
    // =====================================================================
    static void CrearTextoNivel(Transform canvas, int numero, TMP_FontAsset fuente)
    {
        CrearTexto(canvas, "TextoNivel", "NIVEL " + numero + " / 3",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 6f), new Vector2(150f, 14f),
            9f, TextAlignmentOptions.BottomRight, fuente, new Color(1f, 1f, 1f, 0.92f), FontStyles.Bold);
    }

    static void CrearAviso(Transform canvas, int numero, string nombre, TMP_FontAsset fuente)
    {
        TextMeshProUGUI texto = CrearTexto(canvas, "AvisoNivel", "NIVEL " + numero + "\n" + nombre,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(290f, 60f),
            16f, TextAlignmentOptions.Center, fuente, new Color(1f, 0.95f, 0.62f, 1f), FontStyles.Bold);

        AvisoNivel aviso = canvas.GetComponent<AvisoNivel>();
        if (aviso == null) aviso = canvas.gameObject.AddComponent<AvisoNivel>();

        aviso.texto = texto;
        aviso.segundosVisibles = 2.5f;
        aviso.segundosDesvanecido = 0.7f;
    }

    static void CrearTutorial(Transform canvas, TMP_FontAsset fuente, bool visible)
    {
        Transform existente = canvas.Find("TutorialNivel");
        GameObject panel = existente != null ? existente.gameObject : new GameObject("TutorialNivel", typeof(RectTransform));
        if (existente == null) panel.transform.SetParent(canvas, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(6f, -34f);
        rt.sizeDelta = new Vector2(152f, 54f);

        Image fondo = panel.GetComponent<Image>();
        if (fondo == null) fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.05f, 0.03f, 0.06f, 0.88f);
        fondo.raycastTarget = false;

        string controles =
            "<b>CONTROLES</b>\n" +
            "Mover: A / D  o  flechas\n" +
            "Saltar: ESPACIO\n" +
            "Apuntar: W / S  o  arriba / abajo\n" +
            "Disparar: J  o  CTRL IZQ\n" +
            "Ayuda: H";

        CrearTexto(panel.transform, "Texto", controles,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -5f), new Vector2(140f, 44f),
            7f, TextAlignmentOptions.TopLeft, fuente, Color.white, FontStyles.Normal);

        TutorialNivel tutorial = canvas.GetComponent<TutorialNivel>();
        if (tutorial == null) tutorial = canvas.gameObject.AddComponent<TutorialNivel>();

        tutorial.panel = panel;
        tutorial.mostrarAlEmpezar = visible;

        panel.SetActive(visible);
    }

    static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string contenido,
        Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamano, float tamanoFuente,
        TextAlignmentOptions alineacion, TMP_FontAsset fuente, Color color, FontStyles estilo)
    {
        Transform existente = padre.Find(nombre);

        GameObject go = existente != null ? existente.gameObject : new GameObject(nombre, typeof(RectTransform));
        if (existente == null) go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();

        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();

        if (fuente != null) tmp.font = fuente;
        tmp.text = contenido;
        tmp.fontSize = tamanoFuente;
        tmp.alignment = alineacion;
        tmp.color = color;
        tmp.fontStyle = estilo;
        tmp.raycastTarget = false;

        return tmp;
    }

    static GameObject Buscar(Transform raiz, string nombre)
    {
        if (raiz == null) return null;

        Transform hijo = raiz.Find(nombre);
        return hijo != null ? hijo.gameObject : null;
    }
}
