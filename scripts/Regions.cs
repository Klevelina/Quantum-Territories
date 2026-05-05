using Godot;
using System.Collections.Generic;

public partial class Regions : Node
{
	public int Owner = -1;

	[Export] public int RegionIndex;

	public Dictionary<int, int> Units = new Dictionary<int, int>()
	{
		{0, 0},
		{1, 0}
	};

	public List<Regions> Neighbors = new List<Regions>();

	public enum LandState { Empty, Farm, Unstable }
	public LandState State = LandState.Empty;

	public bool Contested = false;

	[Export] public string RegionID;
	[Export] public string[] NeighborIDs;

	public List<Vector2I> Tiles = new List<Vector2I>();

	public void AddUnit(int player)
	{
		Units[player]++;
		UpdateState();
	}

	public void UpdateState()
	{
		int p0 = Units[0];
		int p1 = Units[1];

		if (p0 > 0 && p1 > 0)
		{
			Contested = true;
			Owner = -1;
			State = LandState.Unstable;
		}
		else if (p0 > 0)
		{
			Owner = 0;
			Contested = false;
		}
		else if (p1 > 0)
		{
			Owner = 1;
			Contested = false;
		}
		else
		{
			Owner = -1;
			Contested = false;
		}
	}

	// ✅ CORE FIX: use SOURCE ID instead of atlas coords
	public void GenerateTilesFromMap(TileMapLayer regionLayer)
	{
		Tiles.Clear();

		foreach (Vector2I cell in regionLayer.GetUsedCells())
		{
			Vector2I atlasCoords = regionLayer.GetCellAtlasCoords(cell);

			// Use X as region index (tile position in atlas)
			if (atlasCoords.X == RegionIndex)
			{
				Tiles.Add(cell);
			}
		}

		GD.Print($"{RegionID} -> {Tiles.Count} tiles");
	}
}
