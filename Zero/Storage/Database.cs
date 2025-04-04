namespace Zero.Storage;

internal class Database
{
	public string DatabaseName;

	public uint PoolMaxSize;

	public uint PoolMinSize;

	public Database(string _DatabaseName, uint _PoolMinSize, uint _PoolMaxSize)
	{
		DatabaseName = _DatabaseName;
		PoolMinSize = _PoolMinSize;
		PoolMaxSize = _PoolMaxSize;
	}
}
