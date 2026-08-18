using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{
    public class MasterKeywordSet : BaseEntity
    {
        [MaxLength(100)]
        public string Keyword { get; set; }
        [MaxLength(50)]
        public string SchemaId { get; set; }
        [MaxLength(30)]
        public string AwsBlock { get; set; } //word, line, field, cell
        [MaxLength(1000)]
        public string Description { get; set; }
                                           }
    [DebuggerDisplay("Synonym: {Synonym} - MasterKeyword id: {MasterKeywordId}")]
    public class KeywordSynonym : BaseEntity
    {
        [MaxLength(100)]
        public string Synonym { get; set; }
        public float weight { get; set; }

        [ForeignKey(nameof(MasterKeywordSetObj))]
        public int MasterKeywordId { get; set; }
        public virtual MasterKeywordSet MasterKeywordSetObj { get; set; }
    }

    [DebuggerDisplay("{Keyword} - {SchemaId} - {ValuePosition}")]
    public class TemplateKeyword : BaseEntity
    {
        [MaxLength(100)]
        public string Keyword { get; set; }
        [MaxLength(30)]
        public string SchemaId { get; set; }
        [MaxLength(30)]
        public string AwsBlock { get; set; } //word, line, field, cell
        [MaxLength(10)]
        public string ValuePosition { get; set; } // Where to look for the value when identifying element is found.
        public string Rule { get; set; } // Rule created as JSON.

        [ForeignKey(nameof(TemplateObj))]
        public int TemplateId { get; set; }
        public virtual Template TemplateObj { get; set; }
    }

    [DebuggerDisplay("{ElementName} - {Elementkey}")]
    public class IdentifyingElement : BaseEntity
    {
        [MaxLength(30)]
        public string ElementName { get; set; }
        [MaxLength(20)]
        public string Elementkey { get; set; }
    }
    [DebuggerDisplay("Val: {ValueStr}-ValueNum | Element id: {ElementId}")]
    public class KeywordElement : BaseEntity
    {
        [MaxLength(100)]
        public string ValueStr { get; set; }
        public double? ValueNum { get; set; }
        [MaxLength(20)]
        public string ComparisonType { get; set; } // equals, contains, is_any_number, greater_than, less_then
        public string LogicalOperator { get; set; } // AND , OR
        public float Weight { get; set; }

        [ForeignKey(nameof(ElementObj))]
        public int? ElementId { get; set; }
        public virtual IdentifyingElement ElementObj { get; set; }

        [ForeignKey(nameof(TemplateKeywordObj))]
        public int? TemplateKeywordId { get; set; }
        public virtual TemplateKeyword TemplateKeywordObj { get; set; }
    }


}
