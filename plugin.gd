tool
extends EditorPlugin

func _enter_tree():
	add_custom_type("Docker Container", "Spatial", preload("ContainerNode.cs"), null)
	add_custom_type("Terminal", "Control", preload("TerminalControl.cs"), null)

func _exit_tree():
	remove_custom_type("Docker Container")
	remove_custom_type("Terminal")
