using System.Data;
using Zero.Storage;

namespace Zero.Hotel.Users.Authenticator;

internal class Authenticator
{
	public static Habbo TryLoginHabbo(string AuthTicket)
	{
		DataRow Row = null;
		if (AuthTicket.Length < 10)
		{
			throw new IncorrectLoginException("Autorização/Ticket SSO invalidos");
		}
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("auth_ticket", AuthTicket);
			Row = dbClient.ReadDataRow("SELECT * FROM users WHERE auth_ticket = @auth_ticket LIMIT 1");
		}
		if (Row == null)
		{
			throw new IncorrectLoginException("Autorização/Ticket SSO invalidos");
		}
		if (!HolographEnvironment.GetGame().GetRoleManager().RankHasRight((uint)Row["rank"], "fuse_login"))
		{
			throw new IncorrectLoginException("Não é permitido fazer o login, devido ao papel / restrição de direito (fuse_login is missing)");
		}
		if (Row["newbie_status"].ToString() == "0")
		{
			throw new IncorrectLoginException("Não é permitido fazer o login, você ainda é novato");
		}
		return GenerateHabbo(Row, AuthTicket);
	}

	public static Habbo GenerateHabbo(DataRow Data, string AuthTicket)
	{
		return new Habbo((uint)Data["id"], (string)Data["username"], (string)Data["real_name"], AuthTicket, (uint)Data["rank"], (string)Data["motto"], (string)Data["look"], (string)Data["gender"], (int)Data["credits"], (int)Data["activity_points"], (double)Data["activity_points_lastUpdate"], HolographEnvironment.EnumToBool(Data["is_muted"].ToString()), (uint)Data["home_room"], (int)Data["respect"], (int)Data["daily_respect_points"], (int)Data["daily_pet_respect_points"], (int)Data["newbie_status"], Data["mutant_penalty"].ToString() != "0", HolographEnvironment.EnumToBool(Data["block_newfriends"].ToString()));
	}
}
