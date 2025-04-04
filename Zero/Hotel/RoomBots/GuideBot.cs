using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;
using Zero.Messages;

namespace Zero.Hotel.RoomBots;

internal class GuideBot : BotAI
{
	private int SpeechTimer;

	private int ActionTimer;

	public GuideBot()
	{
		SpeechTimer = 0;
		ActionTimer = 0;
	}

	public override void OnSelfEnterRoom()
	{
		GetRoomUser().Chat(null, "Hi and welcome to Zero! I am a bot Guide and I'm here to help you.", Shout: false);
		GetRoomUser().Chat(null, "This is your own room, you can always come back to room by clicking the nest icon on the left.", Shout: false);
		GetRoomUser().Chat(null, "If you want to explore the Habbo by yourself, click on the orange hotel icon on the left (we call it navigator).", Shout: false);
		GetRoomUser().Chat(null, "You will find cool rooms and fun events with other people in them, feel free to visit them.", Shout: false);
		GetRoomUser().Chat(null, "I can give you tips and hints on what to do here, just ask me a question :)", Shout: false);
	}

	public override void OnSelfLeaveRoom(bool Kicked)
	{
	}

	public override void OnUserEnterRoom(RoomUser User)
	{
	}

	public override void OnUserLeaveRoom(GameClient Client)
	{
		if (GetRoom().Owner.ToLower() == Client.GetHabbo().Username.ToLower())
		{
			GetRoom().RemoveBot(GetRoomUser().VirtualId, Kicked: false);
		}
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
			SpeechTimer = HolographEnvironment.GetRandomNumber(0, 150);
		}
		else
		{
			SpeechTimer--;
		}
		if (ActionTimer <= 0)
		{
			int randomX = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeX);
			int randomY = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeY);
			GetRoomUser().MoveTo(randomX, randomY);
			ActionTimer = HolographEnvironment.GetRandomNumber(0, 30);
		}
		else
		{
			ActionTimer--;
		}
	}
}
