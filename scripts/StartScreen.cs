using Godot;
using System;

public partial class StartScreen : Control
{
	public override void _Ready()
	{
		GetNode<Button>("VBoxContainer/PlayButton").Pressed += OnPlay;
		GetNode<Button>("VBoxContainer/QuitButton").Pressed += OnQuit;
	}

	void OnPlay()
	{
		GetTree().ChangeSceneToFile("res://Game.tscn"); // your main game
	}

	void OnQuit()
	{
		GetTree().Quit();
	}
}
