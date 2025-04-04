namespace Zero.Hotel.Users.Subscriptions;

internal class Subscription
{
	private string Caption;

	private int TimeActivated;

	private int TimeExpire;

	public string SubscriptionId => Caption;

	public int ExpireTime => TimeExpire;

	public Subscription(string Caption, int TimeActivated, int TimeExpire)
	{
		this.Caption = Caption;
		this.TimeActivated = TimeActivated;
		this.TimeExpire = TimeExpire;
	}

	public bool IsValid()
	{
		if ((double)TimeExpire <= HolographEnvironment.GetUnixTimestamp())
		{
			return false;
		}
		return true;
	}

	public void ExtendSubscription(int Time)
	{
		TimeExpire += Time;
	}
}
