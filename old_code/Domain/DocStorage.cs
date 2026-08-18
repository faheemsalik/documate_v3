using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{

    public class DocStorage : BaseEntity
    {
        [MaxLength(128)]
        public string BucketName { get; set; }
        [MaxLength(128)]
        public string FolderName { get; set; }
        [MaxLength(32)]
        public string Region { get; set; }
        [MaxLength(32)]
        public string StorageKey { get; set; }
        [MaxLength(512)]
        public string Description { get; set; }
    }
}
