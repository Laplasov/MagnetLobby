using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class BecomePeer
{
    public static async Task MainBecomePeer()
    {
        try
        {
            // Define the magnet link
            string magnetLink = "magnet:?xt=urn:btih:232b369a657f606cf16ed2c8cc11f8079910f9ad&dn=TestApp.txt";

            // Define a directory for cache
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "torrents");
            Directory.CreateDirectory(savePath);

            // Configure engine settings
            var settings = new EngineSettingsBuilder
            {
                CacheDirectory = savePath,
                AllowPortForwarding = true,
                AllowLocalPeerDiscovery = true // Enable local peer discovery
            }.ToSettings();

            // Initialize the torrent engine
            using var engine = new ClientEngine(settings);

            // Log Peer ID and Listening IP/Port
            Console.WriteLine($"Client Peer ID: {engine.PeerId?.Text}");

            // Parse the magnet link
            MagnetLink link = MagnetLink.Parse(magnetLink);

            // Create torrent manager
            var manager = await engine.AddAsync(link, savePath);

            // Log peer connections
            manager.PeerConnected += (s, e) =>
            {
                Console.WriteLine($"Peer connected: {e.Peer.Uri} (Connected peers: {manager.OpenConnections})");
            };
            manager.PeerDisconnected += (s, e) =>
            {
                Console.WriteLine($"Peer disconnected: {e.Peer.Uri} (Connected peers: {manager.OpenConnections})");
            };

            // Start the manager in metadata-only mode
            Console.WriteLine("Joining torrent as a peer...");
            await manager.StartAsync(); // true for metadata-only mode

            // Wait for metadata (with timeout)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await manager.WaitForMetadataAsync(cts.Token);
                Console.WriteLine($"Metadata retrieved. Acting as peer for: {manager.Name}");
                Console.WriteLine($"Info Hash: {manager.InfoHashes.V1?.ToHex() ?? "Unknown"}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Metadata retrieval timed out, but continuing as peer.");
            }
            // Announce to trackers and DHT
            using var shutdownCts = new CancellationTokenSource();
            await manager.TrackerManager.AnnounceAsync(shutdownCts.Token);
            if (manager.CanUseDht)
            {
                await manager.DhtAnnounceAsync();
                Console.WriteLine("Announced to DHT.");
            }

            // Keep running until interrupted
            Console.WriteLine("Running as peer. Press Ctrl+C to stop.");
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                shutdownCts.Cancel();
            };

            try
            {
                await Task.Delay(Timeout.Infinite, shutdownCts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutting down...");
            }

            // Stop the manager
            await manager.StopAsync();
            Console.WriteLine($"Final connected peers: {manager.OpenConnections}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}