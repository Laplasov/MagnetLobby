using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class MagnetGetInfo
{
    public static async Task MainGetInfo()
    {
        try
        {
            // Define the magnet link
            string magnetLink = "magnet:?xt=urn:btih:232b369a657f606cf16ed2c8cc11f8079910f9ad&dn=TestApp.txt";

            // Define a directory to save the metadata (torrent file)
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "torrents");
            Directory.CreateDirectory(savePath);

            // Configure engine settings
            var settings = new EngineSettingsBuilder
            {
                CacheDirectory = savePath,
                AllowPortForwarding = true // Optional: for better peer connectivity
            }.ToSettings();

            // Initialize the torrent engine
            using var engine = new ClientEngine(settings);

            // Parse the magnet link
            MagnetLink link = MagnetLink.Parse(magnetLink);

            // Create torrent manager to handle the magnet link
            var manager = await engine.AddAsync(link, savePath);

            // Start the manager to begin fetching metadata
            await manager.StartAsync();

            // Wait for metadata with a timeout
            Console.WriteLine("Waiting for metadata...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30-second timeout
            try
            {
                await manager.WaitForMetadataAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Metadata retrieval timed out after 30 seconds.");
                Console.WriteLine($"Current connected peers: {manager.OpenConnections}");
                await manager.StopAsync();
                return;
            }

            // Check if metadata is available
            if (manager.HasMetadata)
            {
                // Access the torrent metadata
                var torrent = manager.Torrent;
                Console.WriteLine("Metadata retrieved successfully!");
                Console.WriteLine($"Torrent Name: {manager.Name}");
                Console.WriteLine($"Info Hash: {manager.InfoHashes.V1?.ToHex() ?? "Unknown"}");
                Console.WriteLine($"Current connected peers: {manager.OpenConnections}");

                if (torrent != null)
                {
                    Console.WriteLine($"Size: {torrent.Size} bytes");
                    Console.WriteLine($"Created By: {torrent.CreatedBy ?? "Unknown"}");
                    Console.WriteLine($"Comment: {torrent.Comment ?? "None"}");
                    Console.WriteLine("Announce URLs:");
                    foreach (var announce in torrent.AnnounceUrls)
                    {
                        foreach (var url in announce)
                        {
                            Console.WriteLine($"  {url}");
                        }
                    }
                    Console.WriteLine("Files:");
                    foreach (var file in torrent.Files)
                    {
                        Console.WriteLine($"  {file.Path} ({file.Length} bytes)");
                    }
                }
                else
                {
                    Console.WriteLine("Warning: Torrent metadata is null, limited information available.");
                }
            }
            else
            {
                Console.WriteLine("Failed to retrieve metadata.");
                Console.WriteLine($"Current connected peers: {manager.OpenConnections}");
            }

            // Stop the manager
            await manager.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}