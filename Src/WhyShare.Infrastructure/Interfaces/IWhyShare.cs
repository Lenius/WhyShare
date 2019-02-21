using System.Threading.Tasks;

namespace WhyShare.Infrastructure.Interfaces
{
    public interface IWhyShare
    {
        int Process { get; set; }
        string Status { get; set; }
        string FileName { get; set; }
        IShortProvider ShortUrlProvider { get; set; }
        string Url { get; }
        string ShortUrl { get; }
        bool Delete();
        Task Start();
        void Dispose();
        void CancelUpload();
    }
}
