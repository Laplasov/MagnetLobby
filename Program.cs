using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <MagnetGetInfo|BecomePeer>");
            Console.WriteLine("  MagnetGetInfo: Retrieve torrent metadata");
            Console.WriteLine("  BecomePeer: Join torrent as a peer");
            return;
        }

        string command = args[0].ToLower();
        switch (command)
        {
            case "magnetgetinfo":
                await MagnetGetInfo.MainGetInfo();
                break;
            case "becomepeer":
                await BecomePeer.MainBecomePeer();
                break;
            default:
                Console.WriteLine($"Invalid command: {command}");
                Console.WriteLine("Valid commands: MagnetGetInfo, BecomePeer");
                break;
        }
    }
}