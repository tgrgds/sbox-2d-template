using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace MyGame;

public partial class Shadow : Sprite
{
	public Thing Thing { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SpriteTexture = "textures/sprites/shadow3.png";
		Depth = -218f;
		ColorTint = new Color( 0f, 0f, 0f, 0f );
		Filter = SpriteFilter.Pixelated;
		Scale = new Vector2( 1f, 1f );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		//SpriteTexture = "textures/sprites/shadow3.png";
		//Depth = -50f;
		//Scale = 1f;
		//ColorFill = new Color(0f, 0f, 0f);
		//Filter = SpriteFilter.Pixelated;
		//Scale = new Vector2(2f, 2f);

		// Log.Info("ClientSpawn");
	}

	public void SetThing( Thing thing )
	{
		Thing = thing;
		Filter = SpriteFilter.Pixelated;

		Parent = Thing;
	}

	[GameEvent.Tick.Client]
	public void ClientTick()
	{
		if ( Thing == null || !Thing.IsValid() )
		{
			Delete();
			return;
		}

		LocalPosition = Vector2.Zero;
		ColorTint = new Color( 0f, 0f, 0f, Thing.ShadowOpacity );
		Scale = Thing.ShadowScale;

		//DebugOverlay.Text("ColorFill: " + ColorFill.ToString(), Position + new Vector2(0.1f, -0.1f), 0f, float.MaxValue);
	}
}
