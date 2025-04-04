namespace Zero.Hotel.Pathfinding;

public struct Coord
{
	internal int x;

	internal int y;

	internal Coord(int _x, int _y)
	{
		x = _x;
		y = _y;
	}

	public static bool operator ==(Coord a, Coord b)
	{
		if (object.ReferenceEquals(a, b))
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return a.x == b.x && a.y == b.y;
	}

	public static bool operator !=(Coord a, Coord b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return x ^ y;
	}

	public override bool Equals(object obj)
	{
		return base.GetHashCode().Equals(obj.GetHashCode());
	}
}
