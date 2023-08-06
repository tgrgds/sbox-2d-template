using Sandbox;
using System;
using System.Diagnostics;
using System.Linq;
using static MyGame.MyGame;
using System.Collections.Generic;
using Sandbox.Diagnostics;
using System.Numerics;

namespace MyGame;

public partial class Player : Thing
{
	public const float BASE_MOVE_SPEED = 15f;

	public override void Spawn()
	{
		base.Spawn();

		if ( Sandbox.Game.IsServer )
		{
			SpriteTexture = SpriteTexture.Atlas( "textures/sprites/player_spritesheet_1.png", 5, 4 );
			BasePivotY = 0.05f;
			HeightZ = 0f;

			CollideWith.Add( typeof( Player ) );

			Predictable = true;

			AnimationPath = "textures/sprites/player_idle.frames";
			AnimationSpeed = 0.66f;
			GridPos = Game.GetGridSquareForPos( Position );

			ColorTint = Color.White;
			EnableDrawing = true;
			TempWeight = 0f;
			ShadowOpacity = 0.8f;
			ShadowScale = 1.12f;
		}
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		// TODO: spawn nametag
		SpawnShadow( 1.12f );
	}

	[GameEvent.Tick.Server]
	public void ServerTick()
	{

	}

	[GameEvent.Tick.Client]
	public void ClientTick()
	{
		if ( this == Sandbox.Game.LocalPawn )
		{
			Sound.Listener = new()
			{
				Position = new Vector3( Position.x, Position.y, 512f ),
				Rotation = global::Rotation.LookAt( new Vector3( 0f, 1f, 0f ) )
			};
		}
	}

	protected override void OnSimulate( IClient cl )
	{
		float dt = Time.Delta;

		Vector2 inputVector = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x );

		if ( inputVector.LengthSquared > 0f )
			Velocity += inputVector.Normal * BASE_MOVE_SPEED * dt; // * movespeed playerstat

		Position += Velocity * dt;

		Velocity = Utils.DynamicEaseTo( Velocity, Vector2.Zero, 0.2f, dt );
		TempWeight *= (1f - dt * 4.7f);

		Rotation = Velocity.Length * MathF.Cos( Time.Now * MathF.PI * 7f ) * 1.5f;

		Depth = -Position.y * 10f;

		if ( MathF.Abs( Input.AnalogMove.y ) > 0f )
			Scale = new Vector2( 1f * Input.AnalogMove.y < 0f ? -1f : 1f, 1f ) * 1f;

		if ( Sandbox.Game.IsClient )
		{
			Camera2D.Current.TargetPosition = Position;
		}

		if ( Sandbox.Game.IsServer )
		{
			bool moving = Velocity.LengthSquared > 0.01f && inputVector.LengthSquared > 0.1f;
			AnimationPath = $"textures/sprites/player_{(moving ? "walk" : "idle")}.frames";
			AnimationSpeed = moving ? Utils.Map( Velocity.Length, 0f, 2f, 1.5f, 2f ) : 0.66f;

			var gridPos = Game.GetGridSquareForPos( Position );
			if ( gridPos != GridPos )
			{
				Game.DeregisterThingGridSquare( this, gridPos );
				Game.RegisterThingGridSquare( this, GridPos );
				GridPos = gridPos;
			}

			// for ( int dx = -1; dx <= 1; dx++ )
			// {
			// 	for ( int dy = -1; dy <= 1; dy++ )
			// 	{
			// 		Game.HandleThingCollisionForGridSquare( this, new GridSquare( GridPos.x + dx, GridPos.y + dy ), dt );
			// 	}
			// }
		}
	}
}
