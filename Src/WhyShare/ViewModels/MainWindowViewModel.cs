using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using WhyShare.Infrastructure.Interfaces;
using WhyShare.Infrastructure.Provider.Aws;

namespace WhyShare.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private string _title = "WhyShare";

        public DelegateCommand<IWhyShare> OpenUrlCommand { get; set; }
        public DelegateCommand<IWhyShare> CancelUploadCommand { get; set; }
        public DelegateCommand<IWhyShare> DeleteObjectCommand { get; set; }

        public DelegateCommand<Window> ExitCommand { get; set; }

        public ObservableCollection<IWhyShare> S3Objects { get; set; }

        private IWhyShare _selectedItem;
        private int _selectedItemChanged;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public MainWindowViewModel()
        {
            S3Objects = new ObservableCollection<IWhyShare>();
            S3Objects.CollectionChanged += S3Objects_CollectionChanged;

            OpenUrlCommand = new DelegateCommand<IWhyShare>(OpenUrl, CanOpenUrl);
            CancelUploadCommand = new DelegateCommand<IWhyShare>(CancelUpload, CanCancelUpload);
            DeleteObjectCommand = new DelegateCommand<IWhyShare>(DeleteUpload, CanDeleteUpload);
            ExitCommand = new DelegateCommand<Window>(ExitApp, CanExitApp);
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
            bool result = false;

            if (arg != null)
            {
                result = arg.Process == 100;
            }

            return result;
        }

        private bool CanCancelUpload(IWhyShare arg)
        {
            bool result = false;

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
            var url = obj.GetUrl;
            System.Diagnostics.Process.Start(url);
        }



        private async void S3Objects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //add new items
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

        private bool CanExitApp(Window w)
        {
            return true;
        }

        private void ExitApp(Window w)
        {
            w?.Close();
        }

        public void Add(IWhyShare awsProvider)
        {
            S3Objects.Add(awsProvider);
        }

        public IWhyShare SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SelectedItemChanged = SelectedItemChanged + 1;

                SetProperty(ref _selectedItem, value);

            }
        }

        public int SelectedItemChanged
        {
            get { return _selectedItemChanged; }
            set
            {
                SetProperty(ref _selectedItemChanged, value);
            }
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
