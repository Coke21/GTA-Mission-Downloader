using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AngleSharp;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.ViewModels;
using Microsoft.Win32;
using QueryMaster;
using QueryMaster.GameServer;

namespace GTAMissionDownloader.Classes
{
    class Join
    {
        private static MainViewModel _mvm;
        private static TsViewModel _tvm;
        public Join(MainViewModel mvm, TsViewModel tvm)
        {
            _mvm = mvm;
            _tvm = tvm;

            _ = UpdateServerAsync();
        }

        private static string GetRegistryArma3Path { get; } = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\bohemia interactive\arma 3", "main", string.Empty) + @"\arma3battleye";
        public static void Server(ServersModel serverModel)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = string.IsNullOrWhiteSpace(serverModel.TsSelector) ? GetRegistryArma3Path : $"ts3server://{serverModel.ServerIp}?channel={_tvm.TsChannelNameText}" + $"&channelpassword={_tvm.TsChannelPasswordText}",
                    Arguments = string.IsNullOrWhiteSpace(serverModel.TsSelector) ? $"-connect={serverModel.ServerIp}:{Convert.ToUInt16(serverModel.ServerQueryPort)}" : string.Empty,
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception raised: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task UpdateServerAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    foreach (var serverModel in _mvm.Servers.ToList())
                    {
                        try
                        {
                            if (serverModel.ContentButton == "Join TeamSpeak")
                            {
                                var document = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(serverModel.TsSelectorUrl);
                                var cells = document.QuerySelectorAll(string.IsNullOrWhiteSpace(serverModel.TsSelector) ? "null" : serverModel.TsSelector);
                                var stuff = cells.Select(m => m.TextContent).ToList();

                                if (!stuff.Any())
                                {
                                    serverModel.ServerInfo = string.IsNullOrWhiteSpace(serverModel.TsSelector) ? "TS Selector Not Provided" : "Invalid Selector";
                                    serverModel.IsJoinButtonEnabled = true;
                                    continue;
                                }

                                foreach (var description in stuff)
                                {
                                    if (description != "Offline")
                                    {
                                        serverModel.ServerInfo = $"TeamSpeak: {description}";
                                        serverModel.IsJoinButtonEnabled = true;
                                    }
                                    else
                                    {
                                        serverModel.ServerInfo = "TeamSpeak is offline";
                                        serverModel.IsJoinButtonEnabled = false;
                                    }
                                }
                            }
                            else
                            {
                                if (!int.TryParse(serverModel.ServerQueryPort, out int port))
                                    continue;

                                Server serverInstance = ServerQuery.GetServerInstance(Game.Arma_3, serverModel.ServerIp, ushort.Parse((port + 1).ToString()), false, 1000, 1000, 0);
                                var info = serverInstance.GetInfo();

                                if (info != null)
                                {
                                    if (info.Players != info.MaxPlayers)
                                    {
                                        serverModel.ServerInfo = $"{info.Name} | Players: {info.Players}/{info.MaxPlayers}";
                                        serverModel.IsJoinButtonEnabled = true;
                                    }
                                    else
                                    {
                                        serverModel.ServerInfo = $"{info.Name} is Full";
                                        serverModel.IsJoinButtonEnabled = false;
                                    }
                                }
                                else
                                {
                                    serverModel.ServerInfo = $"Server \"{serverModel.ServerIp}:{serverModel.ServerQueryPort}\" is Offline";
                                    serverModel.IsJoinButtonEnabled = false;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"Exception raised: {e.Message}\n->{serverModel.ServerIp}:{serverModel.ServerQueryPort} is wrong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    await Task.Delay(5_000);
                }
            });
        }
    }
}
