using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Character : KinematicBody
{
    public int PeerId = -1;
    [Export] public float Speed { get; set; } = 7.0f;
    [Export] public float MaxSpeed { get; set; } = 8.5f;
    [Export] public float Gravity { get; set; } = 21.0f;
    [Export] public float JumpPower { get; set; } = 9.0f;
    public Vector3 Velocity;

    private readonly Queue<InputData> inputs;
    private readonly LinkedList<CharacterPredictedData> historyMovements;
    private readonly Queue<CharacterInterpolatedData> interpolateStates;

    private CharacterInterpolatedData fromState, toState;
    private CharacterPredictedData ackState;
    private float currentHeight;
    private bool mustReconcile;
    private float snapshotWaitTime;
    private readonly Stopwatch watch;    // Time measure


    public Character()
    {
        Velocity = Vector3.Zero;
        inputs = new Queue<InputData>();
        historyMovements = new LinkedList<CharacterPredictedData>();
        interpolateStates = new Queue<CharacterInterpolatedData>();
        fromState = toState = CharacterInterpolatedData.Identity;
        ackState = CharacterPredictedData.Identity;
        currentHeight = 0.0f;
        mustReconcile = false;
        snapshotWaitTime = 0.0f;
        watch = new Stopwatch();
    }

    public override void _Ready()
    {
        if (GameManager.ControllerId == PeerId)
        {
            GetNode<CameraFollow>("Pivot/Camera").Current = true;
            watch.Start();
        }
    }

    public override void _Process(float delta)
    {
        if (GameManager.ControllerId == PeerId)
        {
            InputData input = GetInput();
            RpcId(1, nameof(QueueInput), input.Time, input.Direction, input.Jump);
            QueueInput(input.Time, input.Direction, input.Jump);
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (GetTree().IsNetworkServer())
        {
            ProcessInputs(delta);
            // Server only need 20 history movement to reconstruct the world.
            while (20 <= historyMovements.Count)
                historyMovements.RemoveFirst();
        }
        else if (GameManager.ControllerId == PeerId)
        {
            Reconcile(delta);
            ProcessInputs(delta);
        }
        else
        {
            Interpolate(delta);
        }
    }

    private void Interpolate(float delta)
    {
        if (0 == interpolateStates.Count)
        {
            snapshotWaitTime = GameManager.InterpolationWaitTime;
            return;
        }

        currentHeight += delta;
        float t = Mathf.Clamp(currentHeight / GameManager.ServerPacketSendRate, 0.0f, 1.0f);
        CharacterInterpolatedData currentState = CharacterInterpolatedData.Interpolate(fromState, toState, t);
        GlobalTransform = new Transform(currentState.Orientation, currentState.Position);

        if (1.0f <= t && interpolateStates.Count != 0)
        {
            fromState = toState;
            toState = interpolateStates.Dequeue();
            currentHeight = 0.0f;
        }
    }

    // Client reconcile state received from server.
    private void Reconcile(float delta)
    {
        if (!mustReconcile || 0 == historyMovements.Count)
            return;
        mustReconcile = false;

        while (historyMovements.Count != 0)
        {
            var state = historyMovements.First.Value;
            if (ackState.Input.Time < state.Input.Time)
                break;
            historyMovements.RemoveFirst();
        }

        // Rewinds.
        Velocity = ackState.VelocityRemain;
        GlobalTransform = new Transform(ackState.Orientation, ackState.Position);

        // Replay.
        var node = historyMovements.First;
        for (int i = 0; i < historyMovements.Count; ++i)
        {
            var state = node.Value;
            node.Value = MoveAndRotate(state.Input, delta);
            node = node.Next;
        }
    }

    private void ProcessInputs(float delta)
    {
        while (inputs.Count != 0)
        {
            InputData input = inputs.Dequeue();
            CharacterPredictedData state = MoveAndRotate(input, delta);
            historyMovements.AddLast(state);
        }
    }

    private InputData GetInput()
    {
        InputData input = InputData.Identity;

        if (Input.IsActionPressed("move_forward"))
        {
            input.Direction.y -= 1;
        }
        if (Input.IsActionPressed("move_backward"))
        {
            input.Direction.y += 1;
        }
        if (Input.IsActionPressed("move_left"))
        {
            input.Direction.x -= 1;
        }
        if (Input.IsActionPressed("move_right"))
        {
            input.Direction.x += 1;
        }

        input.Time = watch.Elapsed.TotalSeconds;
        input.Jump = Input.IsActionPressed("jump");
        input.Direction = input.Direction.Normalized();
        return input;
    }

    [RemoteSync]
    private void QueueInput(double time, Vector2 direction, bool jump)
    {
        InputData input = InputData.Identity;
        input.Time = time;
        input.Direction = direction;
        input.Jump = jump;
        inputs.Enqueue(input);
    }

    [Remote]
    private void QueueInterpolateState(CharacterInterpolatedData state)
    {
        interpolateStates.Enqueue(state);
    }

    public void Rotate(Vector2 Direction)
    {
        if (Direction != Vector2.Zero)
        {
            // Rotate.
            Vector3 rot = Rotation;
            rot.y = -Mathf.Atan2(Direction.x, -Direction.y);
            Rotation = rot;
        }
    }

    public CharacterPredictedData Move(InputData input, float delta)
    {
        // Calculate velocity.
        float speed = Speed;
        Velocity.x = input.Direction.x * speed;
        Velocity.z = input.Direction.y * speed;
        Velocity.y = Velocity.y - Gravity * delta;

        Velocity = MoveAndSlide(Velocity, Vector3.Up);

        CharacterPredictedData state = CharacterPredictedData.Identity;
        state.Input = input;
        state.Position = GlobalTransform.origin;
        state.Orientation = GlobalTransform.basis.Quat().Normalized();
        state.VelocityRemain = Velocity;

        return state;
    }

    public CharacterPredictedData MoveAndRotate(InputData input, float delta)
    {
        // Calculate velocity.
        float speed = Speed;
        Velocity.x = input.Direction.x * speed;
        Velocity.z = input.Direction.y * speed;
        Velocity.y = Velocity.y - Gravity * delta;

        Rotate(input.Direction);
        Velocity = MoveAndSlide(Velocity, Vector3.Up);

        CharacterPredictedData state = CharacterPredictedData.Identity;
        state.Input = input;
        state.Position = GlobalTransform.origin;
        state.Orientation = GlobalTransform.basis.Quat().Normalized();
        state.VelocityRemain = Velocity;

        return state;
    }

}

