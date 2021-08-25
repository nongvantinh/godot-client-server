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

    public const float ServerPacketSendRate = 1.0f / 60.0f;
    public static int TargetFps = (int)ProjectSettings.GetSetting("physics/common/physics_fps");
    public static float DeltaTime = 1.0f / TargetFps;
    public static float InterpolationWaitTime = (ServerPacketSendRate * 3.0f) + (DeltaTime * 2.0f);

    public override void _Ready()
    {
        GetTree().Connect("connected_to_server", this, nameof(OnConnectedToServer));
        GetTree().Connect("connection_failed", this, nameof(OnConnectionFailed));
        GetTree().Connect("network_peer_connected", this, nameof(OnNetworkPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(OnNetworkPeerDisconnected));
        // GetTree().Connect("network_peer_packet", this, nameof(OnNetworkPeerPacket));
        GetTree().Connect("server_disconnected", this, nameof(OnServerDisconnected));

        InitNetwork();
    }

    private void InitNetwork()
    {
        var networkPeer = new NetworkedMultiplayerENet();
        networkPeer.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;
        Error err = ServerBuild ? networkPeer.CreateServer(ServerPort, MaxClients) : networkPeer.CreateClient(ServerAddress, ServerPort);
        if (err != Error.Ok)
        {
            GD.PushError(err.ToString());
            return;
        }
        GetTree().NetworkPeer = networkPeer;


        // Server always has id = 1.
        if (GetTree().IsNetworkServer())
            GD.Print($"Server is running on: {ServerAddress}:{ServerPort} with {MaxClients} max client.");
        else
            GD.Print($"Client is running.");
    }

    private void OnConnectedToServer()
    {
        GD.Print("OnConnectedToServer");
    }

    private void OnConnectionFailed()
    {
        GD.Print("OnConnectionFailed");
    }

    private void OnNetworkPeerConnected(int id)
    {
        GD.Print("OnNetworkPeerConnected");
        // Ignore server connected signal.
        if (id == 1)
            return;

        GD.Print($"{id} has connected.");
        if (GetTree().IsNetworkServer())
        {
            // Send all client in scene to new connected client.
            {
                int[] ids = new int[entities.Count];
                int index = 0;
                foreach (var item in entities.Keys)
                    ids[index++] = item;

                RpcId(id, nameof(SyncWithServer), ids);
            }
            Rpc(nameof(CreateCharacter), id);
        }
    }

    private void OnNetworkPeerDisconnected(int id)
    {
        GD.Print("OnNetworkPeerDisconnected");
        if (entities.TryGetValue(id, out Node node))
        {
            node.QueueFree();
            entities.Remove(id);
        }
    }

    private void OnNetworkPeerPacket(int id, byte[] packet)
    {
        GD.Print("OnNetworkPeerPacket");
    }

    private void OnServerDisconnected()
    {
        GD.Print("OnServerDisconnected");
    }

    [Remote]
    private void SyncWithServer(int[] ids)
    {
        for (int i = 0; ids.Length != i; ++i)
        {
            var character = CharacterScene.Instance<Character>();
            character.SetNetworkMaster(ids[i]);
            character.Name = ids[i].ToString();
            AddChild(character);

            entities.Add(ids[i], character);
        }
    }

    [RemoteSync]
    private void CreateCharacter(int id)
    {
        var character = CharacterScene.Instance<Character>();
        character.SetNetworkMaster(id);
        character.Name = id.ToString();
        AddChild(character);

        entities.Add(id, character);
    }
}
