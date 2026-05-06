using Sandbox;
using System;

/// <summary>
/// Реалистичное солнце + правильный ambient + поддержка запечённого света.
/// 
/// НАСТРОЙКА:
/// 1. Пустой GameObject "Sun" → DirectionalLight + этот скрипт
/// 2. Ambient = почти чёрный (0.02) — внутри зданий будет ТЕМНО без ламп
/// 3. Добавь IndirectLightVolume на отдельный GameObject — покрой им всю карту
/// 4. Нажми Bake в инспекторе IndirectLightVolume
/// 5. Добавь PointLight/SpotLight внутри зданий для освещения
/// </summary>
public sealed class CustomOurSun : Component
{
    // ── Время ──

    [Property, Range( 1f, 120f )]
    public float FullDayMinutes { get; set; } = 10f;

    [Property, Range( 0f, 60f )]
    public float LatitudeTilt { get; set; } = 23.5f;

    [Property, Range( 0f, 1f )]
    public float TimeOfDay { get; set; } = 0.35f;

    // ── Компоненты ──

    [Property] public DirectionalLight SunLight { get; set; }
    [Property] public SceneSkyBox SkyBox { get; set; }

    // ── Ambient контроль ──
    // Чем ниже — тем темнее внутри без ламп.
    // 0.02 = почти чёрный в здании, 0.15 = видно но темно, 0.3 = светло внутри

    [Property, Range( 0f, 0.3f ), Group( "Ambient" )]
    public float MaxAmbient { get; set; } = 0.02f;

    [Property, Range( 0f, 0.1f ), Group( "Ambient" )]
    public float NightAmbient { get; set; } = 0.005f;

    // ── Цвета солнца ──

    private static readonly Color SunNight     = new Color( 0f, 0f, 0f );
    private static readonly Color SunDawn      = new Color( 1.0f, 0.45f, 0.2f );
    private static readonly Color SunMorning   = new Color( 1.0f, 0.85f, 0.6f );
    private static readonly Color SunNoon      = new Color( 1.0f, 0.98f, 0.92f );
    private static readonly Color SunAfternoon = new Color( 1.0f, 0.9f, 0.7f );
    private static readonly Color SunDusk      = new Color( 1.0f, 0.35f, 0.12f );

    // ── Цвета неба ──

    private static readonly Color SkyNight = new Color( 0.005f, 0.005f, 0.015f );
    private static readonly Color SkyDawn  = new Color( 0.45f, 0.25f, 0.15f );
    private static readonly Color SkyDay   = new Color( 0.35f, 0.5f, 0.75f );
    private static readonly Color SkyDusk  = new Color( 0.5f, 0.2f, 0.1f );

    // ── Skybox tint ──

    private static readonly Color TintNight = new Color( 0.3f, 0.3f, 0.5f );
    private static readonly Color TintDawn  = new Color( 1.0f, 0.6f, 0.4f );
    private static readonly Color TintDay   = new Color( 1.0f, 1.0f, 1.0f );
    private static readonly Color TintDusk  = new Color( 1.0f, 0.5f, 0.3f );

    // ── Яркость ──

    private const float DawnBrightness     = 1.0f;
    private const float MorningBrightness  = 2.0f;
    private const float NoonBrightness     = 3.0f;
    private const float AfternoonBrightness = 2.2f;
    private const float DuskBrightness     = 0.8f;

    protected override void OnUpdate()
    {
        if ( !SunLight.IsValid() ) return;

        var delta = Time.Delta / (FullDayMinutes * 60f);
        TimeOfDay = (TimeOfDay + delta) % 1f;

        UpdateSunRotation();
        UpdateLighting();
        UpdateShadows();
    }

    // ═══════════════════════════════════════════
    //  Позиция солнца
    // ═══════════════════════════════════════════

    private void UpdateSunRotation()
    {
        float elevation = (TimeOfDay - 0.25f) * 360f;
        GameObject.WorldRotation = Rotation.From( new Angles( elevation, LatitudeTilt, 0f ) );
    }

    // ═══════════════════════════════════════════
    //  Цвет и яркость
    // ═══════════════════════════════════════════

