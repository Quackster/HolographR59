using System.Collections.Generic;
using System.Drawing;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Pathfinding;

internal class Pathfinder
{
	private Point[] Movements;

	private CompleteSquare[,] Squares;

	private Room Room;

	private RoomModel Model;

	private RoomUser User;

	private int mapSizeX;

	private int mapSizeY;

	public Pathfinder(Room Room, RoomUser User)
	{
		this.Room = Room;
		Model = Room.Model;
		this.User = User;
		if (Room == null || Model == null || User == null)
		{
			return;
		}
		InitMovements(0);
		mapSizeX = Model.MapSizeX;
		mapSizeY = Model.MapSizeY;
		Squares = new CompleteSquare[mapSizeX, mapSizeY];
		for (int x = 0; x < mapSizeX; x++)
		{
			for (int y = 0; y < mapSizeY; y++)
			{
				Squares[x, y] = new CompleteSquare(x, y);
			}
		}
	}

	private IEnumerable<Point> GetSquares()
	{
		for (int x = 0; x < mapSizeX; x++)
		{
			for (int y = 0; y < mapSizeY; y++)
			{
				yield return new Point(x, y);
			}
		}
	}

	private IEnumerable<Point> ValidMoves(int x, int y)
	{
		try
		{
			Point[] movements = Movements;
			foreach (Point movePoint in movements)
			{
				Point point = movePoint;
				int newX = x + point.X;
				point = movePoint;
				int newY = y + point.Y;
				if (ValidCoordinates(newX, newY) && IsSquareOpen(newX, newY, CheckHeight: true))
				{
					yield return new Point(newX, newY);
				}
			}
		}
		finally
		{
		}
	}

	public List<Coord> FindPath()
	{
		int UserX = User.X;
		int UserY = User.Y;
		Squares[User.X, User.Y].DistanceSteps = 0;
		bool MadeProgress;
		do
		{
			// bool flag = true;
			MadeProgress = false;
			foreach (Point MainPoint in GetSquares())
			{
				int x = MainPoint.X;
				int y = MainPoint.Y;
				if (!IsSquareOpen(x, y, CheckHeight: true))
				{
					continue;
				}
				int passHere = Squares[x, y].DistanceSteps;
				foreach (Point movePoint in ValidMoves(x, y))
				{
					int newX = movePoint.X;
					int newY = movePoint.Y;
					int newPass = passHere + 1;
					if (Squares[newX, newY].DistanceSteps > newPass)
					{
						Squares[newX, newY].DistanceSteps = newPass;
						MadeProgress = true;
					}
				}
			}
		}
		while (MadeProgress);
		int goalX = User.GoalX;
		int goalY = User.GoalY;
		if (goalX == -1 || goalY == -1)
		{
			return null;
		}
		List<Coord> Path = new List<Coord>();
		Path.Add(new Coord(User.GoalX, User.GoalY));
		do
		{
			// bool flag = true;
			Point lowestPoint = Point.Empty;
			int lowest = 100;
			foreach (Point movePoint in ValidMoves(goalX, goalY))
			{
				int count = Squares[movePoint.X, movePoint.Y].DistanceSteps;
				if (count < lowest)
				{
					lowest = count;
					lowestPoint.X = movePoint.X;
					lowestPoint.Y = movePoint.Y;
				}
			}
			if (lowest != 100)
			{
				Squares[lowestPoint.X, lowestPoint.Y].IsPath = true;
				goalX = lowestPoint.X;
				goalY = lowestPoint.Y;
				Path.Add(new Coord(lowestPoint.X, lowestPoint.Y));
				continue;
			}
			break;
		}
		while (goalX != UserX || goalY != UserY);
		return Path;
	}

	private bool IsSquareOpen(int x, int y, bool CheckHeight)
	{
		if (Room.ValidTile(x, y) && User.AllowOverride)
		{
			return true;
		}
		if (User.X == x && User.Y == y)
		{
			return true;
		}
		bool isLastStep = false;
		if (User.GoalX == x && User.GoalY == y)
		{
			isLastStep = true;
		}
		if (!Room.CanWalk(x, y, 0.0, isLastStep))
		{
			return false;
		}
		return true;
	}

	private bool ValidCoordinates(int x, int y)
	{
		if (x < 0 || y < 0 || x > mapSizeX || y > mapSizeY)
		{
			return false;
		}
		return true;
	}

	public void InitMovements(int movementCount)
	{
		if (movementCount == 4)
		{
			Movements = new Point[8]
			{
				new Point(0, -1),
				new Point(1, 0),
				new Point(0, 1),
				new Point(-1, 0),
				new Point(-1, -1),
				new Point(1, -1),
				new Point(1, 1),
				new Point(-1, 1)
			};
		}
		else
		{
			Movements = new Point[8]
			{
				new Point(0, -1),
				new Point(1, 0),
				new Point(0, 1),
				new Point(-1, 0),
				new Point(-1, -1),
				new Point(1, -1),
				new Point(1, 1),
				new Point(-1, 1)
			};
		}
	}
}
