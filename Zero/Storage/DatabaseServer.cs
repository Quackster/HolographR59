namespace Zero.Storage;

internal class DatabaseServer
{
    public string Hostname;

    public string Password;

    public uint Port;

    public string Username;

    public DatabaseServer(string _Hostname, uint _Port, string _Username, string _Password)
    {
        Hostname = _Hostname;
        Port = _Port;
        Username = _Username;
        Password = _Password;
    }
}
