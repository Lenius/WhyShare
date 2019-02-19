using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Web;
using DeviceId;
using Prism.Mvvm;
using WhyShare.Infrastructure.Interfaces;

namespace WhyShare.Infrastructure.Provider.Aws
{
    public class AwsProvider : BindableBase, IDisposable, IWhyShare
    {
        private BackgroundWorker bw;
        private string _fileName;
        private string _fileSize;
        private int _process;
        private string _status;
        private readonly AmazonS3Client _client;
        private CancellationTokenSource _tokenSource;
        private readonly string _prefix;

        public AwsProvider()
        {
            _tokenSource = new CancellationTokenSource();
            AwsBucket = ConfigurationManager.AppSettings["AwsBucket"];
            _client = new AmazonS3Client(Amazon.RegionEndpoint.EUCentral1);
            _prefix = new DeviceIdBuilder()
                .AddUserName()
                .AddMotherboardSerialNumber()
                .ToString();

            bw = new BackgroundWorker();
            bw.DoWork += Bw_DoWork;
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var file = new FileInfo(FileName);
            FileSize = file.Length.ToString();
            Status = "Uploading";

            var transferUtilityConfig = new TransferUtilityConfig
            {
                ConcurrentServiceRequests = 5,
                MinSizeBeforePartUpload = 20 * 1024,
            };

            using (var fileTransferUtility = new TransferUtility(_client))
            {
                fileTransferUtility.UploadAsync(file.FullName, AwsBucket);

                var uploadRequest =
                    new TransferUtilityUploadRequest
                    {
                        BucketName = AwsBucket,
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

        public string AwsBucket { get; set; }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                RaisePropertyChanged();
            }
        }

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

        public string GetUrl
        {
            get
            {
                var file = new FileInfo(FileName);

                GetPreSignedUrlRequest requestOrg = new GetPreSignedUrlRequest
                {
                    BucketName = AwsBucket,
                    Key = $"{_prefix}/{file.Name}",
                    Expires = DateTime.Now.AddMinutes(60*24)
                };

                return _client.GetPreSignedURL(requestOrg);
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
                    BucketName = AwsBucket,
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
                if (bw.IsBusy) return;

                _tokenSource = new CancellationTokenSource();
                bw.RunWorkerAsync();
            });
        }

        public void Dispose()
        {
            _tokenSource?.Cancel();
            _client?.Dispose();
            bw?.Dispose();
        }

        public void CancelUpload()
        {
            Status = "Canceling";
            _tokenSource.CancelAfter(TimeSpan.FromSeconds(1));
        }
    }
}
