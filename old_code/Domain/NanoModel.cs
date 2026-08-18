using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Documate.Domain
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NanoModel : BaseEntity
    {
        [MaxLength(128)]
        public string NanoModelId { get; set; }
        [MaxLength(32)]
        public string ModelKey { get; set; }
        [MaxLength(512)]
        public string Description { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
    }
}
