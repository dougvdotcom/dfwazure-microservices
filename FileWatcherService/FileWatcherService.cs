using System.ServiceProcess;

namespace FileWatcherService
{
    public partial class FileWatcherService : ServiceBase
    {
        public FileWatcherService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var watcher = new FtpFileWatcher();
        }

        protected override void OnStop()
        {
        }
    }
}
