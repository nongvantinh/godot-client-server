using Godot;
// [Tool]
public class CameraFollow : Camera
{
    [Export] private Vector3 _offset = new Vector3(0.0f, 12.0f, 0.0f);
    [Export] private Vector3 _rotationDegree = new Vector3(-60.0f, 0.0f, 0.0f);
    [Export] private readonly float _smoothSpeed = 0.125f;
    private Spatial _pivot;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Don't inherit transform of its parent.
        SetAsToplevel(true);

        _pivot = GetParent<Spatial>();
    }

    public override void _PhysicsProcess(float delta)
    {
        RotationDegrees = _rotationDegree;
        Transform transform = GlobalTransform;
        transform.origin = transform.origin.LinearInterpolate((_pivot.GlobalTransform.origin + _offset), _smoothSpeed);
        GlobalTransform = transform;
    }
}
