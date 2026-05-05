using Godot;

public partial class ControlPoint : Area2D
{
	[Signal]
	public delegate void ClickedEventHandler(Vector2I cell);

	[Export]
	public Vector2I Cell;

	public override void _Ready()
	{
		InputEvent += OnInput;
	}

	private void OnInput(Node viewport, InputEvent e, long shapeIdx)
	{
		if (e is InputEventMouseButton m && m.Pressed)
		{
			GD.Print("AREA CLICK WORKS");
			EmitSignal(SignalName.Clicked, Cell);
		}
	}

	public void UpdateVisual(int owner, int strength, int value, bool selected)
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");
		var highlight = GetNodeOrNull<Sprite2D>("Highlight");
		var str = GetNode<Label>("Strength");
		var val = GetNode<Label>("Value");

		sprite.Modulate = owner switch
		{
			0 => Colors.Red,
			1 => Colors.Blue,
			_ => Colors.Gray
		};

		// ✅ restore working text
		str.Text = strength.ToString();
		val.Text = value.ToString();

		if (highlight != null)
			highlight.Visible = selected;
	}
}
