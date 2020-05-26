using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AngleSharp;
using Caliburn.Micro;
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
        //private static string[] ServerIps { get; } = { "164.132.200.53:2302", "164.132.202.63:2602", "164.132.202.63:2302", "ts3server://TS.grandtheftarma.com:9987" };
        public static void Server(string server)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = server switch
                    {
                        "Ts" => $"{_mvm.TsAddressText}" + $"?channel={_tvm.TsChannelNameText}" + $"&channelpassword={_tvm.TsChannelPasswordText}",
                        _ => GetRegistryArma3Path
                    },
                    Arguments = server switch
                    {
                        "S1" => $"-connect={_mvm.S1AddressText}:{Convert.ToUInt16(_mvm.S1PortText)}",
                        "S2" => $"-connect={_mvm.S2AddressText}:{Convert.ToUInt16(_mvm.S2PortText)}",
                        "S3" => $"-connect={_mvm.S3AddressText}:{Convert.ToUInt16(_mvm.S3PortText)}",
                        _ => string.Empty
                    },
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
                    try
                    {
                        int port1 = Convert.ToInt32(_mvm.S1PortText) + 1;
                        int port2 = Convert.ToInt32(_mvm.S2PortText) + 1;
                        int port3 = Convert.ToInt32(_mvm.S3PortText) + 1;

                        Server gta1 = ServerQuery.GetServerInstance(Game.Arma_3, _mvm.S1AddressText, (ushort)port1, false, 1000, 1000, 0);
                        Server gta2 = ServerQuery.GetServerInstance(Game.Arma_3, _mvm.S2AddressText, (ushort)port2, false, 1000, 1000, 0);
                        Server gta3 = ServerQuery.GetServerInstance(Game.Arma_3, _mvm.S3AddressText, (ushort)port3, false, 1000, 1000, 0);

                        ServerInfo info1 = gta1.GetInfo();
                        ServerInfo info2 = gta2.GetInfo();
                        ServerInfo info3 = gta3.GetInfo();

                        await ShowServerInfo(info1, "Server1Info", "IsJoinServer1Enabled");
                        await ShowServerInfo(info2, "Server2Info", "IsJoinServer2Enabled");
                        await ShowServerInfo(info3, "Server3Info", "IsJoinServer3Enabled");
                        await ShowTsInfo("TsInfo", "IsJoinTsEnabled");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Exception raised: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    await Task.Delay(15_000);
                }
            });
        }

        private static Task ShowServerInfo(ServerInfo info, string serverPropertyName, string buttonPropertyName)
        {
            var server = _mvm.GetType().GetProperty(serverPropertyName);
            var button = _mvm.GetType().GetProperty(buttonPropertyName);

            if (info != null)
            {
                if (info.Players + 1 != info.MaxPlayers)
                {
                    server.SetValue(_mvm, $"{info.Name} | Players: {info.Players + 1}/{info.MaxPlayers}");
                    button.SetValue(_mvm, true);
                }
                else
                {
                    server.SetValue(_mvm, "Server full");
                    button.SetValue(_mvm, false);
                }
            }
            else
            {
                server.SetValue(_mvm, "Server offline");
                button.SetValue(_mvm, false);
            }

            return Task.CompletedTask;
        }

        private static async Task ShowTsInfo(string tsPropertyName, string tsButtonPropertyName)
        {
            var tsInfo = _mvm.GetType().GetProperty(tsPropertyName);
            var tsButton = _mvm.GetType().GetProperty(tsButtonPropertyName);

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync("https://grandtheftarma.com/");
            var cellSelector = _mvm.TsSelectorText;
            var cells = document.QuerySelectorAll(cellSelector);
            var stuff = cells.Select(m => m.TextContent);

            foreach (var description in stuff)
                if (description != "Offline")
                {
                    tsInfo.SetValue(_mvm, $"TeamSpeak: {description}");
                    tsButton.SetValue(_mvm, true);
                }
                else
                {
                    tsInfo.SetValue(_mvm, "TeamSpeak is offline");
                    tsButton.SetValue(_mvm, false);
                }
        }
    }
}
