namespace Zero.Hotel.Pathfinding;

internal class CompleteSquare
{
    public int x = 0;

    public int y = 0;

    private int _distanceSteps = 100;

    private bool _isPath = false;

    public int DistanceSteps
    {
        get
        {
            return _distanceSteps;
        }
        set
        {
            _distanceSteps = value;
        }
    }

    public bool IsPath
    {
        get
        {
            return _isPath;
        }
        set
        {
            _isPath = value;
        }
    }

    public CompleteSquare(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
