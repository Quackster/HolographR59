namespace Zero.Hotel.Pathfinding;

internal class ShitPathfinder
{
    public static Coord GetNextStep(int X, int Y, int goalX, int goalY)
    {
        Coord Next = new Coord(-1, -1);
        if (X > goalX && Y > goalY)
        {
            Next = new Coord(X - 1, Y - 1);
        }
        else if (X < goalX && Y < goalY)
        {
            Next = new Coord(X + 1, Y + 1);
        }
        else if (X > goalX && Y < goalY)
        {
            Next = new Coord(X - 1, Y + 1);
        }
        else if (X < goalX && Y > goalY)
        {
            Next = new Coord(X + 1, Y - 1);
        }
        else if (X > goalX)
        {
            Next = new Coord(X - 1, Y);
        }
        else if (X < goalX)
        {
            Next = new Coord(X + 1, Y);
        }
        else if (Y < goalY)
        {
            Next = new Coord(X, Y + 1);
        }
        else if (Y > goalY)
        {
            Next = new Coord(X, Y - 1);
        }
        return Next;
    }
}
