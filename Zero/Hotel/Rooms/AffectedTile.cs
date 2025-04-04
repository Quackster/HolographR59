namespace Zero.Hotel.Rooms;

public class AffectedTile
{
	private int mX;

	private int mY;

	private int mI;

	public int X => mX;

	public int Y => mY;

	public int I => mI;

	public AffectedTile(int x, int y, int i)
	{
		mX = x;
		mY = y;
		mI = i;
	}
}
