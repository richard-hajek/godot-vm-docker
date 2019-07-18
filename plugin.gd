tool
extends EditorPlugin

func _enter_tree():
    add_custom_type("Peripheral", "Spatial", preload("PeripheralContainer.cs"), null)
    add_custom_type("Computer", "Spatial", preload("ComputerContainer.cs"), null)
    add_custom_type("VM Bridge Manager", "Node", preload("BridgeContainer.cs"), null)
    add_custom_type("Terminal", "Control", preload("TerminalContainer.cs"), null)

func _exit_tree():
    remove_custom_type("Peripheral")
    remove_custom_type("Computer")
    remove_custom_type("Hackfest")
    remove_custom_type("Terminal")
