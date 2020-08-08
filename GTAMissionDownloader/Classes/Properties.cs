using System;
using Microsoft.Win32;

namespace GTAMissionDownloader.Classes
{
    class Properties
    {
        public static string AppVersion { get; } = "1.3";
        public static string FolderId { get; } = "0B8j-xMQtDZvwVjN6R25sWF94dG8";

        //Dev
        public static string ProgramId { get; } = "1w4UUHr4MyMSXjpbz_bCYFsk9smQwSjxa";
        //Original
        //public static string ProgramId { get; } = "1EHQqd72EELxE-GXFCS4urWzn_3fL5wI2";

        public static string GetArma3FolderPath { get; } = @Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/../Local/Arma 3";
        public static string GetArma3MissionFolderPath { get; } = @Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/../Local/Arma 3/MPMissionsCache/";

        public static RegistryKey KeyStartUp { get; } = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    }
}