    private void UpdateLighting()
    {
        float t = TimeOfDay;

        // ── Солнечный свет ──
        Color sunColor;
        float brightness;

        if ( t < 0.2f )
        {
            float p = t / 0.2f;
            sunColor = SunNight.LerpTo( SunDawn, S( p ) );
            brightness = MathX.Lerp( 0f, DawnBrightness, S( p ) );
        }
        else if ( t < 0.3f )
        {
            float p = (t - 0.2f) / 0.1f;
            sunColor = SunDawn.LerpTo( SunMorning, S( p ) );
            brightness = MathX.Lerp( DawnBrightness, MorningBrightness, p );
        }
        else if ( t < 0.45f )
        {
            float p = (t - 0.3f) / 0.15f;
            sunColor = SunMorning.LerpTo( SunNoon, p );
            brightness = MathX.Lerp( MorningBrightness, NoonBrightness, p );
        }
        else if ( t < 0.55f )
        {
            sunColor = SunNoon;
            brightness = NoonBrightness;
        }
        else if ( t < 0.7f )
        {
            float p = (t - 0.55f) / 0.15f;
            sunColor = SunNoon.LerpTo( SunAfternoon, p );
            brightness = MathX.Lerp( NoonBrightness, AfternoonBrightness, p );
        }
        else if ( t < 0.8f )
        {
            float p = (t - 0.7f) / 0.1f;
            sunColor = SunAfternoon.LerpTo( SunDusk, S( p ) );
            brightness = MathX.Lerp( AfternoonBrightness, DuskBrightness, S( p ) );
        }
        else
        {
            float p = (t - 0.8f) / 0.2f;
            sunColor = SunDusk.LerpTo( SunNight, S( p ) );
            brightness = MathX.Lerp( DuskBrightness, 0f, S( p ) );
        }

        SunLight.LightColor = sunColor * brightness;

        // ── Sky Color ──
        Color skyColor;
        if ( t < 0.22f )
            skyColor = SkyNight.LerpTo( SkyDawn, S( t / 0.22f ) );
        else if ( t < 0.35f )
            skyColor = SkyDawn.LerpTo( SkyDay, (t - 0.22f) / 0.13f );
        else if ( t < 0.65f )
            skyColor = SkyDay;
        else if ( t < 0.78f )
            skyColor = SkyDay.LerpTo( SkyDusk, (t - 0.65f) / 0.13f );
        else
            skyColor = SkyDusk.LerpTo( SkyNight, S( (t - 0.78f) / 0.22f ) );

        SunLight.SkyColor = skyColor;

        // ── Ambient — МИНИМАЛЬНЫЙ, чтобы внутри было темно ──
        // Высота солнца влияет на ambient (чуть светлее днём на улице)
        float sunHeight = MathF.Sin( (TimeOfDay - 0.25f) * MathF.PI * 2f );
        float amb = MathX.Lerp( NightAmbient, MaxAmbient, MathF.Max( 0f, sunHeight ) );

        if ( Scene.SceneWorld.IsValid() )
        {
            Scene.SceneWorld.AmbientLightColor = new Color( amb, amb, amb );
        }

        // ── Skybox tint ──
        if ( SkyBox.IsValid() )
        {
            Color tint;
            if ( t < 0.22f )
                tint = TintNight.LerpTo( TintDawn, S( t / 0.22f ) );
            else if ( t < 0.35f )
                tint = TintDawn.LerpTo( TintDay, (t - 0.22f) / 0.13f );
            else if ( t < 0.65f )
                tint = TintDay;
            else if ( t < 0.78f )
                tint = TintDay.LerpTo( TintDusk, (t - 0.65f) / 0.13f );
            else
                tint = TintDusk.LerpTo( TintNight, S( (t - 0.78f) / 0.22f ) );

            SkyBox.SkyTint = tint;
        }
    }

    // ═══════════════════════════════════════════
    //  Тени
    // ═══════════════════════════════════════════

    private void UpdateShadows()
    {
        float t = TimeOfDay;
        bool isDay = t > 0.22f && t < 0.78f;

        if ( !isDay )
        {
            SunLight.Shadows = false;
            return;
        }

        SunLight.Shadows = true;
        SunLight.ShadowCascadeCount = 4;
        SunLight.ShadowCascadeSplitRatio = 0.91f;
        SunLight.ShadowBias = 0.0003f;
        SunLight.ShadowHardness = 0.0f;
    }

    private static float S( float t ) => Math.Clamp( t, 0f, 1f ) * t * (3f - 2f * t );
}