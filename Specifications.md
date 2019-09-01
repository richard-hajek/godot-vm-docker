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

### BridgeWrapper

Should provide an instance of DockerBridge.
Should start and shutdown DockerBridge.

### ComputerContainer

