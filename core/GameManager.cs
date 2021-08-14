using Godot;
using System;
using System.Collections.Generic;

public class GameManager : Node
{
    [Export] public bool ServerBuild = false;
    [Export] public string ServerAddress = "127.0.0.1";
    [Export] public int ServerPort = 24245, MaxClients = 100 * 12;

    [Export] public PackedScene CharacterScene = null;

    private Dictionary<int, Node> entities = new Dictionary<int, Node>();


    public static int ControllerId = -1;
    public const float ServerPacketSendRate = 1.0f / 60.0f;
    public static int TargetFps = (int)ProjectSettings.GetSetting("physics/common/physics_fps");
    public static float DeltaTime = 1.0f / TargetFps;
    public static float InterpolationWaitTime = (ServerPacketSendRate * 3.0f) + (DeltaTime * 2.0f);


    public override void _Ready()
    {
        GetTree().Connect("network_peer_connected", this, nameof(OnPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
        GetTree().Connect("connected_to_server", this, nameof(OnConnectedToServer));
        GetTree().Connect("connection_failed", this, nameof(OnConnectionFailed));
        GetTree().Connect("server_disconnected", this, nameof(OnServerDisconnected));

        InitNetwork();
    }

    private void InitNetwork()
    {
        if (ServerBuild)
        {
            var networkPeer = new NetworkedMultiplayerENet();
            networkPeer.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;
            Error err = networkPeer.CreateServer(ServerPort, MaxClients);
            if (err != Error.Ok)
            {
                GD.Print(err);
                return;
            }

            GetTree().NetworkPeer = networkPeer;
            // Server always has id = 1.
            GD.Print($"Server is running on: {ServerAddress}:{ServerPort} with {MaxClients} max client.");
        }
        else
        {
            var networkPeer = new NetworkedMultiplayerENet();
            networkPeer.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;
            Error err = networkPeer.CreateClient(ServerAddress, ServerPort);
            if (err != Error.Ok)
            {
                GD.Print(err);
                return;
            }

            GetTree().NetworkPeer = networkPeer;
            GD.Print($"Client is running.");
        }
    }

    private void OnPeerConnected(int peerId)
    {
        // Ignore server connected signal.
        if (peerId == 1)
            return;

        GD.Print($"{peerId} has connected.");
        if (GetTree().IsNetworkServer())
        {
            // Send all client in scene to new connected client.
            foreach (var other in entities.Keys)
                RpcId(peerId, nameof(SyncWithServer), other);

            // Send new client to all.
            Rpc(nameof(OnClientConnected), peerId);

            RpcId(peerId, nameof(ConnectController), peerId);
        }
    }

    [Remote]
    public void ConnectController(int peerId)
    {
        GD.Print("Received ConnectController:" + peerId);
        GameManager.ControllerId = peerId;
    }

    [RemoteSync]
    private void OnClientConnected(int peerId)
    {
        var character = CharacterScene.Instance<Character>();
        character.Name = peerId.ToString();
        character.PeerId = peerId;
        entities.Add(peerId, character);
        AddChild(character);
    }

    [Remote]
    private void SyncWithServer(int peerId)
    {
        var character = CharacterScene.Instance<Character>();
        character.Name = peerId.ToString();
        character.PeerId = peerId;
        entities.Add(peerId, character);
        AddChild(character);
    }

    private void OnPeerDisconnected(int peerId)
    {
        GD.Print($"{peerId} has disconnected.");
        if (entities.TryGetValue(peerId, out Node entity))
        {
            entity.QueueFree();
            entities.Remove(peerId);
        }
    }

    private void OnConnectedToServer()
    {
        GD.Print($"successfully connected to a server.");
    }

    private void OnConnectionFailed()
    {
        GD.Print($"fails to establish a connection to a server.");
    }

    private void OnServerDisconnected()
    {
        GD.Print($"Server has disconnected me.");

        foreach (var entity in entities.Values)
            entity.QueueFree();

        entities.Clear();
    }
}
