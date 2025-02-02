namespace Otus_Task_5;

public class Vector: IEquatable<Vector>
{
    public int X { get; set; }
    public int Y { get; set; }

    public bool Equals(Vector other)
    {
        if (other == null)
            return false;
        return this.X == other.X && this.Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        if (obj is null)
            return false;
        if (obj.GetType() != this.GetType())
            return false;
        return Equals(obj as Vector);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();
            return hash;
        }
    }

    public override string ToString() => $"Vector({X}, {Y})";
}