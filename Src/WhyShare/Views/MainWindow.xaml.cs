using System.IO;
using System.Windows;
using WhyShare.Infrastructure.Provider.Aws;
using WhyShare.ViewModels;

namespace WhyShare.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var viewModel = DataContext as MainWindowViewModel;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var ext = info.Extension.ToLower();
                //viewModel?.Add(new AwsProvider() { FileName = file });
                if (ext.Equals(".bmp") ||
                    ext.Equals(".gif") ||
                    ext.Equals(".jpg") ||
                    ext.Equals(".jpeg") ||
                    ext.Equals(".zip") ||
                    ext.Equals(".rar") ||
                    ext.Equals(".pdf") ||
                    ext.Equals(".png"))
                {
                    viewModel?.Add(new AwsProvider() { FileName = file });
                }
            }
        }
    }
}
