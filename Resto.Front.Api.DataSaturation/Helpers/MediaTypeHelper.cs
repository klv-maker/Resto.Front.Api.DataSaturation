using Resto.Front.Api.DataSaturation.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public static class MediaTypeHelper
    {
        public static MediaType GetMediaType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                    return MediaType.Image;
                case ".gif":
                    return MediaType.Gif;
                case ".mp4":
                case ".avi":
                case ".mov":
                case ".mkv":
                case ".wmv":
                    return MediaType.Video;
                default:
                    return MediaType.Unknown;
            }
            ;
        }
    }
}
