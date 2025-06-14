using MonoTorrent;
using MonoTorrent.Connections.Dht;
using MonoTorrent.Dht;
using System;
using System.Net;
using System.Threading.Tasks;

public class ProgramOld
{
    /*
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: program.exe <infohash>");
            Console.WriteLine("Example: program.exe 232b369a657f606cf16ed2c8cc11f8079910f9ad");
            return;
        }

        try
        {
            string hashString = args[1];

            // Validate and fix hash length
            if (hashString.Length < 40)
            {
                hashString = hashString.PadLeft(40, '0');
                Console.WriteLine($"Hash padded to: {hashString}");
            }
            else if (hashString.Length > 40 && hashString.Length != 64)
            {
                Console.WriteLine("Error: InfoHash must be 40 characters (V1) or 64 characters (V2)");
                return;
            }

            var infoHash = InfoHash.FromHex(hashString);

            var lobby = new SimpleLobby(infoHash);
            lobby.StartAsync().Wait();

            Console.WriteLine($"Lobby started with InfoHash: {hashString}");
            Console.WriteLine("Press Enter to exit...");

            Console.ReadLine();

            lobby.StopAsync().Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }*/
}

public class SimpleLobby
{
    private readonly DhtEngine dhtEngine;
    private readonly InfoHash infoHash;

    public SimpleLobby(InfoHash infoHash)
    {
        this.infoHash = infoHash;
        this.dhtEngine = new DhtEngine();
    }

    public async Task StartAsync()
    {
        // Setup DHT listener
        var listener = new DhtListener(new IPEndPoint(IPAddress.Any, 6881));
        await dhtEngine.SetListenerAsync(listener);

        // Start DHT engine
        await dhtEngine.StartAsync();

        // Bootstrap with known nodes
        await BootstrapDht();

        // Subscribe to peer events
        dhtEngine.PeersFound += OnPeersFound;

        // Announce our presence
        dhtEngine.Announce(infoHash, 6881);

        dhtEngine.GetPeers(infoHash);
    }

    private async Task BootstrapDht()
    {
        var bootstrapHosts = new[]
        {
            "router.bittorrent.com:6881",
            "dht.transmissionbt.com:6881",
            "router.utorrent.com:6881"
        };

        foreach (var host in bootstrapHosts)
        {
            try
            {
                var parts = host.Split(':');
                var addresses = await Dns.GetHostAddressesAsync(parts[0]);

                foreach (var address in addresses)
                {
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var endpoint = new IPEndPoint(address, int.Parse(parts[1]));
                        // Convert IPEndPoint to compact node format (6 bytes: 4 for IP, 2 for port)
                        var ipBytes = address.GetAddressBytes();
                        var portBytes = BitConverter.GetBytes((ushort)endpoint.Port);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(portBytes);

                        var compactNode = new byte[6];
                        Array.Copy(ipBytes, 0, compactNode, 0, 4);
                        Array.Copy(portBytes, 0, compactNode, 4, 2);

                        dhtEngine.Add(new[] { new ReadOnlyMemory<byte>(compactNode) });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add bootstrap node {host}: {ex.Message}");
            }
        }
    }

    private void OnPeersFound(object? sender, PeersFoundEventArgs e)
    {
        if (!e.InfoHash.Equals(infoHash)) return;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Found {e.Peers.Count} peers:");
        foreach (var peer in e.Peers)
        {
            Console.WriteLine($"  {peer.ConnectionUri.Host}:{peer.ConnectionUri.Port}");
        }
    }

    public async Task StopAsync()
    {
        dhtEngine.PeersFound -= OnPeersFound;
        await dhtEngine.StopAsync();
        Console.WriteLine("Lobby stopped.");
    }
}