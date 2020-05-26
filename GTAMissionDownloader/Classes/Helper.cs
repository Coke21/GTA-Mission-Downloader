using System.Threading;
using Caliburn.Micro;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace GTAMissionDownloader.Classes
{
    class Helper
    {
        public static CancellationTokenSource CtsOnStart = new CancellationTokenSource();
        public static CancellationTokenSource CtsStopDownloading = new CancellationTokenSource();

        public static TaskbarIcon MyNotifyIcon = new TaskbarIcon();

        public static DriveService Service = new DriveService(new BaseClientService.Initializer()
        {
            ApiKey = "AIzaSyB8KixGHl2SPwQ5HJixKGm7IGbOYbpuc1w"
        });

        public static FilesResource.GetRequest GetFileRequest(string fileId, string field)
        {
            var request = Service.Files.Get(fileId);
            request.Fields = field;

            return request;
        }

        public static IWindowManager Manager = new WindowManager();
    }
}
