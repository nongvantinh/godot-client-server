# godot-client-server
Godot version: `3.3.mono.stable`

This is an attempt to create client/server model using Enet offer by Godot.

Source code is fully written in C#.
## Branch 0.1
The model includes Server has full authority over the network, client-side prediction, client-side interpolation, server reconciliation.

The result is not as good as I expect it to be. It seems that Godot sends a packet whenever the Rpc method is called, which is not what I want.
C# marshalling is very bad, I can't send custom data type using Rpc method.
## Branch 1.0
- Change the model again. use MMO architecture that we "trust" client honesty.
- C# List<> is not allowed in Rpc call see [API diference](https://docs.godotengine.org/en/stable/getting_started/scripting/c_sharp/c_sharp_differences.html) for more information of which type is allowed to send over network.


Game start lagging when 2 players join the game.

I'll try another networking library to see if there is a better solution.
- [yojimbo.net](https://github.com/netcode-io/yojimbo.net.git) returns with memory leak, I opened [an issue](https://github.com/netcode-io/yojimbo.net/issues/3) and it seems that this library is dead, It received no support from the creator, the C++ version of this library doesn't support mobile devices.
