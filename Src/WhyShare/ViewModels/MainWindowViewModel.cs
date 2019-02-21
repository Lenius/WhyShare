using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Unity;
using WhyShare.Infrastructure.Interfaces;
using WhyShare.Infrastructure.Provider.Aws;

namespace WhyShare.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private string _title = "WhyShare";

        public DelegateCommand<IWhyShare> OpenUrlCommand { get; set; }
        public DelegateCommand<IWhyShare> OpenShortUrlCommand { get; set; }
        public DelegateCommand<IWhyShare> CancelUploadCommand { get; set; }
        public DelegateCommand<IWhyShare> DeleteObjectCommand { get; set; }
        public DelegateCommand<Window> ExitCommand { get; set; }

        public ObservableCollection<IWhyShare> S3Objects { get; set; }

        private IWhyShare _selectedItem;
        private int _selectedItemChanged;
        private IShortProvider _shortProvider;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public MainWindowViewModel(IUnityContainer container)
        {

            _shortProvider = container.Resolve<IShortProvider>();


            OpenUrlCommand = new DelegateCommand<IWhyShare>(OpenUrl, CanOpenUrl);
            OpenShortUrlCommand = new DelegateCommand<IWhyShare>(OpenShortUrl, CanOpenShortUrl);
            CancelUploadCommand = new DelegateCommand<IWhyShare>(CancelUpload, CanCancelUpload);
            DeleteObjectCommand = new DelegateCommand<IWhyShare>(DeleteUpload, CanDeleteUpload);
            ExitCommand = new DelegateCommand<Window>(ExitApp, CanExitApp);

            S3Objects = new ObservableCollection<IWhyShare>();
            S3Objects.CollectionChanged += S3Objects_CollectionChanged;
        }

        private bool CanOpenShortUrl(IWhyShare arg)
        {
            var result = false;

            if (arg != null)
            {
                result = arg.Process == 100;
            }

            return result;
        }

        private void OpenShortUrl(IWhyShare obj)
        {
            var url = obj.ShortUrl;
            try
            {
                Clipboard.SetText(url);
                MessageBox.Show("Link er kopieret til udklipsholder");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Der er sket en fejl...");
            }
        }

        private void DeleteUpload(IWhyShare obj)
        {
            if (obj.Delete())
            {
                S3Objects.Remove(obj);
            }
        }

        private bool CanDeleteUpload(IWhyShare arg)
        {
            var result = false;

            if (arg != null)
            {
                result = arg.Process == 100;
            }

            return result;
        }

        private bool CanCancelUpload(IWhyShare arg)
        {
            var result = false;

            if (arg != null)
            {
                result = arg.Process < 100;
            }

            return result;
        }

        private void CancelUpload(IWhyShare obj)
        {
            obj.CancelUpload();
        }

        private bool CanOpenUrl(IWhyShare arg)
        {
            return SelectedItem != null;
        }

        private void OpenUrl(IWhyShare obj)
        {
            var url = obj.Url;
            System.Diagnostics.Process.Start(url);
        }

        private bool CanExitApp(Window w)
        {
            return true;
        }

        private void ExitApp(Window w)
        {
            w?.Close();
        }

        private async void S3Objects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // add new items
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (IWhyShare newItem in e.NewItems)
                {
                    await newItem.Start();
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (IWhyShare oldItem in e.OldItems)
                {
                    oldItem.Dispose();
                }
            }
        }

        public void Add(string file)
        {
            S3Objects.Add(new AwsProvider() { FileName = file, ShortUrlProvider = _shortProvider });
        }

        public IWhyShare SelectedItem
        {
            get => _selectedItem;
            set
            {
                SelectedItemChanged = SelectedItemChanged + 1;

                SetProperty(ref _selectedItem, value);

            }
        }

        public int SelectedItemChanged
        {
            get => _selectedItemChanged;
            set => SetProperty(ref _selectedItemChanged, value);
        }

        public void Dispose()
        {
            foreach (var viewModelS3Object in S3Objects)
            {
                viewModelS3Object.Dispose();
            }
        }
    }
}
