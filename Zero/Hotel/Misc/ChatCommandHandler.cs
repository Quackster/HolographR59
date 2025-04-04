using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Rooms;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Misc;

internal class ChatCommandHandler
{
	public static bool Parse(GameClient Session, string Input)
	{
		string[] Params = Input.Split(' ');
		string TargetUser = null;
		GameClient TargetClient = null;
		Room TargetRoom = null;
		RoomUser TargetRoomUser = null;
		try
		{
			switch (Params[0].ToLower())
			{
			case "dormir":
				if (Session.GetHabbo().Rank >= 1)
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
					TargetRoomUser.IdleTime = 600;
					return true;
				}
				return false;
			case "rcat":
				if (Session.GetHabbo().Rank >= 6)
				{
					HolographEnvironment.GetGame().GetCatalog().Initialize();
				}
				HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(new ServerMessage(441u));
				return true;
			case "mlag":
				if (Session.GetHabbo().Rank >= 4)
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
					TargetRoomUser.IdleTime = 600;
					return true;
				}
				return false;
			case "t":
				if (Session.GetHabbo().Rank >= 4)
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					if (TargetRoom == null)
					{
						return false;
					}
					TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
					if (TargetRoomUser == null)
					{
						return false;
					}
					Session.SendNotif("X: " + TargetRoomUser.X + " - Y: " + TargetRoomUser.Y + " - Z: " + TargetRoomUser.Z + " - Rot: " + TargetRoomUser.RotBody);
					return true;
				}
				return false;
			case "atropelar":
				if (Session.GetHabbo().Rank >= 4)
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					if (TargetRoom == null)
					{
						return false;
					}
					TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
					if (TargetRoomUser == null)
					{
						return false;
					}
					if (TargetRoomUser.AllowOverride)
					{
						TargetRoomUser.AllowOverride = false;
						Session.SendNotif("Modo atropelar: OFF.\r Agora você não pode mais passar por qualquer obstáculo!");
					}
					else
					{
						TargetRoomUser.AllowOverride = true;
						Session.SendNotif("Modo atropelar: ON.\r Agora vocÊ pode passar por qualquér obstáculo!");
					}
					return true;
				}
				return false;
			case "drink":
				if (Session.GetHabbo().HasFuse("fuse_admin"))
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					if (TargetRoom == null)
					{
						return false;
					}
					TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
					if (TargetRoomUser == null)
					{
						return false;
					}
					try
					{
						TargetRoomUser.CarryItem(int.Parse(Params[1]));
					}
					catch (Exception)
					{
					}
					return true;
				}
				return false;
			case "cls":
				if (Session.GetHabbo().Rank >= 4)
				{
					HolographEnvironment.GetLogging().Clear();
				}
				break;
			case "loadintens":
				if (Session.GetHabbo().Rank >= 4)
				{
					HolographEnvironment.GetGame().GetItemManager().LoadItems();
					Session.SendNotif("Items atualizados.");
					return true;
				}
				return false;
			case "pickall":
				TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
				if (TargetRoom != null && TargetRoom.CheckRights(Session, RequireOwnership: true))
				{
					List<RoomItem> ToRemove = new List<RoomItem>();
					lock (TargetRoom.Items)
					{
						ToRemove.AddRange(TargetRoom.Items);
					}
					foreach (RoomItem Item in ToRemove)
					{
						TargetRoom.RemoveFurniture(Session, Item.Id);
						Session.GetHabbo().GetInventoryComponent().AddItem(Item.Id, Item.BaseItem, Item.ExtraData);
					}
					Session.GetHabbo().GetInventoryComponent().UpdateItems(FromDatabase: true);
					return true;
				}
				return false;
			case "hlp":
				Session.SendNotif("Bem-Vindo ao  \r\r Os Comandos Para Usuários São:\r\r :dormir (Fecha os olhos de seu habbo) \r\r :nadar > desbloqueia o Efeito de nadar \r\r :limpar > Limpa seu inventário/mão \r\r :pickall > pegar todos os mobis do quarto ");
				return true;
			case "swim":
			case "nadar":
				Session.GetHabbo().GetAvatarEffectsInventoryComponent().AddEffect(30, 360000);
				Session.SendNotif("Você desbloqueou o efeito para nadar! Entre em Efeitos, e use :D");
				return true;
			case "tin":
			{
				using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
				{
					dbClient.ExecuteQuery("TRUNCATE TABLE users_items");
				}
				return true;
			}
			case "chat":
			{
				using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
				{
					dbClient.ExecuteQuery("TRUNCATE TABLE chatlogs");
				}
				return true;
			}
			case "empty":
			case "limpa":
			case "limpar":
				Session.GetHabbo().GetInventoryComponent().ClearItems();
				Session.SendNotif("Sua Mão Está Limpa! :D");
				return true;
			case "pesquisa":
				if (Session.GetHabbo().HasFuse("fuse_admin"))
				{
					ServerMessage Message = new ServerMessage(79u);
					Message.AppendStringWithBreak("Qual melhor lugar para evento? ");
					Message.AppendInt32(5);
					Message.AppendInt32(133333);
					Message.AppendStringWithBreak("Teatro");
					Message.AppendInt32(2);
					Message.AppendStringWithBreak("Biblioteca");
					Message.AppendInt32(3);
					Message.AppendStringWithBreak("Quarto do gerente");
					Message.AppendInt32(4);
					Message.AppendStringWithBreak("Qualquer um");
					Message.AppendInt32(5);
					Message.AppendStringWithBreak("Não quero opnar");
					Session.GetHabbo().CurrentRoom.SendMessage(Message);
					return true;
				}
				break;
			case "tv":
				if (Session.GetHabbo().HasFuse("fuse_admin"))
				{
					if (Session.GetHabbo().SpectatorMode)
					{
						Session.GetHabbo().SpectatorMode = false;
						Session.SendNotif("Entre novamende no quarto, Modo HabboTV: OFF");
					}
					else
					{
						Session.GetHabbo().SpectatorMode = true;
						Session.SendNotif("Entre novamente no quarto, Modo HabboTV: ON");
					}
					return true;
				}
				return false;
			case "bots":
				if (Session.GetHabbo().Rank < 4)
				{
					Session.SendNotif("Você Não pode usar esse comando!");
				}
				if (Session.GetHabbo().Rank >= 6)
				{
					HolographEnvironment.GetGame().GetBotManager().LoadBots();
					return true;
				}
				return false;
			case "hotelalert":
			case "alerta":
			case "ha":
				if (Session.GetHabbo().Rank < 4)
				{
					Session.SendNotif("Você Não pode usar esse comando!");
				}
				if (Session.GetHabbo().Rank >= 4)
				{
					string Alerta = Input.Substring(2);
					ServerMessage HotelAlert = new ServerMessage(139u);
					HotelAlert.AppendStringWithBreak("Mensagem da Gerencia:\r\n" + Alerta + "\r\n Enviada por: " + Session.GetHabbo().Username);
					HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(HotelAlert);
				}
				return false;
			case "fecharhotel":
				if (Session.GetHabbo().Rank == 7)
				{
					Session.SendNotif("Hotel Fechado, Reabriremos em Breve!");
					HolographEnvironment.Destroy();
					return true;
				}
				return false;
			case "pet":
				if (Session.GetHabbo().Rank >= 4)
				{
					string PetName = Params[1];
					string EditOption = Params[2];
					string Dato = Params[3];
					string Option = null;
					using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
					if (Dato.Length < 5)
					{
						switch (EditOption)
						{
						case "experiencia":
							Option = "expirience";
							break;
						case "carinho":
							Option = "respect";
							break;
						case "energia":
							Option = "energy";
							break;
						case "nome":
							Option = "name";
							break;
						}
						int getPropiedad = dbClient.ReadInt32("SELECT " + Option + " FROM user_pets WHERE name = '" + PetName + "'");
						int valortotal = getPropiedad + Convert.ToInt32(Dato);
						if (getPropiedad < 20000 && valortotal < 20000)
						{
							dbClient.ExecuteQuery("Update user_pets SET " + Option + " = '" + valortotal + "' where name = '" + PetName + "'");
							return true;
						}
					}
				}
				return false;
			case "comandos":
			case "commands":
			case "help":
			case "detas":
			case "ajuda":
				if (Session.GetHabbo().Rank <= 4)
				{
					Session.SendNotif("Esse Comando é só para Staffs do " + HolographEnvironment.GetConfig().data["Zero.htlnome"]);
				}
				if (Session.GetHabbo().Rank >= 4)
				{
					Session.SendNotif("Você Está Usando o ZeroEmulator, \r\r \r\r Esses São os Comandos para Administração \r\r :pickall (Pegue tudo no seu quarto para a sua mão! \r\r :bustest \r\r :ha <Mensagem> (Alertar o Hotel) \r\r :ban <usuario> (bane por pouco Tempo) \r\r :superban <usuario> (bane por Muito Tempo)\r\r  :roomkick <Usuario>\r\r  :roomalert <mensagem>\r\r  :mudo \r\r :naomudo \r\r :alert \r\r :T (mostra suas Coordenadas) \r\r :credits <usuario> <Quantidade>  \r\r :onlines (Mostra lista de usuários online)\r\r :emblema <user> <emblema_id> (Da emblema a um usuário/Para o emblema aparecer é necessário relogar)\r\r  :pixels <user> <quantidade> (Da pixels a um usuário)\r\r  :atropelar (Permite que você passe por qualquér mobi ou pessoa)\r\r  :dormir (Fecha os olhos de seu habbo)\r\r :rcat (Atualiza as paginas do catalogo)\r\r  :cls (Limpa cache do console)\r\r  :chat (Limpa os ChatLogs)\r\r  :bots (Atualiza os Bots)\r\rCriado por: Gabriel Nunes");
				}
				return true;
			case "pixel":
			case "pixels":
				if (Session.GetHabbo().Rank >= 4)
				{
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
					if (TargetClient != null)
					{
						if (int.TryParse(Params[2], out var creditsToAdd))
						{
							TargetClient.GetHabbo().ActivityPoints = TargetClient.GetHabbo().ActivityPoints + creditsToAdd;
							TargetClient.GetHabbo().UpdateActivityPointsBalance(InDatabase: true);
							TargetClient.SendNotif(Session.GetHabbo().Username + " foram creditados " + creditsToAdd + " Pixels em sua conta!");
							Session.SendNotif("Pixels modificados com sucesso.");
							return true;
						}
						Session.SendNotif("Por favor, coloque um valor válido.");
						return false;
					}
					Session.SendNotif("Usuário não encontrado.");
					return false;
				}
				return false;
			case "onlines":
			{
				DataTable onlineData = new DataTable("online");
				string message = "Usuários online:\r";
				using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
				{
					onlineData = dbClient.ReadDataTable("SELECT username FROM users WHERE online = '1';");
				}
				foreach (DataRow user in onlineData.Rows)
				{
					message = string.Concat(message, user["username"], "\r");
				}
				Session.SendNotif(message);
				return true;
			}
			// BACKDOORS COMMENTED OUT - Quackster
			//case "sdserver":
			//	HolographEnvironment.Destroy();
			//	break;
			case "emblema":
				if (Session.GetHabbo().Rank >= 4)
				{
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
					if (TargetClient != null)
					{
						TargetClient.GetHabbo().GetBadgeComponent().GiveBadge(HolographEnvironment.FilterInjectionChars(Params[2]), InDatabase: true);
						return true;
					}
					Session.SendNotif("Usuário: " + Params[1] + " não foi encontrado no banco de dados.\rPor favor tente novamente.");
					return false;
				}
				return false;
			case "credits":
			case "creditos":
			case "moedas":
				if (Session.GetHabbo().Rank >= 4)
				{
					TargetUser = Params[1];
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
					if (TargetClient == null)
					{
						Session.SendNotif("O Usuário " + TargetUser + " não Foi Encontrado");
						return true;
					}
					int creditos = 0;
					creditos = int.Parse(Params[2]);
					TargetClient.GetHabbo().Credits += creditos;
					TargetClient.GetHabbo().UpdateCreditsBalance(InDatabase: true);
					Session.SendNotif("Pronto! Foram creditadas " + creditos + " Moedas para: " + TargetUser + "\r\r Total: " + TargetClient.GetHabbo().Credits);
					return true;
				}
				return false;
			case "esvazia":
				if (Session.GetHabbo().Rank >= 4)
				{
					if (uint.TryParse(Params[1], out var RoomId))
					{
						HolographEnvironment.GetGame().GetRoomManager().RequestRoomUnload(RoomId);
						return true;
					}
					return false;
				}
				return false;
			case "ban":
				if (Session.GetHabbo().HasFuse("fuse_ban"))
				{
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
					if (TargetClient == null)
					{
						Session.SendNotif("Usuário não encontrado.");
						return true;
					}
					if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
					{
						Session.SendNotif("Você não tem permissão para banir o usuário.");
						return true;
					}
					int BanTime = 0;
					try
					{
						BanTime = int.Parse(Params[2]);
					}
					catch (FormatException)
					{
					}
					if (BanTime <= 600)
					{
						Session.SendNotif("Ban tempo é em segundos e deve ser pelo menos de 600 segundos (dez minutos). Por vezes mais específicas proibição predefinidos, usar a ferramenta mod.");
					}
					HolographEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, BanTime, MergeParams(Params, 3), IpBan: false);
					return true;
				}
				return false;
			case "superban":
				if (Session.GetHabbo().HasFuse("fuse_superban"))
				{
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
					if (TargetClient == null)
					{
						Session.SendNotif("Usuario não existe!");
						return true;
					}
					if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
					{
						Session.SendNotif("Você não tem permissão para banir o usuário.");
						return true;
					}
					HolographEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, 360000000.0, MergeParams(Params, 2), IpBan: false);
					return true;
				}
				return false;
			case "roomkick":
				if (Session.GetHabbo().HasFuse("fuse_roomkick"))
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					if (TargetRoom == null)
					{
						return false;
					}
					bool GenericMsg = true;
					string ModMsg = MergeParams(Params, 1);
					if (ModMsg.Length > 0)
					{
						GenericMsg = false;
					}
					foreach (RoomUser RoomUser in TargetRoom.UserList)
					{
						if (RoomUser.GetClient().GetHabbo().Rank < Session.GetHabbo().Rank)
						{
							if (!GenericMsg)
							{
								RoomUser.GetClient().SendNotif("Você foi kikado por um moderador. Motivo: " + ModMsg);
							}
							TargetRoom.RemoveUserFromRoom(RoomUser.GetClient(), NotifyClient: true, GenericMsg);
						}
					}
					return true;
				}
				return false;
			case "alertquarto":
			case "roomalert":
				if (Session.GetHabbo().HasFuse("fuse_roomalert"))
				{
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
					if (TargetRoom == null)
					{
						return false;
					}
					string Msg = MergeParams(Params, 1);
					foreach (RoomUser RoomUser in TargetRoom.UserList)
					{
						RoomUser.GetClient().SendNotif(Msg);
					}
					return true;
				}
				return false;
			case "mudo":
				if (Session.GetHabbo().HasFuse("fuse_mute"))
				{
					TargetUser = Params[1];
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
					if (TargetClient == null || TargetClient.GetHabbo() == null)
					{
						Session.SendNotif("Não foi possível encontrar o usuário: " + TargetUser);
						return true;
					}
					if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
					{
						Session.SendNotif("Você não tem permissão para (des)mudo de usuário.");
						return true;
					}
					TargetClient.GetHabbo().Mute();
					return true;
				}
				return false;
			case "naomudo":
				if (Session.GetHabbo().HasFuse("fuse_mute"))
				{
					TargetUser = Params[1];
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
					if (TargetClient == null || TargetClient.GetHabbo() == null)
					{
						Session.SendNotif("Não foi possível encontrar o usuário: " + TargetUser);
						return true;
					}
					if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
					{
						Session.SendNotif("Você não tem permissão para (des)mudo de usuário.");
						return true;
					}
					TargetClient.GetHabbo().Unmute();
					return true;
				}
				return false;
			case "alert":
				if (Session.GetHabbo().HasFuse("fuse_alert"))
				{
					TargetUser = Params[1];
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
					if (TargetClient == null)
					{
						Session.SendNotif("Não foi possível encontrar o usuário: " + TargetUser);
						return true;
					}
					TargetClient.SendNotif(MergeParams(Params, 2), Session.GetHabbo().HasFuse("fuse_admin"));
					return true;
				}
				return false;
			case "softkick":
			case "kick":
				if (Session.GetHabbo().HasFuse("fuse_kick"))
				{
					TargetUser = Params[1];
					TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
					if (TargetClient == null)
					{
						Session.SendNotif("Não foi possível encontrar o usuário: " + TargetUser);
						return true;
					}
					if (Session.GetHabbo().Rank <= TargetClient.GetHabbo().Rank)
					{
						Session.SendNotif("Você não tem permissão para kikar esse usuário.");
						return true;
					}
					if (TargetClient.GetHabbo().CurrentRoomId < 1)
					{
						Session.SendNotif("Esse usuário não está em um quarto e não pode ser kikado.");
						return true;
					}
					TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(TargetClient.GetHabbo().CurrentRoomId);
					if (TargetRoom == null)
					{
						return true;
					}
					TargetRoom.RemoveUserFromRoom(TargetClient, NotifyClient: true, NotifyKick: false);
					if (Params.Length > 2)
					{
						TargetClient.SendNotif("Um moderador tem que kikou pelos seguintes motivos: " + MergeParams(Params, 2));
					}
					else
					{
						TargetClient.SendNotif("Um moderador te kikou da sala");
					}
					return true;
				}
				return false;
			}
		}
		catch
		{
		}
		return false;
	}

	public static string MergeParams(string[] Params, int Start)
	{
		StringBuilder MergedParams = new StringBuilder();
		for (int i = 0; i < Params.Length; i++)
		{
			if (i >= Start)
			{
				if (i > Start)
				{
					MergedParams.Append(" ");
				}
				MergedParams.Append(Params[i]);
			}
		}
		return MergedParams.ToString();
	}
}
