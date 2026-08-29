using System.Net;
using System.Net.Sockets;

namespace KanbanC.PlaywrightTests.Infrastructure;

public static class FreierPort
{
    public static int Ermittle()
    {
        var horcher = new TcpListener(IPAddress.Loopback, 0);
        horcher.Start();
        var port = ((IPEndPoint)horcher.LocalEndpoint).Port;
        horcher.Stop();
        return port;
    }
}
