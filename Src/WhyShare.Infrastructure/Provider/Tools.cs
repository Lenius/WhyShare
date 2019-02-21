using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WhyShare.Infrastructure.Provider
{
    static class Tools
    {
        public static string FileToSize(string filepath)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = new FileInfo(filepath).Length;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public static string SizeToHuman(long filesize)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (filesize >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                filesize = filesize / 1024;
            }

            return $"{filesize:0.##} {sizes[order]}";
        }

        public static void UiInvoke(Action a)
        {
            //Application.Current.Dispatcher.Invoke(a);
        }
    }
}
