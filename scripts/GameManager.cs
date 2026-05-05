using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	private Dictionary<Vector2I, ControlPointData> controlPoints = new();
	private Dictionary<Vector2I, List<Vector2I>> connections = new();

	private Vector2I? selectedCP = null;
	private Vector2I? fortifyFrom = null;
	private bool hasFortified = false;

	public int CurrentPlayer = 0;
	public int Stability = 20;

	public enum TurnPhase { Reinforce, Attack, Fortify }
	public TurnPhase CurrentPhase = TurnPhase.Reinforce;

	public int pendingReinforcements = 3;

	private Label turnLabel;
	private Label reinforceLabel;
	private Label statusLabel;
	private Label eventLabel;

	private Button endTurnButton;
	private Button endPhaseButton;

	// =========================
	public class ControlPointData
	{
		public Vector2I Position;
		public int Owner = -1;
		public int Strength = 5;
		public int Value = 1;
		public ControlPoint Node;
	}

	public override void _Ready()
	{
		RegisterControlPoints();
		SetupConnections();

		// ✅ FIXED PATHS (CanvasLayer is key)
		turnLabel = GetNodeOrNull<Label>("UI/TurnLabel");
		reinforceLabel = GetNodeOrNull<Label>("UI/ReinforceLabel");
		statusLabel = GetNodeOrNull<Label>("UI/StatusLabel");
		eventLabel = GetNodeOrNull<Label>("UI/EventLabel");

		endTurnButton = GetNodeOrNull<Button>("UI/EndTurnButton");
		endPhaseButton = GetNodeOrNull<Button>("UI/EndPhaseButton");

		if (endTurnButton != null)
			endTurnButton.Pressed += OnEndTurnPressed;

		if (endPhaseButton != null)
			endPhaseButton.Pressed += OnEndPhasePressed;
		// ✅ ensure first reinforce works
		CurrentPlayer = 0;
		CurrentPhase = TurnPhase.Reinforce;
		pendingReinforcements = 3;

		DrawAll();
		UpdateUI();
	}

	// =========================
	void OnEndTurnPressed()
	{
		CurrentPlayer = (CurrentPlayer + 1) % 2;
		StartReinforcePhase();

		if (eventLabel != null)
			eventLabel.Text = $"Player {CurrentPlayer} turn";

		DrawAll();
		UpdateUI();
	}

	void CheckWin()
	{
		int owner = -1;

		foreach (var cp in controlPoints.Values)
		{
			if (owner == -1)
				owner = cp.Owner;
			else if (cp.Owner != owner)
				return; // not all owned by same player
		}

		// WIN
		if (eventLabel != null)
			eventLabel.Text = $"Player {owner} WINS!";
	}

	void OnEndPhasePressed()
	{
		// block skipping reinforce
		if (CurrentPhase == TurnPhase.Reinforce && pendingReinforcements > 0)
		{
			eventLabel.Text = "Use all reinforcements first!";
			return;
		}

		if (CurrentPhase == TurnPhase.Reinforce)
		{
			CurrentPhase = TurnPhase.Attack;
			eventLabel.Text = "Attack phase";
		}
		else if (CurrentPhase == TurnPhase.Attack)
		{
			CurrentPhase = TurnPhase.Fortify;
			eventLabel.Text = "Fortify phase";
		}
		else // Fortify → End Turn
		{
			CurrentPlayer = (CurrentPlayer + 1) % 2;
			StartReinforcePhase();

			eventLabel.Text = $"Player {CurrentPlayer} turn";

			// 👉 if AI player
			if (CurrentPlayer == 1)
				DoAITurn();
		}

		selectedCP = null;
		DrawAll();
		UpdateUI();
	}

	void StartReinforcePhase()
	{
		CurrentPhase = TurnPhase.Reinforce;
		pendingReinforcements = 3;
		selectedCP = null;

		hasFortified = false;
		fortifyFrom = null;
	}

	// =========================
	void RegisterControlPoints()
	{
		int i = 0;
		var neutralPos = new Vector2I(21, 11);

		foreach (Node n in GetTree().GetNodesInGroup("cp"))
		{
			var cp = n as ControlPoint;
			if (cp == null) continue;

			Vector2I cell = cp.Cell;
			cp.Clicked += OnControlPointClicked;

			Vector2I p1Start = new Vector2I(10, 2);
			Vector2I p2Start = new Vector2I(36, 19);

			int owner = (cell.DistanceTo(p1Start) < cell.DistanceTo(p2Start)) ? 0 : 1;
			int strength = 5;

			// 👇 FORCE neutral
			if (cell == neutralPos)
			{
				owner = -1;
				strength = 3;
			}

			controlPoints[cell] = new ControlPointData
			{
				Position = cell,
				Node = cp,
				Owner = owner,
				Strength = 5,
				Value = GD.RandRange(1, 3)
			};
		}
	}

	void SetupConnections()
	{
		foreach (var key in controlPoints.Keys)
			connections[key] = new List<Vector2I>();

		// DEFINE YOUR MAP HERE
		ConnectCP(22,2, 10,2);
		// ConnectCP(10,2, 22,2);
		ConnectCP(10,2, 8,11);
		// ConnectCP(8,11, 10,2);
		ConnectCP(8,11, 21,11);
		// ConnectCP(21,11, 8,11);
		ConnectCP(22,2, 21,11);
		// ConnectCP(21,11, 22,2);

		ConnectCP(21,11, 25,19);
		// ConnectCP(25,19, 21,11);
		ConnectCP(25,19, 36,19);
		// ConnectCP(36,19, 25,19);

		ConnectCP(36,19, 36,8);
		// ConnectCP(36,8, 36,19);

		ConnectCP(36,8, 21,11);
		// ConnectCP(21,11, 36,8);
	}

	void ConnectCP(int x1, int y1, int x2, int y2)
	{
		var a = new Vector2I(x1, y1);
		var b = new Vector2I(x2, y2);

		if (!controlPoints.ContainsKey(a) || !controlPoints.ContainsKey(b))
		{
			GD.Print($"Invalid connection: {a} -> {b}");
			return;
		}

		Connect(a, b);
	}

	void Connect(Vector2I a, Vector2I b)
	{
		if (!connections[a].Contains(b))
			connections[a].Add(b);

		if (!connections[b].Contains(a))
			connections[b].Add(a);
	}

	bool IsConnected(Vector2I a, Vector2I b)
	{
		return connections.ContainsKey(a) && connections[a].Contains(b);
	}

	// =========================
	public void OnControlPointClicked(Vector2I cell)
	{
		if (!controlPoints.ContainsKey(cell)) return;

		var cp = controlPoints[cell];

		// =========================
		// FORTIFY PHASE
		// =========================
		if (CurrentPhase == TurnPhase.Fortify)
		{
			if (cp.Owner != CurrentPlayer)
			{
				eventLabel.Text = "Select your own CP";
				return;
			}

			// first click = select source
			if (selectedCP == null)
			{
				selectedCP = cell;
				eventLabel.Text = "Select destination";
				DrawAll();
				return;
			}

			// same click = cancel
			if (cell == selectedCP)
			{
				selectedCP = null;
				DrawAll();
				return;
			}

			var from = controlPoints[selectedCP.Value];
			var to = cp;

			if (!HasPath(from.Position, to.Position, CurrentPlayer))
			{
				eventLabel.Text = "Not connected";
				return;
			}

			if (hasFortified)
			{
				eventLabel.Text = "Already fortified";
				return;
			}

			if (from.Strength < 2)
			{
				eventLabel.Text = "Need 2+ troops";
				return;
			}

			int move = from.Strength - 1;

			to.Strength += move;
			from.Strength = 1;

			hasFortified = true;
			selectedCP = null;

			eventLabel.Text = $"Fortified +{move}";

			DrawAll();
			UpdateUI();
			return;
		}

		// =========================
		// REINFORCE
		// =========================
		if (CurrentPhase == TurnPhase.Reinforce)
		{
			if (cp.Owner != CurrentPlayer) return;

			if (pendingReinforcements > 0)
			{
				cp.Strength += 1;
				pendingReinforcements--;

				if (eventLabel != null)
					eventLabel.Text = "+1 Reinforced";

				DrawAll();
				UpdateUI();

				if (pendingReinforcements == 0)
				{
					CurrentPhase = TurnPhase.Attack;

					if (eventLabel != null)
						eventLabel.Text = "Attack phase";
				}

				return;
			}
		}

		// =========================
		// ATTACK
		// =========================

		if (selectedCP == null)
		{
			if (cp.Owner != CurrentPlayer) return;

			selectedCP = cell;
			DrawAll();
			return;
		}

		if (cell == selectedCP)
		{
			selectedCP = null;
			DrawAll();
			return;
		}

		if (!IsConnected(selectedCP.Value, cell))
		{
			if (eventLabel != null)
				eventLabel.Text = "Not connected";
			return;
		}

		var attacker = controlPoints[selectedCP.Value];

		if (attacker.Strength < 2)
		{
			if (eventLabel != null)
				eventLabel.Text = "Need at least 2 troops to attack";
			return;
		}

		if (cp.Owner == CurrentPlayer)
		{
			selectedCP = cell;
			DrawAll();
			return;
		}

		ResolveQuantumAttack(attacker, cp);

		selectedCP = null;

		DrawAll();
		UpdateUI();
	}

	bool HasPath(Vector2I start, Vector2I target, int player)
	{
		var visited = new HashSet<Vector2I>();
		var queue = new Queue<Vector2I>();

		queue.Enqueue(start);
		visited.Add(start);

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();

			if (current == target)
				return true;

			foreach (var neighbor in connections[current])
			{
				if (visited.Contains(neighbor))
					continue;

				// only travel through your own CPs
				if (controlPoints[neighbor].Owner != player)
					continue;

				visited.Add(neighbor);
				queue.Enqueue(neighbor);
			}
		}

		return false;
	}

	// =========================
	void ResolveQuantumAttack(ControlPointData attacker, ControlPointData defender)
	{
		int atk = attacker.Strength;
		int def = defender.Strength;

		float instability = 1f - (Stability / 20f);
		float noise = (float)GD.RandRange(-0.2f, 0.2f) * instability;
		float defenseBonus = 0.2f + (Stability / 100f); 

		int attackerLoss = Mathf.Max(1, (int)(def * 0.3f + noise * def));
		int defenderLoss = Mathf.Max(1, (int)((atk * 0.5f - noise * atk) / (1f + defenseBonus)));

		attacker.Strength -= attackerLoss;
		defender.Strength -= defenderLoss;

		if (eventLabel != null)
			eventLabel.Text = $"A-{attackerLoss} D-{defenderLoss}";

		if (defender.Strength <= 0)
		{
			defender.Owner = CurrentPlayer;

			// move all but 1 troop
			int move = Mathf.Max(1, attacker.Strength - 1);

			defender.Strength = move;
			attacker.Strength = 1;

			if (eventLabel != null)
				eventLabel.Text = $"Captured! Moved {move}";
		}

		attacker.Strength = Mathf.Max(1, attacker.Strength);

		CheckWin();

		Stability -= 1;
	}

	// =========================
	void DrawAll()
	{
		foreach (var cp in controlPoints.Values)
		{
			bool selected = selectedCP != null && selectedCP.Value == cp.Position;

			cp.Node.UpdateVisual(cp.Owner, cp.Strength, cp.Value, selected);
		}
	}

	void UpdateUI()
	{
		if (turnLabel != null)
			turnLabel.Text = $"Player {CurrentPlayer} | {CurrentPhase}";

		if (reinforceLabel != null)
			reinforceLabel.Text = $"Reinforcements: {pendingReinforcements}";

		if (statusLabel != null)
			statusLabel.Text = $"Stability: {Stability}";
	}


	public async void DoAITurn()
	{
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		DoAIReinforce();
		DrawAll(); UpdateUI();

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		DoAIAttack();
		DrawAll(); UpdateUI();

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		DoAIFortify();
		DrawAll(); UpdateUI();

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		// end turn back to player
		CurrentPlayer = 0;
		StartReinforcePhase();

		eventLabel.Text = "Your turn";
		DrawAll(); UpdateUI();
	}

	void DoAIReinforce()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (pendingReinforcements <= 0) break;

			if (cp.Owner != CurrentPlayer) continue;

			// reinforce frontline (has enemy neighbor)
			foreach (var n in connections[cp.Position])
			{
				if (controlPoints[n].Owner != CurrentPlayer)
				{
					cp.Strength++;
					pendingReinforcements--;
					break;
				}
			}
		}
	}

	void DoAIAttack()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (cp.Owner != CurrentPlayer) continue;
			if (cp.Strength < 2) continue;

			foreach (var n in connections[cp.Position])
			{
				var target = controlPoints[n];

				if (target.Owner == CurrentPlayer) continue;

				// simple rule: attack weaker targets
				if (cp.Strength > target.Strength)
				{
					ResolveQuantumAttack(cp, target);
				}
			}
		}
	}

	void DoAIFortify()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (cp.Owner != CurrentPlayer) continue;
			if (cp.Strength < 2) continue;

			foreach (var n in connections[cp.Position])
			{
				var target = controlPoints[n];

				// move troops toward enemy border
				if (target.Owner == CurrentPlayer)
				{
					foreach (var nn in connections[target.Position])
					{
						if (controlPoints[nn].Owner != CurrentPlayer)
						{
							int move = cp.Strength - 1;
							target.Strength += move;
							cp.Strength = 1;
							return; // only once
						}
					}
				}
			}
		}
	}
}
