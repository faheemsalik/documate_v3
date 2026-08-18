using Documate.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Models
{
    public class S3FileModel
    {
        public string BucketName { get; set;}
        public MemoryStream FileMemoryStream { get; set; }
        public string FileName { get; set; }
        public int QueueId { get; set; }
        public string FilePath { get; set; } // path with in bucket filename
    }
    public class ContentType
    {
        public const string PDF = ".pdf";
        public const string JPG = ".jpg";        
        public const string PNG = ".png";         
    }

}
