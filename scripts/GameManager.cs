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
		public Dictionary<int, int> Units = new();
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

	void ModifyStability(int amount)
	{
		Stability += amount;
		Stability = Mathf.Clamp(Stability, 0, 20);
	}

	bool IsContested(ControlPointData cp)
	{
		return cp.Units.Count > 1;
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
			ModifyStability(+1);

			CurrentPlayer = (CurrentPlayer + 1) % 2;
			StartReinforcePhase();

			eventLabel.Text = $"Player {CurrentPlayer} turn";

			// 👉 if AI player
			if (CurrentPlayer == 1)
			{
				DoAITurn(); // 👈 THIS is what makes AI play
			}

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
				Strength = strength,
				Value = GD.RandRange(1, 3),
				Units = new Dictionary<int, int>()
			};

			if (owner != -1)
			{
				controlPoints[cell].Units[owner] = strength;
			}
			else
			{
				// ✅ neutral acts like player 2 (or any unused ID)
				controlPoints[cell].Units[-1] = strength;
			}
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

			if (GetPlayerStrength(from, CurrentPlayer) < 2)
			{
				eventLabel.Text = "Need 2+ troops";
				return;
			}

			int player = CurrentPlayer;

			int available = GetPlayerStrength(from, player);
			int move = Mathf.Max(1, available - 1);

			// subtract from source
			from.Units[player] -= move;

			// add to destination
			if (!to.Units.ContainsKey(player))
				to.Units[player] = 0;

			to.Units[player] += move;

			// 🔥 sync visuals
			from.Strength = GetTotalStrength(from);
			to.Strength = GetTotalStrength(to);

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
				cp.Units[CurrentPlayer] = GetPlayerStrength(cp, CurrentPlayer) + 1;
				cp.Strength = GetTotalStrength(cp);
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

		if (GetPlayerStrength(attacker, CurrentPlayer) < 2)
		{
			if (eventLabel != null)
				eventLabel.Text = "Need at least 2 troops to attack";
			return;
		}

		bool isMine = GetPlayerStrength(cp, CurrentPlayer) > 0;
		bool contested = IsContested(cp);

		if (isMine && !contested)
		{
			selectedCP = cell;
			DrawAll();
			return;
		}

		// ✅ otherwise → attack enemy OR contested
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
		int atk = GetPlayerStrength(attacker, CurrentPlayer);
		int def = 0;

		foreach (var kv in defender.Units)
		{
			if (kv.Key != CurrentPlayer)
				def += kv.Value;
		}

		float instability = 1f - (Stability / 20f);
		float noise = (float)GD.RandRange(-0.2f, 0.2f) * instability;
		float defenseBonus = 0.2f + (Stability / 100f);

		int attackerLoss = Mathf.Max(1, (int)(def * 0.3f + noise * def));
		int defenderLoss = Mathf.Max(1, (int)((atk * 0.5f - noise * atk) / (1f + defenseBonus)));

		// ✅ Apply losses to Units (NOT Strength)
		int newAtk = Mathf.Max(1, atk - attackerLoss);
		int newDef = Mathf.Max(0, def - defenderLoss);

		attacker.Units[CurrentPlayer] = newAtk;
		defender.Units[1 - CurrentPlayer] = newDef;

		// ✅ Ensure both players exist in dictionary
		if (!defender.Units.ContainsKey(CurrentPlayer))
			defender.Units[CurrentPlayer] = 0;

		// move attacking units (leave 1 behind)
		int move = Mathf.Max(1, newAtk - 1);
		defender.Units[CurrentPlayer] += move;
		attacker.Units[CurrentPlayer] = 1;

		// mark contested
		defender.Owner = -1;

		// ✅ Sync visual strength
		attacker.Strength = GetTotalStrength(attacker);
		defender.Strength = GetTotalStrength(defender);

		if (eventLabel != null)
			eventLabel.Text = $"Contested! A-{attackerLoss} D-{defenderLoss}";

		ModifyStability(-2);
		CheckWin();
	}


	void ResolveContested()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (cp.Units.Count < 2) continue;

			int p0 = cp.Units.ContainsKey(0) ? cp.Units[0] : 0;
			int p1 = cp.Units.ContainsKey(1) ? cp.Units[1] : 0;

			if (Mathf.Abs(p0 - p1) >= 2)
			{
				int winner = (p0 > p1) ? 0 : 1;
				int winningStrength = Mathf.Max(p0, p1);

				cp.Owner = winner;

				// ✅ rebuild Units properly
				cp.Units.Clear();
				cp.Units[winner] = winningStrength;

				// ✅ sync visual
				cp.Strength = winningStrength;
				eventLabel.Text = $"Resolved → Player {winner}";
			}
			else
			{
				// stays contested
				cp.Owner = -1;
			}
		}
	}


	bool ShouldAIStabilise()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (!IsContested(cp)) continue;

			int my = GetPlayerStrength(cp, CurrentPlayer);
			int enemy = GetPlayerStrength(cp, 1 - CurrentPlayer);

			if (my >= enemy + 2)
				return true; // guaranteed win → stabilise
		}

		// otherwise: 40% chance to stabilise, 60% attack
		return GD.Randf() < 0.4f;
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
		ResolveContested();

		if (ShouldAIStabilise())
		{
			DoAIStabilise();
		}
		else
		{
			DoAIAttack();
		}

		DrawAll(); UpdateUI();

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		DoAIReinforce();
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


	int GetPlayerStrength(ControlPointData cp, int player)
	{
		return cp.Units.GetValueOrDefault(player, 0);
	}

	int GetTotalStrength(ControlPointData cp)
	{
		int total = 0;
		foreach (var v in cp.Units.Values)
			total += v;
		return total;
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
					cp.Units[CurrentPlayer]++;
					cp.Strength = GetTotalStrength(cp);
					pendingReinforcements--;
					break;
				}
			}
		}
	}


	void DoAIStabilise()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (!IsContested(cp)) continue;

			int myUnits = GetPlayerStrength(cp, CurrentPlayer);
			int enemyUnits = GetPlayerStrength(cp, 1 - CurrentPlayer);

			// can win resolution
			if (myUnits >= enemyUnits + 2)
			{
				cp.Owner = CurrentPlayer;
				cp.Strength = myUnits;

				cp.Units.Clear();

				eventLabel.Text = $"AI stabilised CP";

				ModifyStability(+1);
				return; // only do one per turn
			}
		}
	}


	void DoAIAttack()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (cp.Owner != CurrentPlayer) continue;

			int myPower = GetPlayerStrength(cp, CurrentPlayer);
			if (myPower < 2) continue;

			foreach (var n in connections[cp.Position])
			{
				var target = controlPoints[n];

				if (target.Owner == CurrentPlayer) continue;

				int enemyPower = GetPlayerStrength(target, 1 - CurrentPlayer);

				// ✅ allow slight risk-taking
				if (myPower >= enemyPower - 1)
				{
					ResolveQuantumAttack(cp, target);
					return; // one attack per turn
				}
			}
		}
	}

	void DoAIFortify()
	{
		foreach (var cp in controlPoints.Values)
		{
			if (cp.Owner != CurrentPlayer) continue;

			int strength = GetPlayerStrength(cp, CurrentPlayer);
			if (strength < 2) continue;

			foreach (var n in connections[cp.Position])
			{
				var target = controlPoints[n];

				if (target.Owner == CurrentPlayer)
				{
					foreach (var nn in connections[target.Position])
					{
						if (controlPoints[nn].Owner != CurrentPlayer)
						{
							int move = strength - 1;

							target.Units[CurrentPlayer] =
								GetPlayerStrength(target, CurrentPlayer) + move;

							cp.Units[CurrentPlayer] = 1;

							cp.Strength = GetTotalStrength(cp);
							target.Strength = GetTotalStrength(target);
							return;
						}
					}
				}
			}
		}
	}
}
