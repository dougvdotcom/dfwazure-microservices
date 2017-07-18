using System.ComponentModel;

namespace FileWatcherService
{
    partial class FileWatcherService
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            this.ServiceName = "FileWatcherService";
        }
    }
}
