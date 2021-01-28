using System.Threading;
using Caliburn.Micro;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace GTAMissionDownloader.Classes
{
    class Helper
    {
        public static CancellationTokenSource CtsOnStart { get; set; } = new CancellationTokenSource();
        public static CancellationTokenSource CtsStopDownloading { get; set; } = new CancellationTokenSource();

        public static TaskbarIcon MyNotifyIcon { get; set; } = new TaskbarIcon();

        public static DriveService Service { get; set; } = new DriveService(new BaseClientService.Initializer()
        {
            ApiKey = "YourApiKey"
        });

        public static FilesResource.GetRequest GetFileRequest(string fileId, string field)
        {
            var request = Service.Files.Get(fileId);
            request.Fields = field;

            return request;
        }

        public static IWindowManager Manager { get; set; } = new WindowManager();
    }
}
