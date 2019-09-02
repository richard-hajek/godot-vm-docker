# Specification of Godot - Docker compatibility layer plugin

## Abstract
Godot is a 3D capable game engine with advanced features.
I have decided to enrich the engine by adding Docker support plugin.
This plugin should support:
creating, starting, stopping, destroying containers,
connecting, disconnecting containers by network,
containers to snoop on communication of other containers,
peripherals

## Core

Core of this project shall be Godot-agnostic.
It should run on any C# capable machine.
Provided it meets the implementation's dependencies.

The Core shall provide two components: Docker Bridge and Terminal

### Docker Bridge API

Bridge shall implement an error enum, of which values will all methods be returning.

Any implementation of Docker controller should have the following methods:

```void Begin()```
To prepare everything.

```void Stop()```
To stop and tidy everything.

```int CreateContainer(string container_id, string dockerfile, List<string> peripheral_ids)```

```int StartContainer(string container_id)```

```int StopContainer(string container_id)```

```int DeleteContainer(string container_id)```

```int CreateTTY(string container_id, out StreamWriter stdin, out StreamWriter stdout, bool forceSTTY=false)```

```int Connect(string container_a_id, string container_b_id)```

```int Disconnect(string container_a_id, string container_b_id)```

```int SnoopOn(string container_a_id, string container_b_id, string container_snooping_id)```

```int StopSnoopOn(string container_a_id, string container_b_id, string container_snooping_id)```

```int GetPeripheralStreamReader(string container_id, string peripheral_id, out StreamReader reader)```

```int GetPeripheralStreamWriter(string container_id, string peripheral_id, out StreamReader reader)```

### Terminal

Terminal should support enough ANSI escape sequences to make vim work.

## Godot oriented classes

### Bridge Node

Bridge node shall contain a reference to Docker Bridge,
it will take care of starting and shutting down the Docker Bridge.

### Container Node

Container node shall check if a Bridge Node exists, if not, 
it will create one and attach it to the tree.

Container node shall export the following variables:
ID, Dockerfile, Peripherals

Container nodes shall always have a custom ID,
if user does not provide an ID, it will generate an unique ID.

Container node shall pass the Dockerfile to the bridge.

Container node shall create Peripheral IDs for all attached Peripherals,
pass them onto the bridge and take create streams for those Peripherals.

Consult Peripheral definition to how will container node behave towards
Peripherals.

Container nodes shall provide a `void HotCode(string command)`
which shall execute a command on the target container, without live TTY attached.

### Peripherals

Peripherals are not created by the plugin maintainer,
they are created by plugin user and hooked into the container nodes by
the user.

Peripheral can be any node that has the following methods:

`void peripheral_initialization(string peripheral_id, FuncRef sending_function)`

`void peripheral_receive(string data)`

Peripherals may use the `void sending_function(string id, string message)`
to send data to the container. In which case, peripherals MUST identify
themselves by their ID, assigned on `peripheral_initialization`.
 
Container node shall call `peripheral_receive` on any data it receives from the container.

### Terminal Control

Shall provide a `public void Open(ContainerNode container)` for opening
this terminal attached to this container.

Shall route ALL input to the attached container.
(Including ESC, CTRL and ALT modifiers)

Shall be closed by either GUI control of by SHIFT+ESC.