using Sandbox;
using Sandbox.UI;
using System;

namespace MainMenu;

[StyleSheet( "mainmenu.cs.scss" )]
public sealed class MainMenuHud : PanelComponent
{
	[Property] public string MusicEvent { get; set; } = "sounds/music/Magic Mamaliga.sound";
	[Property] public string DiscordUrl { get; set; } = "https://discord.gg/WhGwYJAK7E";

	private SoundHandle? _music;
	private Panel _root;

	protected override void OnTreeFirstBuilt()
	{
		base.OnTreeFirstBuilt();
		Panel.DeleteChildren();

		_root = new Panel
		{
			Parent = Panel
		};
		_root.AddClass( "main-menu-root" );

		// --- ЛЕВАЯ ЧАСТЬ (ЗАГОЛОВОК) ---
		var titleContainer = new Panel { Parent = _root };
		titleContainer.AddClass( "title-container" );

		var title = new Label( "NIGHTMARE\nROUTE" ) { Parent = titleContainer };
		title.AddClass( "game-title" );

		var subtitle = new Label( "Pre-Alpha" ) { Parent = titleContainer };
		subtitle.AddClass( "game-subtitle" );

		var buttonContainer = new Panel { Parent = _root };
		buttonContainer.AddClass( "button-container" );

		var playButton = new MenuButton( "ИГРАТЬ" ) { Parent = buttonContainer };
		playButton.Clicked += OnStartClicked;

		var discordButton = new MenuButton( "DISCORD" ) { Parent = buttonContainer };
		discordButton.AddClass( "discord-btn" );
		discordButton.Clicked += () =>
		{
			Log.Info( $"Попытка открыть Discord: {DiscordUrl}" );
			
			Clipboard.SetText( DiscordUrl );
			Log.Info( "Ссылка на Discord скопирована в буфер обмена!" );
		};

		var quitButton = new MenuButton( "ВЫХОД" ) { Parent = buttonContainer };
		quitButton.Clicked += () => Game.Close();

		PlayMusic();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( _music == null || !_music.IsValid || !_music.IsPlaying )
		{
			PlayMusic();
		}
	}

	private void OnStartClicked()
	{
		Game.ActiveScene.LoadFromFile( "scenes/testmap.scene" );
	}

	private void PlayMusic()
	{
		if ( string.IsNullOrWhiteSpace( MusicEvent ) )
		{
			Log.Warning( "MusicEvent empty - menu music won't play." );
			return;
		}

		try
		{
			Sound.Preload( MusicEvent );

			_music?.Stop();
			_music = Sound.Play( MusicEvent, 0.25f );

			Log.Info( $"Menu music started: {MusicEvent}" );
		}
		catch ( Exception e )
		{
			Log.Error( $"Failed to play music event '{MusicEvent}': {e}" );
		}
	}

	protected override void OnDestroy()
	{
		_music?.Stop( 0.25f );
		base.OnDestroy();
	}

	private sealed class MenuButton : Panel
	{
		public event Action Clicked;

		public MenuButton( string text )
		{
			AddClass( "menu-button" );

			var label = new Label( text )
			{
				Parent = this
			};
			label.AddClass( "btn-text" );
		}

		protected override void OnClick( MousePanelEvent e )
		{
			base.OnClick( e );
			Clicked?.Invoke();
		}
	}
}