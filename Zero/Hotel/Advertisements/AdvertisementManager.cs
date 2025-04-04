using System.Collections.Generic;
using System.Data;
using Zero.Storage;

namespace Zero.Hotel.Advertisements;

internal class AdvertisementManager
{
	public List<RoomAdvertisement> RoomAdvertisements;

	public AdvertisementManager()
	{
		RoomAdvertisements = new List<RoomAdvertisement>();
	}

	public void LoadRoomAdvertisements()
	{
		RoomAdvertisements.Clear();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM room_ads WHERE enabled = '1'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			RoomAdvertisements.Add(new RoomAdvertisement((uint)Row["id"], (string)Row["ad_image"], (string)Row["ad_link"], (int)Row["views"], (int)Row["views_limit"]));
		}
	}

	public RoomAdvertisement GetRandomRoomAdvertisement()
	{
		if (RoomAdvertisements.Count <= 0)
		{
			return null;
		}
		int RndId;
		do
		{
			// bool flag = true;
			RndId = HolographEnvironment.GetRandomNumber(0, RoomAdvertisements.Count - 1);
		}
		while (RoomAdvertisements[RndId] == null || RoomAdvertisements[RndId].ExceededLimit);
		return RoomAdvertisements[RndId];
	}
}
