using System;
using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;
using Zero.Messages;

namespace Zero.Hotel.RoomBots;

internal class GenericBot : BotAI
{
	private int SpeechTimer;

	private int ActionTimer;

	public GenericBot(int VirtualId)
	{
		SpeechTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 250);
		ActionTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 30);
	}

	public override void OnSelfEnterRoom()
	{
	}

	public override void OnSelfLeaveRoom(bool Kicked)
	{
	}

	public override void OnUserEnterRoom(RoomUser User)
	{
	}

	public override void OnUserLeaveRoom(GameClient Client)
	{
	}

	public override void OnUserSay(RoomUser User, string Message)
	{
		if (GetRoom().TileDistance(GetRoomUser().X, GetRoomUser().Y, User.X, User.Y) > 8)
		{
			return;
		}
		BotResponse Response = GetBotData().GetResponse(Message);
		if (Response != null)
		{
			switch (Response.ResponseType.ToLower())
			{
			case "say":
				GetRoomUser().Chat(null, Response.ResponseText, Shout: false);
				break;
			case "shout":
				GetRoomUser().Chat(null, Response.ResponseText, Shout: true);
				break;
			case "whisper":
			{
				ServerMessage TellMsg = new ServerMessage(25u);
				TellMsg.AppendInt32(GetRoomUser().VirtualId);
				TellMsg.AppendStringWithBreak(Response.ResponseText);
				TellMsg.AppendBoolean(Bool: false);
				User.GetClient().SendMessage(TellMsg);
				break;
			}
			}
			if (Response.ServeId >= 1)
			{
				User.CarryItem(Response.ServeId);
			}
		}
	}

	public override void OnUserShout(RoomUser User, string Message)
	{
		if (HolographEnvironment.GetRandomNumber(0, 10) >= 5)
		{
			GetRoomUser().Chat(null, "There's no need to shout!", Shout: true);
		}
	}

	public override void OnTimerTick()
	{
		if (SpeechTimer <= 0)
		{
			if (GetBotData().RandomSpeech.Count > 0)
			{
				RandomSpeech Speech = GetBotData().GetRandomSpeech();
				GetRoomUser().Chat(null, Speech.Message, Speech.Shout);
			}
			SpeechTimer = HolographEnvironment.GetRandomNumber(10, 300);
		}
		else
		{
			SpeechTimer--;
		}
		if (ActionTimer <= 0)
		{
			int randomX = 0;
			int randomY = 0;
			switch (GetBotData().WalkingMode.ToLower())
			{
			case "freeroam":
				randomX = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeX);
				randomY = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeY);
				GetRoomUser().MoveTo(randomX, randomY);
				break;
			case "specified_range":
				randomX = HolographEnvironment.GetRandomNumber(GetBotData().minX, GetBotData().maxX);
				randomY = HolographEnvironment.GetRandomNumber(GetBotData().minY, GetBotData().maxY);
				GetRoomUser().MoveTo(randomX, randomY);
				break;
			}
			ActionTimer = HolographEnvironment.GetRandomNumber(1, 30);
		}
		else
		{
			ActionTimer--;
		}
	}
}
