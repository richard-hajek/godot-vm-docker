# Godot Virtual Machines and Docker Containers

Welcome!
This plugin allows one to add VM and Docker support and interaction to any Godot project!
It is a base for an upcoming game called Hackfest!

## Info
- Natively allows one to use *Dockerfiles* within the editor
- In-game objects can be made computer peripherals - making them accessible from within the docker container
- Uses a separate virtual machine to run docker daemon
which in turn runs the docker images
- Does *NOT* require privileges to run (achieving that was rough)

## Dependencies

- Godot
- Vagrant (Used to spin the Virtual Machines)

## Installation

### Cloning

- Clone this repository into `addons/godot-virtual-machines`

### Git Submodule

If your project is version controlled by Git, 
you can use add this repo as submodule.

- Execute `git submodule add git@github.com:meowxiik/godot-virtual-machines.git addons/godot-virtual-machines` in the project root


## Usage

1. Enable plugin in Project -> Project Settings -> Plugins
2. Add node `VM Manager` under the root of your project. Do *NOT* change it's name.
3. Add any PCs into the scene
4. Add mesh to the PCs, this mesh will be clickable, and should represent a terminal, screen etc...
5. Test it!

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

