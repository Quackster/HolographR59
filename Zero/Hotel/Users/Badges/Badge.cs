namespace Zero.Hotel.Users.Badges;

internal class Badge
{
	public string Code;

	public int Slot;

	public Badge(string Code, int Slot)
	{
		this.Code = Code;
		this.Slot = Slot;
	}
}
