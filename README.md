# Godot Virtual Machine based Docker Containers

Welcome!
This plugin allows one to add Docker container support and interaction to any Godot project!
It is a base for an upcoming game called *Hackfest*!

## Info
- Natively allows one to use *Dockerfiles* within the editor
- In-game objects can be made container peripherals - making them accessible from within the container
- Uses a separate *virtual machine* to run *Docker containers*
- Does *NOT* require privileges to run (achieving that was rough)

## Dependencies

- Godot
- Vagrant (Used to spin the Virtual Machines)

## Installation

### Cloning

- Clone using `git clone git@github.com:meowxiik/godot-vm-docker.git addons/godot-vm-docker` in your project root

### Git Submodule

If your project is version controlled by Git, 
you can use add this repo as submodule.

- Execute `git submodule add git@github.com:meowxiik/godot-vm-docker.git addons/godot-vm-docker` in the project root

## Usage

1. Enable plugin in Project -> Project Settings -> Plugins
2. Add node `Container`
3. Open the `Container` node in inspector, set a Dockerfile, for example:
 ```
 FROM busybox
 CMD sh
 ```
4. Add control `Terminal`
5. When you want to open a terminal for a container execute `terminalNode.Open(containerNode)`

## Specification

See [Specifications](./Specifications.md) detailed description of relevant classes.

## FAQ

### Why are you talking about VMs and Docker at the same time? They are different concepts

Yes. I use a VM to boot a machine that has Docker installed.
This way this plugin can pretty much do anything with said Docker and not damage anything.
Also this enables this plugin to run without admin privileges.

### Project build fails with `The type or namespace name 'VagrantBridge' could not be found` (or similar)

This is caused by missing references in `.csproj` file.
One needs to reference `.cs` files, because Godot does not do that automatically.

Search your `.csproj` file for lines like so:

```
  <ItemGroup>
    <Compile Include="addons\godot-vm-docker\BridgeContainer.cs"/>
    <Compile Include="Properties\AssemblyInfo.cs" />
  </ItemGroup>
```

And replace any `<Compile Include="addons\godot-vm-docker\<anything>.cs">`
with just `<Compile Include="addons\**\*.cs"/>`

### Scene takes forever to load!
The container nodes need to complete their boot processes.
If you wish you can call `BridgeWrapper.PreStart()` which will preboot the VM.

### The VM stays running even after the game was killed!
VM shutdown relies on Godot's _OnTreeExit(), so if the game is abruptly killed, the VM stays running.
You can manually kill it by killing VirtualBox's process in process explorer
Any damage done to the machine is irrelevant, it will get fixed on next boot.

### Terminal emulator behaves weirdly!
Terminal is highly WIP currently, as best described by `unsupported = true` on every literally two lines in `Terminal.cs`.
But please do report any bugs!
