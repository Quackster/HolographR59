using System.Threading;
using Zero.Hotel.GameClients;

namespace Zero.Hotel.Misc;

internal class PixelManager
{
    private const int RCV_EVERY_MINS = 15;

    private const int RCV_AMOUNT = 50;

    public bool KeepAlive;

    private Thread WorkerThread;

    public PixelManager()
    {
        KeepAlive = true;
        WorkerThread = new Thread(Process);
        WorkerThread.Name = "Pixel Manager";
        WorkerThread.Priority = ThreadPriority.Lowest;
    }

    public void Start()
    {
        WorkerThread.Start();
    }

    private void Process()
    {
        try
        {
            while (KeepAlive)
            {
                if (HolographEnvironment.GetGame() != null && HolographEnvironment.GetGame().GetClientManager() != null)
                {
                    HolographEnvironment.GetGame().GetClientManager().CheckPixelUpdates();
                }
                Thread.Sleep(15000);
            }
        }
        catch (ThreadAbortException)
        {
        }
    }

    public bool NeedsUpdate(GameClient Client)
    {
        double PassedMins = (HolographEnvironment.GetUnixTimestamp() - Client.GetHabbo().LastActivityPointsUpdate) / 60.0;
        if (PassedMins >= 15.0)
        {
            return true;
        }
        return false;
    }

    public void GivePixels(GameClient Client)
    {
        double Timestamp = HolographEnvironment.GetUnixTimestamp();
        Client.GetHabbo().LastActivityPointsUpdate = Timestamp;
        Client.GetHabbo().ActivityPoints += 50;
        Client.GetHabbo().UpdateActivityPointsBalance(InDatabase: true, 50);
    }
}
