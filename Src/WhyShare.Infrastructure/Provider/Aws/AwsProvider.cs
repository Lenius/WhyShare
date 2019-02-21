using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Windows.Threading;
using DeviceId;
using Prism.Mvvm;
using WhyShare.Infrastructure.Interfaces;

namespace WhyShare.Infrastructure.Provider.Aws
{
    public class AwsProvider : BindableBase, IDisposable, IWhyShare
    {
        private readonly BackgroundWorker _backgroundWorker;
        private readonly AmazonS3Client _client;
        private readonly string _prefix;
        private readonly string _awsbucket;

        private string _fileName;
        private string _fileSize;
        private string _shortUrl;
        private string _status;
        private int _process;

        private CancellationTokenSource _tokenSource;

        public AwsProvider()
        {
            _tokenSource = new CancellationTokenSource();
            _awsbucket = ConfigurationManager.AppSettings["AwsBucket"];
            _client = new AmazonS3Client(Amazon.RegionEndpoint.EUCentral1);
            _prefix = new DeviceIdBuilder()
                .AddUserName()
                .AddMotherboardSerialNumber()
                .ToString();

            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.DoWork += Bw_DoWork;
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var file = new FileInfo(FileName);
            FileSize = Tools.SizeToHuman(file.Length);
            Status = "Uploading";

            var transferUtilityConfig = new TransferUtilityConfig
            {
                ConcurrentServiceRequests = 5,
                MinSizeBeforePartUpload = 20 * 1024,
            };

            using (var fileTransferUtility = new TransferUtility(_client))
            {
                fileTransferUtility.UploadAsync(file.FullName, _awsbucket);

                var uploadRequest =
                    new TransferUtilityUploadRequest
                    {
                        BucketName = _awsbucket,
                        FilePath = file.FullName,
                        Key = $"{_prefix}/{file.Name}",
                        CannedACL = S3CannedACL.Private,
                        ContentType = MimeMapping.GetMimeMapping(file.Name),
                        StorageClass = S3StorageClass.Standard,
                        TagSet = new List<Tag>()
                        {
                            new Tag()
                            {
                                Key = "MachineName",
                                Value = Environment.MachineName
                            },
                            new Tag()
                            {
                                Key = "UserName",
                                Value = Environment.UserName
                            }
                        }
                    };

                uploadRequest.UploadProgressEvent += uploadRequest_UploadPartProgressEvent;

                fileTransferUtility.UploadAsync(uploadRequest, _tokenSource.Token);
            }

        }

        private void uploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
        {
            Process = e.PercentDone;

            if (_tokenSource.IsCancellationRequested)
            {
                Status = "Afbrudt";
            }
            else
            {
                Status = e.PercentDone == 100 ? "Uploaded" : "Uploading..";
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                RaisePropertyChanged();
            }
        }

        public IShortProvider ShortUrlProvider { get; set; }

        public int Process
        {
            get => _process;
            set { _process = value; RaisePropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; RaisePropertyChanged(); }
        }

        public string FileSize
        {
            get => _fileSize;
            set { _fileSize = value; RaisePropertyChanged(); }
        }

        public string Url
        {
            get
            {
                var file = new FileInfo(FileName);

                GetPreSignedUrlRequest requestOrg = new GetPreSignedUrlRequest
                {
                    BucketName = _awsbucket,
                    Key = $"{_prefix}/{file.Name}",
                    Expires = DateTime.Now.AddMinutes(60 * 24)
                };

                return _client.GetPreSignedURL(requestOrg);
            }
        }

        public string ShortUrl
        {
            get
            {
                if (_shortUrl == null)
                {
                    var file = new FileInfo(FileName);

                    GetPreSignedUrlRequest requestOrg = new GetPreSignedUrlRequest
                    {
                        BucketName = _awsbucket,
                        Key = $"{_prefix}/{file.Name}",
                        Expires = DateTime.Now.AddMinutes(60 * 24)
                    };

                    _shortUrl = ShortUrlProvider.Url(_client.GetPreSignedURL(requestOrg));
                }

                return _shortUrl;
            }
        }

        public bool Delete()
        {
            var file = new FileInfo(FileName);

            var result = false;

            try
            {
                var deleteResponse = _client.DeleteObject(new DeleteObjectRequest()
                {
                    BucketName = _awsbucket,
                    Key = $"{_prefix}/{file.Name}",
                });

                result = deleteResponse.HttpStatusCode == HttpStatusCode.NoContent;
            }
            catch (AmazonS3Exception s3Exception)
            {

            }

            return result;
        }

        public async Task Start()
        {
            await Task.Run(() =>
            {
                if (_backgroundWorker.IsBusy) return;

                _tokenSource = new CancellationTokenSource();
                _backgroundWorker.RunWorkerAsync();
            });
        }

        public void Dispose()
        {
            _tokenSource?.Cancel();
            _client?.Dispose();
            _backgroundWorker?.Dispose();
        }

        public void CancelUpload()
        {
            Status = "Canceling";
            _tokenSource.CancelAfter(TimeSpan.FromSeconds(1));
        }
    }
}
