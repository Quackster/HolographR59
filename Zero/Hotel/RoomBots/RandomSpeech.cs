namespace Zero.Hotel.RoomBots;

internal class RandomSpeech
{
    public string Message;

    public bool Shout;

    public RandomSpeech(string Message, bool Shout)
    {
        this.Message = Message;
        this.Shout = Shout;
    }
}
