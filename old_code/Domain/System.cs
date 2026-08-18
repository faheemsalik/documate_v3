using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Domain
{
    //public class SysDocType : BaseEntity
    //{
    //    [MaxLength(20)]
    //    public string DocType { get; set; }
    //    [MaxLength(20)]
    //    public string DocTypeKey { get; set; }
    //    public string SchemaJSON { get; set; }
    //    [MaxLength(10)]
    //    public string Locale { get; set; }
    //}

    //public class SchemaSection : BaseEntity
    //{
    //    [MaxLength(100)]
    //    public string SectionName { get; set; }
    //    [MaxLength(30)]
    //    public string SchemaId { get; set; }
    //    public double? Confidence { get; set; }
    //    public int? DisplayOrder { get; set; }

    //    [ForeignKey(nameof(SchemaSectionObj))]
    //    public int? ParentId { get; set; }
    //    public virtual SchemaSection SchemaSectionObj { get; set; }

    //    [ForeignKey(nameof(sysDocTypeObj))]
    //    public int DocTypeId { get; set; }
    //    public virtual SysDocType sysDocTypeObj { get; set; }

    //}

    //public class SchemaDataPoint : BaseEntity
    //{
    //    [MaxLength(100)]
    //    public string Label { get; set; }
    //    [MaxLength(30)]
    //    public string SchemaId { get; set; }
    //    public double? Confidence { get; set; }
    //    public int? DisplayOrder { get; set; }

    //    [ForeignKey(nameof(SchemaSectionObj))]
    //    public int SectionId { get; set; }
    //    public virtual SchemaSection SchemaSectionObj { get; set; }


    //}


    public class SysSetting : BaseEntity
    {
        [MaxLength(100)]
        public string key { get; set; }
        [MaxLength(100)]
        public string Value { get; set; }
    }

    //public class SchemaNodeCategory : BaseEntity
    //{
    //    [MaxLength(20)]
    //    public string Status { get; set; }
    //    [MaxLength(20)]
    //    public string Category { get; set; }
    //}

}
