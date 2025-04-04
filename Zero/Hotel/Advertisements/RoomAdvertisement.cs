using Zero.Storage;

namespace Zero.Hotel.Advertisements;

internal class RoomAdvertisement
{
    public uint Id;

    public string AdImage;

    public string AdLink;

    public int Views;

    public int ViewsLimit;

    public bool ExceededLimit
    {
        get
        {
            if (ViewsLimit <= 0)
            {
                return false;
            }
            if (Views >= ViewsLimit)
            {
                return true;
            }
            return false;
        }
    }

    public RoomAdvertisement(uint Id, string AdImage, string AdLink, int Views, int ViewsLimit)
    {
        this.Id = Id;
        this.AdImage = AdImage;
        this.AdLink = AdLink;
        this.Views = Views;
        this.ViewsLimit = ViewsLimit;
    }

    public void OnView()
    {
        Views++;
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.ExecuteQuery("Update room_ads SET views = views + 1 WHERE id = '" + Id + "'");
    }
}
