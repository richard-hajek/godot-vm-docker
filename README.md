# Godot Virtual Machine based Docker Containers

Welcome!
This plugin allows one to add Docker container support and interaction to any Godot project!
It is a base for an upcoming game called *Hackfest*!

## Info
- Natively allows one to use *Dockerfiles* within the editor
- In-game objects can be made computer peripherals - making them accessible from within the container
- Uses a separate *virtual machine* to run *Docker containers*
(Meaning you cannot run other VMs without hacking the plugin, main focus is to support multiple Docker containers)
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
2. Add node `Computer`
3. Open the `Computer` node in inspector, set a Dockerfile, for example:
 ```
 FROM busybox
 CMD sh
 ```
4. Add control `Terminal`
5. When you want to open a terminal for a computer execute `terminalNode.Open(computerNode)`

## Specification

See Specifications.md detailed description of relevant classes.

## Troubleshooting

### Godot build fails with `The type or namespace name 'VagrantBridge' could not be found` (or similar)

This is caused by missing references in `.csproj` file.
One needs to reference `.cs` files, because Godot does not do that automatically.

Search your `.csproj` file for lines like so:

```
  <ItemGroup>
    <Compile Include="addons\godot-virtual-machines\BridgeContainer.cs"/>
    <Compile Include="Properties\AssemblyInfo.cs" />
  </ItemGroup>
```

And replace any `<Compile Include="addons\godot-virtual-machines\<anything>.cs">`
with just `<Compile Include="addons\**\*.cs"/>`

### Scene takes forever to load!
The computer nodes need to complete their boot processes.
If you wish you can call `BridgeWrapper.PreStart()` which will preboot the VM.

### The VM stays running even after the game was killed!
VM shutdown relies on Godot's _OnTreeExit(), so if the game is abruptly killed, the VM stays running.
You can manually kill it by killing VirtualBox's process in process explorer
Any damage done to the machine is irrelevant, it will get fixed on next boot.

### Terminal emulator behaves weirdly!
Terminal is highly WIP currently, as best described by `unsupported = true` on every literally two lines in `Terminal.cs`.
But please do report any bugs!
