using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Herramienta de editor que construye prefabs y las 4 escenas del juego
/// usando el arte que ya estaba en Assets/SpriteSheets.
/// Menu: Aura -> 1 y 2.
/// </summary>
public static class ConstructorJuego
{
    // ---------- Ajustes generales ----------
    const float PPU = 16f;                 // pixeles por unidad (pixel art)
    const float TAMANO_CAMARA = 5.625f;      // campo de vision pedido (antes 5.625)
    const float ALTO_SUELO = -4f;          // altura de la superficie del suelo
    const float ALTO_CESPED = 0.625f;      // alto de una pieza de cesped

    // ---------- Rutas de arte ----------
    const string R_PLAYER = "Assets/SpriteSheets/player/";
    const string R_ENEMY  = "Assets/SpriteSheets/enemy/";
    const string R_BG     = "Assets/SpriteSheets/background/";
    const string R_BOSS   = "Assets/SpriteSheets/boss/";
    const string R_UI     = "Assets/SpriteSheets/ui/";
    const string R_TITLE  = "Assets/SpriteSheets/titlescreen/";
    const string R_TILES  = "Assets/SpriteSheets/tiles_spritesheet.png";
    const string R_SND    = "Assets/Sounds/";

    // =====================================================================
    //  1. IMPORTACION
    // =====================================================================
    [MenuItem("Aura/1. Ajustar importacion del arte")]
    [System.Obsolete]

    public static void AjustarImportacion()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/SpriteSheets" });
        int cambiadas = 0;

        foreach (string g in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(g);
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;

            imp.spritePixelsToUnits = PPU;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            cambiadas++;
        }

