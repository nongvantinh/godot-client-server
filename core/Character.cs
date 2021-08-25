using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Character : KinematicBody
{
    [Puppet] private Vector3 sync_position = Vector3.Zero;
    [Puppet] private Quat sync_orientation = Quat.Identity;
    
    private InputData input = InputData.Identity;

    [Export] public float Speed { get; set; } = 7.0f;
    [Export] public float MaxSpeed { get; set; } = 8.5f;
    [Export] public float Gravity { get; set; } = 21.0f;
    [Export] public float JumpPower { get; set; } = 9.0f;
    public Vector3 Velocity;

    private readonly Queue<CharacterInterpolatedData> interpolateStates;

    private CharacterInterpolatedData fromState, toState;
    private float currentHeight;
    private float snapshotWaitTime;
    private readonly Stopwatch watch;    // Time measure

    public Character()
    {
        Velocity = Vector3.Zero;
        interpolateStates = new Queue<CharacterInterpolatedData>();
        fromState = toState = CharacterInterpolatedData.Identity;
        currentHeight = 0.0f;
        snapshotWaitTime = 0.0f;
        watch = new Stopwatch();
    }

    public override void _Ready()
    {
        if (IsNetworkMaster())
        {
            GetNode<CameraFollow>("Pivot/Camera").Current = true;
            watch.Start();
        }
    }

    public override void _Process(float delta)
    {
        if (IsNetworkMaster())
        {
            input = GetInput();
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (IsNetworkMaster())
        {
            MoveAndRotate(input, delta);
            RsetUnreliable(nameof(sync_position), GlobalTransform.origin);
            RsetUnreliable(nameof(sync_orientation), GlobalTransform.basis.Quat().Normalized());
        }
        else
        {
            GlobalTransform = new Transform(sync_orientation, sync_position);
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

