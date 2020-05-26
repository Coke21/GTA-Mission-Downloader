using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace GTAMissionDownloader.Classes
{
    class Properties
    {
        public static string AppVersion { get; } = "1.3";
        public static string FolderId { get; } = "0B8j-xMQtDZvwVjN6R25sWF94dG8";
        public static string ProgramId { get; } = "1EHQqd72EELxE-GXFCS4urWzn_3fL5wI2";

        public static string GetArma3FolderPath { get; } = @Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/../Local/Arma 3";
        public static string GetArma3MissionFolderPath { get; } = @Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/../Local/Arma 3/MPMissionsCache/";
        public static string GetProgramFolderPath { get; } = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
        public static string GetProgramName { get; } = AppDomain.CurrentDomain.FriendlyName + ".exe";

        public static RegistryKey KeyStartUp { get; } = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    }
}