        Debug.Log("Importacion ajustada: " + cambiadas + " texturas a PPU " + PPU + " y filtro Point.");
    }

    // =====================================================================
    //  2. CONSTRUIR TODO
    // =====================================================================
    [MenuItem("Aura/2. Construir juego completo")]
    [System.Obsolete]

    public static void ConstruirTodo()
    {
        AjustarImportacion();
        ConstruirEscenas();
    }

    [MenuItem("Aura/3. Reconstruir solo las escenas")]
    public static void ConstruirEscenas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");

        // ---- Prefabs de balas ----
        GameObject balaJugador = CrearPrefabBala("Assets/Prefabs/BalaJugador.prefab",
            R_PLAYER + "weapon_bullet.png", "weapon_bullet_0", 16f, true);
        GameObject balaEnemiga = CrearPrefabBala("Assets/Prefabs/BalaEnemiga.prefab",
            R_ENEMY + "enemy_weapon_bullet.png", "enemy_weapon_bullet_0", 9f, false);

        // ---- Sonidos ----
        AudioClip disparo = Audio(R_SND + "snd_rifle_shot.ogg");
        AudioClip salto = Audio(R_SND + "snd_jump.ogg");
        AudioClip dano = Audio(R_SND + "snd_hurt.ogg");
        AudioClip musica = Audio(R_SND + "snd_main_theme.ogg");
        AudioClip musicaJefe = Audio(R_SND + "snd_boss_music.ogg");

        // ---- Escenas ----
        ConstruirMenu(musica);
        ConstruirNivel1(balaJugador, balaEnemiga, disparo, salto, dano, musica);
        ConstruirNivel2(balaJugador, balaEnemiga, disparo, salto, dano, musica);
        ConstruirNivel3(balaJugador, balaEnemiga, disparo, salto, dano, musicaJefe);
        ConstruirReiniciar(musica);

        // ---- Build settings ----
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MenuPrincipal.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Nivel1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Nivel2.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Nivel3.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Reiniciar.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Juego construido: MenuPrincipal, Nivel1, Nivel2, Nivel3 y Reiniciar.");
    }

    // =====================================================================
    //  ESCENA: MENU PRINCIPAL
    // =====================================================================
    public static void ConstruirMenu(AudioClip musica)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform raiz = new GameObject("Escena").transform;

        Camera cam = CrearCamara(raiz, new Vector3(0f, 0f, -10f), new Color(0.05f, 0.09f, 0.12f));
        CamaraJugador seguimiento = cam.GetComponent<CamaraJugador>();
        if (seguimiento != null) Object.DestroyImmediate(seguimiento);

        LuzGlobal(raiz);

        GameObject fondo = Nueva("FondoTitulo", raiz);
        Dibujar(fondo, Uno(R_TITLE + "main_title_bg.png", "main_title_bg_0"), -10);
        fondo.transform.position = Vector3.zero;

        GameObject titulo = Nueva("Titulo", raiz);
        Dibujar(titulo, Uno(R_TITLE + "main_title_title.png", "main_title_title_0"), 0);
        titulo.transform.position = new Vector3(0f, 1.8f, 0f);

        GameObject objetoMenu = Nueva("Menu", raiz);
        AudioSource altavoz = objetoMenu.AddComponent<AudioSource>();
        altavoz.playOnAwake = false;

        MenuPrincipal menu = objetoMenu.AddComponent<MenuPrincipal>();
        menu.escenaJuego = "Nivel1";
        menu.altavoz = altavoz;
        menu.sonidoAceptar = Audio(R_SND + "snd_select.ogg");

        if (musica != null) Musica(raiz, musica, 0.35f);

        EditorSceneManager.SaveScene(escena, "Assets/Scenes/MenuPrincipal.unity");
    }

    // =====================================================================
    //  ESCENA: NIVEL 1 - El Borde de la Selva
    // =====================================================================
    public static void ConstruirNivel1(GameObject balaJugador, GameObject balaEnemiga,
        AudioClip disparo, AudioClip salto, AudioClip dano, AudioClip musica)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform raiz = new GameObject("Nivel").transform;

        FondoJungla(raiz, -16f, 96f);
        LuzGlobal(raiz);

        // Suelo con tres huecos (precipicios)
        Plataforma(raiz, -16f, 14f, ALTO_SUELO, 4f);
        Plataforma(raiz, 20f, 42f, ALTO_SUELO, 4f);
        Plataforma(raiz, 48f, 70f, ALTO_SUELO, 4f);
        Plataforma(raiz, 76f, 96f, ALTO_SUELO, 4f);

        // Plataformas flotantes
        PlataformaDelgada(raiz, 24f, 30f, -1.5f);
        PlataformaDelgada(raiz, 52f, 58f, -1.5f);
        PlataformaDelgada(raiz, 80f, 86f, -1.5f);

        // Adornos
        Arbol(raiz, -12f); Arbol(raiz, 2f); Arbol(raiz, 30f); Arbol(raiz, 55f); Arbol(raiz, 84f);
        Caja(raiz, 8f); Caja(raiz, 34f); Barril(raiz, 12f); Barril(raiz, 62f);
        CajaGrande(raiz, 46f); Planta(raiz, 18f); Planta(raiz, 66f);

        // Jugador
        GameObject jugador = CrearJugador(raiz, new Vector3(-8f, -3.35f, 0f), balaJugador, disparo, salto, dano);

        // Enemigos
        CrearRobot(raiz, 4f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 11f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 26f, -1.5f, balaEnemiga);
        CrearRobot(raiz, 33f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 54f, -1.5f, balaEnemiga);
        CrearRobot(raiz, 60f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 82f, -1.5f, balaEnemiga);
        CrearRobot(raiz, 88f, ALTO_SUELO, balaEnemiga);

        // Objetos recogibles
        CrearRecogible(raiz, "Botiquin", new Vector3(38f, -3.5f, 0f), ObjetoRecogible.Tipo.Vida,
            Uno(R_UI + "ui_medal_life_red.png", "ui_medal_life_red_0"));
        CrearRecogible(raiz, "Municion", new Vector3(64f, -3.5f, 0f), ObjetoRecogible.Tipo.Municion,
            Uno("Assets/SpriteSheets/props/ammo.png", "ammo_0"));

        // Meta
        CrearMeta(raiz, 94f, "Nivel2");

        // Camara, HUD y gestor
        Camera cam = CrearCamara(raiz, new Vector3(-8f, 0f, -10f), new Color(0.05f, 0.09f, 0.12f));
        ConfigurarCamara(cam, jugador.transform, -16f, 96f);
        Canvas canvas = CrearCanvas(raiz);
        CrearHud(canvas.transform, jugador.GetComponent<JugadorMason>());
        CrearGestor(raiz);

        if (musica != null) Musica(raiz, musica, 0.3f);

        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Nivel1.unity");
    }

    // =====================================================================
    //  ESCENA: NIVEL 2 - Las Profundidades de la Jungla
    // =====================================================================
    public static void ConstruirNivel2(GameObject balaJugador, GameObject balaEnemiga,
        AudioClip disparo, AudioClip salto, AudioClip dano, AudioClip musica)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform raiz = new GameObject("Nivel").transform;

        FondoJungla(raiz, -16f, 118f);
        LuzGlobal(raiz);

        // Suelo mas roto: saltos mas largos y plataformas a distinta altura
        Plataforma(raiz, -16f, 8f, ALTO_SUELO, 4f);
        Plataforma(raiz, 13f, 21f, ALTO_SUELO, 4f);
        Plataforma(raiz, 25f, 31f, -1.5f, 2.5f);
        Plataforma(raiz, 35f, 45f, ALTO_SUELO, 4f);
        Plataforma(raiz, 49f, 55f, -1.5f, 2.5f);
        Plataforma(raiz, 59f, 72f, ALTO_SUELO, 4f);
        Plataforma(raiz, 76f, 82f, -1.5f, 2.5f);
        Plataforma(raiz, 86f, 96f, ALTO_SUELO, 4f);
        Plataforma(raiz, 100f, 118f, ALTO_SUELO, 4f);

        PlataformaDelgada(raiz, 32f, 36f, -0.5f);
        PlataformaDelgada(raiz, 68f, 74f, -0.5f);
        PlataformaDelgada(raiz, 104f, 110f, -0.5f);

        Arbol(raiz, -12f); Arbol(raiz, 18f); Arbol(raiz, 40f); Arbol(raiz, 64f); Arbol(raiz, 92f); Arbol(raiz, 114f);
        Caja(raiz, 4f); Caja(raiz, 38f); Caja(raiz, 90f); Barril(raiz, 15f); Barril(raiz, 61f); Barril(raiz, 108f);
        CajaGrande(raiz, 88f); Planta(raiz, 27f); Planta(raiz, 100f);

        GameObject jugador = CrearJugador(raiz, new Vector3(-8f, -3.35f, 0f), balaJugador, disparo, salto, dano);

        CrearRobot(raiz, 0f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 6f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 18f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 27f, -1.5f, balaEnemiga);
        CrearRobot(raiz, 42f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 51f, -1.5f, balaEnemiga);
        CrearRobot(raiz, 66f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 78f, -1.5f, balaEnemiga);
        CrearRobot(raiz, 92f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 106f, ALTO_SUELO, balaEnemiga);

        CrearRecogible(raiz, "Botiquin", new Vector3(46f, -3.5f, 0f), ObjetoRecogible.Tipo.Vida,
            Uno(R_UI + "ui_medal_life_red.png", "ui_medal_life_red_0"));
        CrearRecogible(raiz, "Botiquin2", new Vector3(98f, -3.5f, 0f), ObjetoRecogible.Tipo.Vida,
            Uno(R_UI + "ui_medal_life_red.png", "ui_medal_life_red_0"));
        CrearRecogible(raiz, "Municion", new Vector3(24f, -1f, 0f), ObjetoRecogible.Tipo.Municion,
            Uno("Assets/SpriteSheets/props/ammo.png", "ammo_0"));
        CrearRecogible(raiz, "Municion2", new Vector3(70f, -3.5f, 0f), ObjetoRecogible.Tipo.Municion,
            Uno("Assets/SpriteSheets/props/ammo.png", "ammo_0"));

        CrearMeta(raiz, 116f, "Nivel3");

        Camera cam = CrearCamara(raiz, new Vector3(-8f, 0f, -10f), new Color(0.05f, 0.09f, 0.12f));
        ConfigurarCamara(cam, jugador.transform, -16f, 118f);
        Canvas canvas = CrearCanvas(raiz);
        CrearHud(canvas.transform, jugador.GetComponent<JugadorMason>());
        CrearGestor(raiz);

        if (musica != null) Musica(raiz, musica, 0.3f);

        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Nivel2.unity");
    }

    // =====================================================================
    //  ESCENA: NIVEL 3 - El Claro del Nucleo (nivel final, jefe)
    // =====================================================================
    public static void ConstruirNivel3(GameObject balaJugador, GameObject balaEnemiga,
        AudioClip disparo, AudioClip salto, AudioClip dano, AudioClip musica)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform raiz = new GameObject("Nivel").transform;

        FondoJungla(raiz, -26f, 50f);
        LuzGlobal(raiz);

        // Arena cerrada: suelo continuo, sin precipicios, la pelea es esquivar balas
        Plataforma(raiz, -26f, 50f, ALTO_SUELO, 4f);

        // Plataformas para saltar y esquivar
        PlataformaDelgada(raiz, -16f, -8f, -1.5f);
        PlataformaDelgada(raiz, 0f, 8f, -0.8f);
        PlataformaDelgada(raiz, 16f, 24f, -1.5f);
        PlataformaDelgada(raiz, 26f, 34f, -0.8f);

        // Adornos
        Arbol(raiz, -24f); Arbol(raiz, -12f); Arbol(raiz, 6f); Arbol(raiz, 46f);
        Caja(raiz, -20f); Barril(raiz, 12f); CajaGrande(raiz, 28f);
        Planta(raiz, -2f); Planta(raiz, 20f);

        GameObject jugador = CrearJugador(raiz, new Vector3(-20f, -3.35f, 0f), balaJugador, disparo, salto, dano);

        // Dos robots de guardia antes de llegar al jefe
        CrearRobot(raiz, -8f, ALTO_SUELO, balaEnemiga);
        CrearRobot(raiz, 20f, ALTO_SUELO, balaEnemiga);

        // Provisiones para una pelea larga
        CrearRecogible(raiz, "Botiquin", new Vector3(-14f, -3.5f, 0f), ObjetoRecogible.Tipo.Vida,
            Uno(R_UI + "ui_medal_life_red.png", "ui_medal_life_red_0"));
        CrearRecogible(raiz, "Botiquin2", new Vector3(16f, -3.5f, 0f), ObjetoRecogible.Tipo.Vida,
            Uno(R_UI + "ui_medal_life_red.png", "ui_medal_life_red_0"));
        CrearRecogible(raiz, "Municion", new Vector3(2f, -3.5f, 0f), ObjetoRecogible.Tipo.Municion,
            Uno("Assets/SpriteSheets/props/ammo.png", "ammo_0"));
        CrearRecogible(raiz, "Municion2", new Vector3(30f, -3.5f, 0f), ObjetoRecogible.Tipo.Municion,
            Uno("Assets/SpriteSheets/props/ammo.png", "ammo_0"));

        // El jefe espera al fondo de la arena
        GameObject jefe = CrearJefe(raiz, new Vector3(38f, 1.5f, 0f), balaEnemiga);

        Camera cam = CrearCamara(raiz, new Vector3(-20f, 0f, -10f), new Color(0.03f, 0.05f, 0.08f));
        ConfigurarCamara(cam, jugador.transform, -26f, 50f);

        Canvas canvas = CrearCanvas(raiz);
        CrearHud(canvas.transform, jugador.GetComponent<JugadorMason>());
        CrearBarraJefe(canvas.transform, jefe.GetComponent<JefeNucleo>());
        CrearGestor(raiz);

        if (musica != null) Musica(raiz, musica, 0.35f);

        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Nivel3.unity");
    }

    // =====================================================================
    //  ESCENA: REINICIAR - pantalla de fin de partida
    // =====================================================================
    public static void ConstruirReiniciar(AudioClip musica)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform raiz = new GameObject("Final").transform;

        Camera cam = CrearCamara(raiz, new Vector3(0f, 0f, -10f), new Color(0f, 0f, 0f));
        CamaraJugador seguimiento = cam.GetComponent<CamaraJugador>();
        if (seguimiento != null) Object.DestroyImmediate(seguimiento);

        LuzGlobal(raiz);

        Canvas canvas = CrearCanvas(raiz);

        // Las dos pantallas posibles: derrota y creditos
        GameObject panelDerrota = PantallaCompleta(canvas.transform, "PanelDerrota",
            Uno(R_TITLE + "GameOverScreen-still.png", "GameOverScreen-still_0")).gameObject;
        GameObject panelVictoria = PantallaCompleta(canvas.transform, "PanelVictoria",
            Uno(R_TITLE + "CreditsScreen.png", "CreditsScreen_0")).gameObject;

        // La pantalla de derrota se queda visible por defecto: asi la escena nunca
        // se ve vacia y PantallaFinal la cambia en Start() si el jugador gano.
        panelDerrota.SetActive(true);
        panelVictoria.SetActive(false);

        GameObject objetoFinal = Nueva("PantallaFinal", raiz);
        AudioSource altavoz = objetoFinal.AddComponent<AudioSource>();
        altavoz.playOnAwake = false;

        PantallaFinal pantalla = objetoFinal.AddComponent<PantallaFinal>();
        pantalla.panelDerrota = panelDerrota;
        pantalla.panelVictoria = panelVictoria;
        pantalla.escenaSiguiente = "MenuPrincipal";
        pantalla.altavoz = altavoz;
        pantalla.sonidoAceptar = Audio(R_SND + "snd_select.ogg");

        if (musica != null) Musica(raiz, musica, 0.3f);

        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Reiniciar.unity");
    }

    // =====================================================================
    //  AYUDAS: sprites y audio
    // =====================================================================
    static Sprite Uno(string ruta, string nombre)
    {
        UnityEngine.Object[] todos = AssetDatabase.LoadAllAssetsAtPath(ruta);
        foreach (UnityEngine.Object o in todos)
        {
            Sprite s = o as Sprite;
            if (s != null && s.name == nombre) return s;
        }
        Debug.LogWarning("No encontre el sprite " + nombre + " en " + ruta);
        return null;
    }

    static Sprite[] Varios(string ruta, string prefijo, int cuantos)
    {
        List<Sprite> lista = new List<Sprite>();
        UnityEngine.Object[] todos = AssetDatabase.LoadAllAssetsAtPath(ruta);

        for (int i = 0; i < cuantos; i++)
        {
            string nombre = prefijo + "_" + i;
            foreach (UnityEngine.Object o in todos)
            {
                Sprite s = o as Sprite;
                if (s != null && s.name == nombre) { lista.Add(s); break; }
            }
        }
        return lista.ToArray();
    }

    static AudioClip Audio(string ruta)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        if (clip == null) Debug.LogWarning("No encontre el sonido " + ruta);
        return clip;
    }

    // =====================================================================
    //  AYUDAS: objetos basicos
    // =====================================================================
    static GameObject Nueva(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre, false);
        return go;
    }

    static GameObject NuevaUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        if (padre != null) go.transform.SetParent(padre, false);
        return go;
    }

    static SpriteRenderer Dibujar(GameObject go, Sprite sprite, int orden)
    {
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        return sr;
    }

    static GameObject CrearPrefabBala(string rutaPrefab, string rutaSprite, string nombreSprite,
        float velocidad, bool delJugador)
    {
        GameObject go = new GameObject(delJugador ? "BalaJugador" : "BalaEnemiga");
        Dibujar(go, Uno(rutaSprite, nombreSprite), delJugador ? 7 : 6);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.18f;

        Bala script = go.AddComponent<Bala>();
        script.velocidad = velocidad;
        script.duracion = 3f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, rutaPrefab);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // =====================================================================
    //  AYUDAS: escenario
    // =====================================================================
    static void Plataforma(Transform padre, float x1, float x2, float yArriba, float grosor)
    {
        float ancho = x2 - x1;
        GameObject raiz = Nueva("Plataforma", padre);
        raiz.transform.position = new Vector3((x1 + x2) * 0.5f, yArriba, 0f);

        BoxCollider2D col = raiz.AddComponent<BoxCollider2D>();
        col.size = new Vector2(ancho, grosor);
        col.offset = new Vector2(0f, -grosor * 0.5f);

        // Relleno macizo detras de los tiles: tapa las costuras transparentes
        // de los tiles y hace que se vea tierra y no cielo entre ellos.
        GameObject rellenoFondo = Nueva("Relleno", raiz.transform);
        SpriteRenderer srFondo = Dibujar(rellenoFondo, Uno(R_BG + "background_color.png", "background_color_0"), -2);
        if (srFondo.sprite != null)
        {
            float anchoSprite = srFondo.sprite.rect.width / PPU;
            float altoSprite = srFondo.sprite.rect.height / PPU;
            srFondo.color = new Color(0.09f, 0.06f, 0.07f, 1f);
            rellenoFondo.transform.localScale = new Vector3(ancho / anchoSprite, grosor / altoSprite, 1f);
            rellenoFondo.transform.localPosition = new Vector3(0f, -grosor * 0.5f, 0f);
        }

        GameObject cesped = Nueva("Cesped", raiz.transform);
        SpriteRenderer srCesped = Dibujar(cesped, Uno(R_TILES, "tiles_spritesheet_6"), 1);
        srCesped.drawMode = SpriteDrawMode.Tiled;
        srCesped.size = new Vector2(ancho, ALTO_CESPED);
        cesped.transform.localPosition = new Vector3(0f, -ALTO_CESPED * 0.5f, 0f);

        float altoRelleno = grosor - ALTO_CESPED;
        if (altoRelleno <= 0f) return;

        GameObject tierra = Nueva("Tierra", raiz.transform);
        SpriteRenderer srTierra = Dibujar(tierra, Uno(R_TILES, "tiles_spritesheet_18"), 0);
        srTierra.drawMode = SpriteDrawMode.Tiled;
        srTierra.size = new Vector2(ancho, altoRelleno);
        tierra.transform.localPosition = new Vector3(0f, -ALTO_CESPED - altoRelleno * 0.5f, 0f);
    }

    static void PlataformaDelgada(Transform padre, float x1, float x2, float yArriba)
    {
        Plataforma(padre, x1, x2, yArriba, ALTO_CESPED + 0.2f);
    }

    static void FondoJungla(Transform padre, float desde, float hasta)
    {
        Repetir(padre, R_BG + "background_color.png", "background_color_0", desde, hasta, 0f, -100);
        Repetir(padre, R_BG + "jungle_paralax_bg2.png", "jungle_paralax_bg2_0", desde, hasta, -1.2f, -80);
        Repetir(padre, R_BG + "jungle_paralax_bg1.png", "jungle_paralax_bg1_0", desde, hasta, -1.4f, -70);
        Repetir(padre, R_BG + "jungle_bg_trees.png", "jungle_bg_trees_0", desde, hasta, -0.7f, -60);
    }

    static void Repetir(Transform padre, string ruta, string nombreSprite, float desde, float hasta, float y, int orden)
    {
        Sprite sprite = Uno(ruta, nombreSprite);
        if (sprite == null) return;

        float ancho = sprite.rect.width / PPU;
        if (ancho <= 0.1f) return;

        GameObject grupo = Nueva("Fondo_" + nombreSprite, padre);
        float x = desde;

        while (x < hasta)
        {
            GameObject copia = Nueva(nombreSprite, grupo.transform);
            Dibujar(copia, sprite, orden);
            copia.transform.position = new Vector3(x + ancho * 0.5f, y, 0f);
            x += ancho;
        }
    }

    static void Arbol(Transform padre, float x)
    {
        GameObject go = Nueva("Arbol", padre);
        SpriteRenderer sr = Dibujar(go, Uno(R_BG + "jungle_tree1.png", "jungle_tree1_0"), -20);
        float alto = (sr.sprite != null) ? sr.sprite.rect.height / PPU : 3.7f;
        go.transform.position = new Vector3(x, ALTO_SUELO + alto * 0.5f, 0f);
    }

    static void Planta(Transform padre, float x)
    {
        GameObject go = Nueva("Planta", padre);
        SpriteRenderer sr = Dibujar(go, Uno("Assets/SpriteSheets/props/plant.png", "plant_0"), 2);
        float alto = (sr.sprite != null) ? sr.sprite.rect.height / PPU : 0.7f;
        go.transform.position = new Vector3(x, ALTO_SUELO + alto * 0.5f, 0f);
    }

    static void PropConColision(Transform padre, string nombre, string ruta, string sprite, float x)
    {
        GameObject go = Nueva(nombre, padre);
        SpriteRenderer sr = Dibujar(go, Uno(ruta, sprite), 3);
        if (sr.sprite == null) return;

        Vector2 tam = sr.sprite.bounds.size;
        go.transform.position = new Vector3(x, ALTO_SUELO + tam.y * 0.5f, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = tam;
    }

    static void Caja(Transform padre, float x)
    {
        PropConColision(padre, "Caja", "Assets/SpriteSheets/props/crate.png", "crate_0", x);
    }

    static void CajaGrande(Transform padre, float x)
    {
        PropConColision(padre, "CajaGrande", "Assets/SpriteSheets/props/bigcrate.png", "bigcrate_0", x);
    }

    static void Barril(Transform padre, float x)
    {
        PropConColision(padre, "Barril", "Assets/SpriteSheets/props/barrel.png", "barrel_0", x);
    }

    // =====================================================================
    //  AYUDAS: personajes
    // =====================================================================
    static GameObject CrearJugador(Transform padre, Vector3 pos, GameObject balaJugador,
        AudioClip disparo, AudioClip salto, AudioClip dano)
    {
        GameObject go = Nueva("Mason", padre);
        go.transform.position = pos;
        Dibujar(go, Uno(R_PLAYER + "john_idle.png", "john_idle_0"), 5);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.6f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.55f, 1.1f);
        col.offset = new Vector2(0f, 0.02f);

        AudioSource altavoz = go.AddComponent<AudioSource>();
        altavoz.playOnAwake = false;

        GameObject puntoDisparo = Nueva("PuntoDisparo", go.transform);
        puntoDisparo.transform.localPosition = new Vector3(0.6f, 0.15f, 0f);

        JugadorMason jugador = go.AddComponent<JugadorMason>();
        jugador.velocidad = 6.5f;
        jugador.fuerzaSalto = 9.5f;
        jugador.prefabBala = balaJugador;
        jugador.puntoDisparo = puntoDisparo.transform;
        jugador.cadencia = 0.22f;
        jugador.vidaMaxima = 5;
        jugador.cuadrosQuieto = Varios(R_PLAYER + "john_idle.png", "john_idle", 5);
        jugador.cuadrosCorrer = Varios(R_PLAYER + "john_run.png", "john_run", 10);
        jugador.cuadrosSalto = Varios(R_PLAYER + "john_jump.png", "john_jump", 9);
        jugador.cuadrosDisparo = Varios(R_PLAYER + "john_stand_shooting.png", "john_stand_shooting", 2);
        jugador.sonidoDisparo = disparo;
        jugador.sonidoSalto = salto;
        jugador.sonidoDano = dano;

        return go;
    }

    static GameObject CrearRobot(Transform padre, float x, float ySuelo, GameObject balaEnemiga)
    {
        GameObject go = Nueva("Robot", padre);
        go.transform.position = new Vector3(x, ySuelo + 0.6f, 0f);
        Dibujar(go, Uno(R_ENEMY + "grunt_idle.png", "grunt_idle_0"), 4);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.55f, 1.05f);

        GameObject puntoDisparo = Nueva("PuntoDisparo", go.transform);
        puntoDisparo.transform.localPosition = new Vector3(0.5f, 0.1f, 0f);

        RobotEnemigo robot = go.AddComponent<RobotEnemigo>();
        robot.vidaMaxima = 3;
        robot.prefabBala = balaEnemiga;
        robot.puntoDisparo = puntoDisparo.transform;
        robot.distanciaDeteccion = 10f;
        robot.cadencia = 1.4f;
        robot.cuadrosQuieto = Varios(R_ENEMY + "grunt_idle.png", "grunt_idle", 2);
        robot.cuadrosDisparo = Varios(R_ENEMY + "grunt_shoot.png", "grunt_shoot", 2);
        robot.cuadrosMuerte = Varios(R_ENEMY + "grunt_death.png", "grunt_death", 8);

        return go;
    }

    static GameObject CrearJefe(Transform padre, Vector3 pos, GameObject balaEnemiga)
    {
        GameObject go = Nueva("ElNucleo", padre);
        go.transform.position = pos;

        GameObject cuerpo = Nueva("Cuerpo", go.transform);
        Dibujar(cuerpo, Uno(R_BOSS + "BossBody-static.png", "BossBody-static_0"), 3);
        AnimadorSimple animCuerpo = cuerpo.AddComponent<AnimadorSimple>();
        animCuerpo.cuadros = Varios(R_BOSS + "BossBodyIdle.png", "BossBodyIdle", 12);
        animCuerpo.cuadrosPorSegundo = 8f;

        GameObject cabeza = Nueva("Cabeza", go.transform);
        Dibujar(cabeza, Uno(R_BOSS + "BossHead-static.png", "BossHead-static_0"), 4);
        cabeza.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        AnimadorSimple animCabeza = cabeza.AddComponent<AnimadorSimple>();
        animCabeza.cuadros = Varios(R_BOSS + "BossHeadIdle.png", "BossHeadIdle", 12);
        animCabeza.cuadrosPorSegundo = 8f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(10f, 5f);

        GameObject puntoDisparo = Nueva("PuntoDisparo", go.transform);
        puntoDisparo.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        JefeNucleo jefe = go.AddComponent<JefeNucleo>();
        jefe.vidaMaxima = 40;
        jefe.prefabBala = balaEnemiga;
        jefe.puntoDisparo = puntoDisparo.transform;
        jefe.cadencia = 1.2f;
        jefe.aperturaAbanico = 12f;
        jefe.amplitud = 1.2f;
        jefe.velocidadBalanceo = 1.1f;

        return go;
    }

    // =====================================================================
    //  AYUDAS: recogibles, meta, camara, luz, HUD
    // =====================================================================
    static void CrearRecogible(Transform padre, string nombre, Vector3 pos, ObjetoRecogible.Tipo tipo, Sprite sprite)
    {
        GameObject go = Nueva(nombre, padre);
        go.transform.position = pos;
        Dibujar(go, sprite, 3);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        ObjetoRecogible recogible = go.AddComponent<ObjetoRecogible>();
        recogible.tipo = tipo;
        recogible.cantidadVida = 1;
        recogible.segundosDeMejora = 10f;
        recogible.sonido = Audio(R_SND + "snd_snap.ogg");
    }

    static void CrearMeta(Transform padre, float x, string escenaSiguiente)
    {
        GameObject go = Nueva("Meta", padre);
        go.transform.position = new Vector3(x, ALTO_SUELO + 3f, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 6f);
        col.isTrigger = true;

        MetaNivel meta = go.AddComponent<MetaNivel>();
        meta.escenaSiguiente = escenaSiguiente;
    }

    static Camera CrearCamara(Transform padre, Vector3 pos, Color colorFondo)
    {
        GameObject go = Nueva("Main Camera", padre);
        go.tag = "MainCamera";
        go.transform.position = pos;

        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = TAMANO_CAMARA;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = colorFondo;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;

        go.AddComponent<AudioListener>();
        go.AddComponent<CamaraJugador>();

        return cam;
    }

    static void ConfigurarCamara(Camera cam, Transform objetivo, float limiteIzquierdo, float limiteDerecho)
    {
        CamaraJugador seguimiento = cam.GetComponent<CamaraJugador>();
        if (seguimiento == null) return;

        seguimiento.objetivo = objetivo;
        seguimiento.limiteIzquierdo = limiteIzquierdo;
        seguimiento.limiteDerecho = limiteDerecho;
        seguimiento.altura = 0f;
        seguimiento.suavizado = 8f;
    }

    static void LuzGlobal(Transform padre)
    {
        GameObject go = Nueva("Global Light 2D", padre);
        Light2D luz = go.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 1f;
        luz.color = Color.white;
    }

    static void Musica(Transform padre, AudioClip clip, float volumen)
    {
        GameObject go = Nueva("Musica", padre);
        AudioSource fuente = go.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.loop = true;
        fuente.volume = volumen;
        fuente.playOnAwake = true;
    }

    static Canvas CrearCanvas(Transform padre)
    {
        GameObject go = Nueva("Canvas", padre);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(320f, 180f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void CrearHud(Transform canvas, JugadorMason jugador)
    {
        GameObject fila = NuevaUI("Vidas", canvas);

        RectTransform rt = fila.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(6f, -6f);
        rt.sizeDelta = new Vector2(36f, 11f);

        HudVidas hud = fila.AddComponent<HudVidas>();
        hud.jugador = jugador;
        hud.iconoLleno = Uno(R_UI + "life_unit.png", "life_unit_0");
        hud.iconoVacio = Uno(R_UI + "life_empty.png", "life_empty_0");
        hud.iconos = new Image[jugador.vidaMaxima];

        for (int i = 0; i < jugador.vidaMaxima; i++)
        {
            GameObject icono = NuevaUI("Vida" + i, fila.transform);

            Image img = icono.AddComponent<Image>();
            img.sprite = hud.iconoLleno;
            img.raycastTarget = false;

            RectTransform rti = icono.GetComponent<RectTransform>();
            rti.anchorMin = new Vector2(0f, 1f);
            rti.anchorMax = new Vector2(0f, 1f);
            rti.pivot = new Vector2(0f, 1f);
            rti.anchoredPosition = new Vector2(i * 7f, 0f);
            rti.sizeDelta = new Vector2(6f, 10f);

            hud.iconos[i] = img;
        }

        CrearBarraMunicion(canvas, jugador);
    }

    // Barra de municion: marco oscuro + relleno amarillo que se encoge
    static void CrearBarraMunicion(Transform canvas, JugadorMason jugador)
    {
        GameObject marco = NuevaUI("BarraMunicion", canvas);

        RectTransform rt = marco.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(6f, -22f);
        rt.sizeDelta = new Vector2(62f, 8f);

        Image fondo = marco.AddComponent<Image>();
        fondo.color = new Color(0.06f, 0.03f, 0.05f, 0.85f);
        fondo.raycastTarget = false;

        GameObject barra = NuevaUI("Relleno", marco.transform);

        Image relleno = barra.AddComponent<Image>();
        relleno.color = new Color(0.98f, 0.80f, 0.20f, 1f);
        relleno.raycastTarget = false;

        RectTransform rtb = barra.GetComponent<RectTransform>();
        rtb.anchorMin = new Vector2(0f, 0.5f);
        rtb.anchorMax = new Vector2(0f, 0.5f);
        rtb.pivot = new Vector2(0f, 0.5f);
        rtb.anchoredPosition = new Vector2(2f, 0f);
        rtb.sizeDelta = new Vector2(58f, 4f);

        HudMunicion hud = marco.AddComponent<HudMunicion>();
        hud.jugador = jugador;
        hud.barra = rtb;
        hud.anchoTotal = 58f;
    }

    static void CrearBarraJefe(Transform canvas, JefeNucleo jefe)
    {
        GameObject marco = NuevaUI("BarraJefe", canvas);

        RectTransform rt = marco.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -6f);
        rt.sizeDelta = new Vector2(160f, 10f);

        Image fondo = marco.AddComponent<Image>();
        fondo.color = new Color(0.06f, 0.03f, 0.05f, 0.85f);
        fondo.raycastTarget = false;

        GameObject barra = NuevaUI("Relleno", marco.transform);

        Image relleno = barra.AddComponent<Image>();
        relleno.color = new Color(0.85f, 0.14f, 0.22f, 1f);
        relleno.raycastTarget = false;

        RectTransform rtb = barra.GetComponent<RectTransform>();
        rtb.anchorMin = new Vector2(0f, 0.5f);
        rtb.anchorMax = new Vector2(0f, 0.5f);
        rtb.pivot = new Vector2(0f, 0.5f);
        rtb.anchoredPosition = new Vector2(2f, 0f);
        rtb.sizeDelta = new Vector2(156f, 6f);

        HudJefe hud = marco.AddComponent<HudJefe>();
        hud.jefe = jefe;
        hud.barra = rtb;
        hud.anchoTotal = 156f;
    }

    static Image PantallaCompleta(Transform canvas, string nombre, Sprite sprite)
    {
        GameObject go = NuevaUI(nombre, canvas);

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }

    static void CrearGestor(Transform raiz)
    {
        GameObject go = Nueva("GestorJuego", raiz);
        GestorJuego gestor = go.AddComponent<GestorJuego>();
        gestor.escenaFinal = "Reiniciar";
    }
}
