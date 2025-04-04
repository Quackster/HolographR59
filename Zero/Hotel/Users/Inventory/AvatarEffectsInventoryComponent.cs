using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Users.Inventory;

internal class AvatarEffectsInventoryComponent
{
	private List<AvatarEffect> Effects;

	private uint UserId;

	public int CurrentEffect;

	public int Count => Effects.Count;

	public AvatarEffectsInventoryComponent(uint UserId)
	{
		Effects = new List<AvatarEffect>();
		this.UserId = UserId;
		CurrentEffect = -1;
	}

	public void LoadEffects()
	{
		Effects.Clear();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM user_effects WHERE user_id = '" + UserId + "'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			AvatarEffect Effect = new AvatarEffect((int)Row["effect_id"], (int)Row["total_duration"], HolographEnvironment.EnumToBool(Row["is_activated"].ToString()), (double)Row["activated_stamp"]);
			if (Effect.HasExpired)
			{
				using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
				dbClient.ExecuteQuery("DELETE FROM user_effects WHERE user_id = '" + UserId + "' AND effect_id = '" + Effect.EffectId + "' LIMIT 1");
			}
			else
			{
				Effects.Add(Effect);
			}
		}
	}

	public void AddEffect(int EffectId, int Duration)
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.ExecuteQuery("INSERT INTO user_effects (user_id,effect_id,total_duration,is_activated,activated_stamp) VALUES ('" + UserId + "','" + EffectId + "','" + Duration + "','0','0')");
		}
		Effects.Add(new AvatarEffect(EffectId, Duration, Activated: false, 0.0));
		GetClient().GetMessageHandler().GetResponse().Init(461u);
		GetClient().GetMessageHandler().GetResponse().AppendInt32(EffectId);
		GetClient().GetMessageHandler().GetResponse().AppendInt32(Duration);
		GetClient().GetMessageHandler().SendResponse();
	}

	public void StopEffect(int EffectId)
	{
		AvatarEffect Effect = GetEffect(EffectId, IfEnabledOnly: true);
		if (Effect != null && Effect.HasExpired)
		{
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.ExecuteQuery("DELETE FROM user_effects WHERE user_id = '" + UserId + "' AND effect_id = '" + EffectId + "' AND is_activated = '1' LIMIT 1");
			}
			Effects.Remove(Effect);
			GetClient().GetMessageHandler().GetResponse().Init(463u);
			GetClient().GetMessageHandler().GetResponse().AppendInt32(EffectId);
			GetClient().GetMessageHandler().SendResponse();
			if (CurrentEffect >= 0)
			{
				ApplyEffect(-1);
			}
		}
	}

	public void ApplyEffect(int EffectId)
	{
		if (!HasEffect(EffectId, IfEnabledOnly: true))
		{
			return;
		}
		Room Room = GetUserRoom();
		if (Room != null)
		{
			RoomUser User = Room.GetRoomUserByHabbo(GetClient().GetHabbo().Id);
			if (User != null)
			{
				CurrentEffect = EffectId;
				ServerMessage Message = new ServerMessage(485u);
				Message.AppendInt32(User.VirtualId);
				Message.AppendInt32(EffectId);
				Room.SendMessage(Message);
			}
		}
	}

	public void EnableEffect(int EffectId)
	{
		AvatarEffect Effect = GetEffect(EffectId, IfEnabledOnly: false);
		if (Effect != null && !Effect.HasExpired && !Effect.Activated)
		{
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.ExecuteQuery("Update user_effects SET is_activated = '1', activated_stamp = '" + HolographEnvironment.GetUnixTimestamp() + "' WHERE user_id = '" + UserId + "' AND effect_id = '" + EffectId + "' LIMIT 1");
			}
			Effect.Activate();
			GetClient().GetMessageHandler().GetResponse().Init(462u);
			GetClient().GetMessageHandler().GetResponse().AppendInt32(Effect.EffectId);
			GetClient().GetMessageHandler().GetResponse().AppendInt32(Effect.TotalDuration);
			GetClient().GetMessageHandler().SendResponse();
		}
	}

	public bool HasEffect(int EffectId, bool IfEnabledOnly)
	{
		if (EffectId == -1)
		{
			return true;
		}
		lock (Effects)
		{
			foreach (AvatarEffect Effect in Effects)
			{
				if ((IfEnabledOnly && !Effect.Activated) || Effect.HasExpired || Effect.EffectId != EffectId)
				{
					continue;
				}
				return true;
			}
		}
		return false;
	}

	public AvatarEffect GetEffect(int EffectId, bool IfEnabledOnly)
	{
		lock (Effects)
		{
			foreach (AvatarEffect Effect in Effects)
			{
				if ((!IfEnabledOnly || Effect.Activated) && Effect.EffectId == EffectId)
				{
					return Effect;
				}
			}
		}
		return null;
	}

	public ServerMessage Serialize()
	{
		ServerMessage Message = new ServerMessage(460u);
		Message.AppendInt32(Count);
		lock (Effects)
		{
			foreach (AvatarEffect Effect in Effects)
			{
				Message.AppendInt32(Effect.EffectId);
				Message.AppendInt32(Effect.TotalDuration);
				Message.AppendBoolean(!Effect.Activated);
				Message.AppendInt32(Effect.TimeLeft);
			}
		}
		return Message;
	}

	public void CheckExpired()
	{
		lock (Effects)
		{
			List<int> ToRemove = new List<int>();
			foreach (AvatarEffect Effect in Effects)
			{
				if (Effect.HasExpired)
				{
					ToRemove.Add(Effect.EffectId);
				}
			}
			foreach (int trmv in ToRemove)
			{
				StopEffect(trmv);
			}
		}
	}

	private GameClient GetClient()
	{
		return HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
	}

	private Room GetUserRoom()
	{
		return HolographEnvironment.GetGame().GetRoomManager().GetRoom(GetClient().GetHabbo().CurrentRoomId);
	}
}
