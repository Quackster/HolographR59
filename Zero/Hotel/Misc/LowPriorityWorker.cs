using System;
using System.Threading;
using Zero.Storage;

namespace Zero.Hotel.Misc;

public class LowPriorityWorker
{
	public static void Process()
	{
		Thread.Sleep(10000);
		while (true)
		{
			bool flag = true;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			int Status = 1;
			int UsersOnline = HolographEnvironment.GetGame().GetClientManager().ClientCount;
			int RoomsLoaded = HolographEnvironment.GetGame().GetRoomManager().LoadedRoomsCount;
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.ExecuteQuery("Update server_status SET stamp = '" + HolographEnvironment.GetUnixTimestamp() + "', status = '" + Status + "', users_online = '" + UsersOnline + "', rooms_loaded = '" + RoomsLoaded + "', server_ver = '" + HolographEnvironment.Versao + "' LIMIT 1");
			}
			HolographEnvironment.GetGame().GetClientManager().CheckEffects();
			Thread.Sleep(30000);
		}
	}
}
