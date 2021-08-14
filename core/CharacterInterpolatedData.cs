using Godot;

public struct CharacterInterpolatedData
{
    public Vector3 Position;
    public Quat Orientation;

    private static readonly CharacterInterpolatedData _identity = new CharacterInterpolatedData(0);
    public static CharacterInterpolatedData Identity => _identity;

    public CharacterInterpolatedData(int i)
    {
        Position = Vector3.Zero;
        Orientation = Quat.Identity;
    }

    public override string ToString()
    {
        System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
        strBuilder.AppendLine("Position: " + Position.ToString());
        strBuilder.AppendLine("Orientation: " + Orientation.ToString());
        return strBuilder.ToString();
    }

    public static bool operator ==(CharacterInterpolatedData first, CharacterInterpolatedData second)
    {
        return first.Position == second.Position
            && first.Orientation == second.Orientation;
    }

    public static bool operator !=(CharacterInterpolatedData first, CharacterInterpolatedData second)
    {
        return !(first == second);
    }

    public override bool Equals(object obj)
    {
        return obj is CharacterInterpolatedData data && (data == this);
    }

    public override int GetHashCode()
    {
        int hashCode = -2096153443;
        hashCode = hashCode * -1521134295 + Position.GetHashCode();
        hashCode = hashCode * -1521134295 + Orientation.GetHashCode();
        return hashCode;
    }

    public static CharacterInterpolatedData Interpolate(CharacterInterpolatedData prev, CharacterInterpolatedData next, float weight)
    {
        CharacterInterpolatedData data = CharacterInterpolatedData.Identity;

        data.Position = prev.Position.LinearInterpolate(next.Position, weight);
        data.Orientation = prev.Orientation.Slerp(next.Orientation, weight);
        return data;
    }
}