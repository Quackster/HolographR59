namespace Zero.Hotel.Support;

internal class ModerationBan
{
    public ModerationBanType Type;

    public string Variable;

    public string ReasonMessage;

    public double Expire;

    public bool Expired
    {
        get
        {
            if (HolographEnvironment.GetUnixTimestamp() >= Expire)
            {
                return true;
            }
            return false;
        }
    }

    public ModerationBan(ModerationBanType Type, string Variable, string ReasonMessage, double Expire)
    {
        this.Type = Type;
        this.Variable = Variable;
        this.ReasonMessage = ReasonMessage;
        this.Expire = Expire;
    }
}
