using Godot;

public struct InputData
{
    public double Time;
    public Vector2 Direction;
    public bool Jump;

    private static readonly InputData _identity = new InputData(0);
    public static InputData Identity => _identity;

    public InputData(int i)
    {
        Time = 0.0;
        Direction = Vector2.Zero;
        Jump = false;
    }

    public static bool operator ==(InputData first, InputData second)
    {
        return first.Time == second.Time
        && first.Direction == second.Direction
        && first.Jump == second.Jump;
    }

    public static bool operator !=(InputData first, InputData second)
    {
        return !(first == second);
    }

    public override bool Equals(object obj)
    {
        return obj is InputData data && (data == this);
    }

    public override int GetHashCode()
    {
        int hashCode = -1963547961;
        hashCode = hashCode * -1521134295 + Time.GetHashCode();
        hashCode = hashCode * -1521134295 + Direction.GetHashCode();
        hashCode = hashCode * -1521134295 + Jump.GetHashCode();
        return hashCode;
    }
}

public struct CharacterPredictedData
{
    public InputData Input;
    public Vector3 Position;
    public Quat Orientation;
    public Vector3 VelocityRemain;

    private static readonly CharacterPredictedData _identity = new CharacterPredictedData(0);
    public static CharacterPredictedData Identity => _identity;

    public CharacterPredictedData(int i)
    {
        Input = InputData.Identity;
        Position = Vector3.Zero;
        Orientation = Quat.Identity;
        VelocityRemain = Vector3.Zero;
    }

    public override string ToString()
    {
        System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
        strBuilder.AppendLine("Input: " + Input.ToString());
        strBuilder.AppendLine("Position: " + Position.ToString());
        strBuilder.AppendLine("Orientation: " + Orientation.ToString());
        strBuilder.AppendLine("VelocityRemain: " + VelocityRemain.ToString());
        return strBuilder.ToString();
    }

    public static bool operator ==(CharacterPredictedData first, CharacterPredictedData second)
    {
        return first.Input == second.Input
            && first.Position == second.Position
            && first.Orientation == second.Orientation
            && first.VelocityRemain == second.VelocityRemain;
    }

    public static bool operator !=(CharacterPredictedData first, CharacterPredictedData second)
    {
        return !(first == second);
    }

    public override bool Equals(object obj)
    {
        return obj is CharacterPredictedData data && (data == this);
    }

    public override int GetHashCode()
    {
        int hashCode = 2058627125;
        hashCode = hashCode * -1521134295 + Input.GetHashCode();
        hashCode = hashCode * -1521134295 + Position.GetHashCode();
        hashCode = hashCode * -1521134295 + Orientation.GetHashCode();
        hashCode = hashCode * -1521134295 + VelocityRemain.GetHashCode();
        return hashCode;
    }
}
