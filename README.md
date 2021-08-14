# godot-client-server
Godot version: `3.3.mono.stable`

This is an attempt to create client/server model using Enet offer by Godot.

The model includes Server has full authority over the network, client-side prediction, client-side interpolation, server reconciliation.

Source code is fully written in C#.

The result is not as good as I expect it to be. It seems that Godot sends a packet whenever the Rpc method is called, which is not what I want.
C# marshalling is very bad, I can't send custom data type using Rpc method.

Game start lagging when 2 players join the game.

I'll try another networking library to see if there is a better solution.
