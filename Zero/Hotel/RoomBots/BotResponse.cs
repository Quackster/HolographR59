using System.Collections.Generic;

namespace Zero.Hotel.RoomBots;

internal class BotResponse
{
	private uint Id;

	public uint BotId;

	public List<string> Keywords;

	public string ResponseText;

	public string ResponseType;

	public int ServeId;

	public BotResponse(uint Id, uint BotId, string Keywords, string ResponseText, string ResponseType, int ServeId)
	{
		this.Id = Id;
		this.BotId = BotId;
		this.Keywords = new List<string>();
		this.ResponseText = ResponseText;
		this.ResponseType = ResponseType;
		this.ServeId = ServeId;
		string[] array = Keywords.Split(';');
		foreach (string Keyword in array)
		{
			this.Keywords.Add(Keyword.ToLower());
		}
	}

	public bool KeywordMatched(string Message)
	{
		lock (Keywords)
		{
			foreach (string Keyword in Keywords)
			{
				if (Message.ToLower().Contains(Keyword.ToLower()))
				{
					return true;
				}
			}
		}
		return false;
	}
}
