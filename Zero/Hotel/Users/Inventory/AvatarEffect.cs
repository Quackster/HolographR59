namespace Zero.Hotel.Users.Inventory;

internal class AvatarEffect
{
	public int EffectId;

	public int TotalDuration;

	public bool Activated;

	public double StampActivated;

	public int TimeLeft
	{
		get
		{
			if (!Activated)
			{
				return -1;
			}
			double diff = HolographEnvironment.GetUnixTimestamp() - StampActivated;
			if (diff >= (double)TotalDuration)
			{
				return 0;
			}
			return (int)((double)TotalDuration - diff);
		}
	}

	public bool HasExpired
	{
		get
		{
			if (TimeLeft == -1)
			{
				return false;
			}
			if (TimeLeft <= 0)
			{
				return true;
			}
			return false;
		}
	}

	public AvatarEffect(int EffectId, int TotalDuration, bool Activated, double ActivateTimestamp)
	{
		this.EffectId = EffectId;
		this.TotalDuration = TotalDuration;
		this.Activated = Activated;
		StampActivated = ActivateTimestamp;
	}

	public void Activate()
	{
		Activated = true;
		StampActivated = HolographEnvironment.GetUnixTimestamp();
	}
}
