using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WhyShare.Infrastructure.Interfaces
{
    public interface IWhyShare
    {
        int Process { get; set; }
        string Status { get; set; }
        string FileName { get; set; }
        string GetUrl { get; }
        bool Delete();
        Task Start();
        void Dispose();
        void CancelUpload();
    }
}
