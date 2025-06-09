using System;
using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Storage;

namespace Zero.Hotel.Support;

internal class ModerationBanManager
{
    public SynchronizedCollection<ModerationBan> Bans;

    public ModerationBanManager()
    {
        Bans = new SynchronizedCollection<ModerationBan>();
    }

    public void LoadBans()
    {
        Bans.Clear();
        DataTable BanData = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            BanData = dbClient.ReadDataTable("SELECT bantype,value,reason,expire FROM bans WHERE expire > '" + HolographEnvironment.GetUnixTimestamp() + "'");
        }
        if (BanData == null)
        {
            return;
        }
        foreach (DataRow Row in BanData.Rows)
        {
            ModerationBanType Type = ModerationBanType.IP;
            if ((string)Row["bantype"] == "user")
            {
                Type = ModerationBanType.USERNAME;
            }
            Bans.Add(new ModerationBan(Type, (string)Row["value"], (string)Row["reason"], (double)Row["expire"]));
        }
    }

    public void CheckForBanConflicts(GameClient Client)
    {
            foreach (ModerationBan Ban in Bans)
            {
                if (!Ban.Expired)
                {
                    if (Ban.Type == ModerationBanType.IP && Client.GetConnection().IPAddress == Ban.Variable)
                    {
                        throw new ModerationBanException(Ban.ReasonMessage);
                    }
                    if (Client.GetHabbo() != null && Ban.Type == ModerationBanType.USERNAME && Client.GetHabbo().Username.ToLower() == Ban.Variable.ToLower())
                    {
                        throw new ModerationBanException(Ban.ReasonMessage);
                    }
                }
            }
    }

    public void BanUser(GameClient Client, string Moderator, double LengthSeconds, string Reason, bool IpBan)
    {
        ModerationBanType Type = ModerationBanType.USERNAME;
        string Var = Client.GetHabbo().Username;
        string RawVar = "user";
        double Expire = HolographEnvironment.GetUnixTimestamp() + LengthSeconds;
        if (IpBan)
        {
            Type = ModerationBanType.IP;
            Var = Client.GetConnection().IPAddress;
            RawVar = "ip";
        }
        Bans.Add(new ModerationBan(Type, Var, Reason, Expire));
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("INSERT INTO bans (bantype,value,reason,expire,added_by,added_date) VALUES ('" + RawVar + "','" + Var + "','" + Reason + "','" + Expire + "','" + Moderator + "','" + DateTime.Now.ToLongDateString() + "')");
        }
        if (IpBan)
        {
            DataTable UsersAffected = null;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                UsersAffected = dbClient.ReadDataTable("SELECT id FROM users WHERE ip_last = '" + Var + "'");
            }
            if (UsersAffected != null)
            {
                foreach (DataRow Row in UsersAffected.Rows)
                {
                    using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                    dbClient.ExecuteQuery("Update user_info SET bans = bans + 1 WHERE user_id = '" + (uint)Row["id"] + "' LIMIT 1");
                }
            }
        }
        else
        {
            using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
            dbClient.ExecuteQuery("Update user_info SET bans = bans + 1 WHERE user_id = '" + Client.GetHabbo().Id + "' LIMIT 1");
        }
        Client.SendBanMessage("You have been banned: " + Reason);
        Client.Disconnect();
    }
}
