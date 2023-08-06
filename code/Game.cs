using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyGame;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class MyGame : GameManager
{
	public new static MyGame Current => GameManager.Current as MyGame;

	private readonly List<Thing> _things = new();

	public record struct GridSquare( int x, int y );
	public Dictionary<GridSquare, List<Thing>> ThingGridPositions = new();

	public float GRID_SIZE = 1f;
	public Vector2 BOUNDS_MIN;
	public Vector2 BOUNDS_MAX;
	public Vector2 BOUNDS_MIN_SPAWN;
	public Vector2 BOUNDS_MAX_SPAWN;

	public BackgroundManager BackgroundManager { get; private set; } // client only

	public MyGame()
	{
		BOUNDS_MIN = new Vector2( -16f, -12f );
		BOUNDS_MAX = new Vector2( 16f, 12f );
		BOUNDS_MIN_SPAWN = new Vector2( -15.5f, -11.5f );
		BOUNDS_MAX_SPAWN = new Vector2( 15.5f, 11.5f );

		if ( Sandbox.Game.IsServer )
		{
			for ( float x = BOUNDS_MIN.x; x < BOUNDS_MAX.x; x += GRID_SIZE )
			{
				for ( float y = BOUNDS_MIN.y; y < BOUNDS_MAX.y; y += GRID_SIZE )
				{
					ThingGridPositions.Add( GetGridSquareForPos( new Vector2( x, y ) ), new List<Thing>() );
				}
			}

		}

		if ( Sandbox.Game.IsClient )
		{
			//_ = new MainHud();
			// Hud = new HUD();
			BackgroundManager = new BackgroundManager();
		}
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Player();
		client.Pawn = pawn;

		// // Get all of the spawnpoints
		// var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// // chose a random one
		// var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// // if it exists, place the pawn there
		// if ( randomSpawnPoint != null )
		// {
		// 	var tx = randomSpawnPoint.Transform;
		// 	tx.Position += Vector3.Up * 50.0f; // raise it up
		// 	pawn.Transform = tx;
		// }
	}

	public void HandleThingCollisionForGridSquare( Thing thing, GridSquare gridSquare, float dt )
	{
		if ( !ThingGridPositions.ContainsKey( gridSquare ) )
			return;

		var things = ThingGridPositions[gridSquare];
		if ( things.Count == 0 )
			return;

		for ( int i = things.Count - 1; i >= 0; i-- )
		{
			if ( i >= things.Count )
				continue;
			//Log.Info("!!! " + thing.Name + " --- " + i.ToString() + " count: " + things.Count);

			if ( thing == null || !thing.IsValid || thing.IsRemoved )
				return;

			var other = things[i];
			if ( other == thing || other.IsRemoved || !other.IsValid )
				continue;

			bool isValidType = false;
			foreach ( var t in thing.CollideWith )
			{
				if ( t.IsAssignableFrom( other.GetType() ) )
				{
					isValidType = true;
					break;
				}
			}

			if ( !isValidType )
				continue;

			var dist_sqr = (thing.Position - other.Position).LengthSquared;
			var total_radius_sqr = MathF.Pow( thing.Radius + other.Radius, 2f );
			if ( dist_sqr < total_radius_sqr )
			{
				float percent = Utils.Map( dist_sqr, total_radius_sqr, 0f, 0f, 1f );
				//thing.Velocity += (thing.Position - other.Position).Normal * Utils.Map(dist_sqr, total_radius_sqr, 0f, 0f, 10f) * (1f + other.TempWeight) * dt;
				thing.Colliding( other, percent, dt * thing.TimeScale );
			}
		}
	}

	public void AddThingsInGridSquare( GridSquare gridSquare, List<Thing> things )
	{
		if ( !ThingGridPositions.ContainsKey( gridSquare ) )
			return;

		things.AddRange( ThingGridPositions[gridSquare] );
	}

	public bool IsGridSquareInArena( GridSquare gridSquare )
	{
		return ThingGridPositions.ContainsKey( gridSquare );
	}

	public void RegisterThingGridSquare( Thing thing, GridSquare gridSquare )
	{
		if ( IsGridSquareInArena( gridSquare ) )
			ThingGridPositions[gridSquare].Add( thing );
	}

	public void DeregisterThingGridSquare( Thing thing, GridSquare gridSquare )
	{
		if ( ThingGridPositions.ContainsKey( gridSquare ) && ThingGridPositions[gridSquare].Contains( thing ) )
		{
			ThingGridPositions[gridSquare].Remove( thing );
		}
	}

	public void AddThing( Thing thing )
	{
		_things.Add( thing );
		thing.GridPos = GetGridSquareForPos( thing.Position );
		RegisterThingGridSquare( thing, thing.GridPos );
	}

	public void RemoveThing( Thing thing )
	{
		if ( ThingGridPositions.ContainsKey( thing.GridPos ) )
		{
			ThingGridPositions[thing.GridPos].Remove( thing );
		}
	}

	public GridSquare GetGridSquareForPos( Vector2 pos )
	{
		return new GridSquare( (int)MathF.Floor( pos.x ), (int)MathF.Floor( pos.y ) );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Camera2D.Current = new Camera2D();

		BackgroundManager?.Restart();
	}

	[GameEvent.Client.Frame]
	public void ClientFrame()
	{
		Camera2D.Current?.Update();
	}
}
