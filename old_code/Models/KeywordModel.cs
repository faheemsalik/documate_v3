

using System.Diagnostics;

namespace Documate.Models
{
    [DebuggerDisplay("{Keyword}- {Synonym} - {SchemaId} - {AwsBlock} ")]
    public class KeywordSynomModel
    {
        public int Id { get; set; }
        public string Keyword { get; set; }
        public string SchemaId { get; set; }
        public string AwsBlock { get; set; } //word, line, field, cell
        public string Synonym { get; set; }
        public float weight { get; set; }
        public int MasterKeywordId { get; set; }
        public string ValuePosition { get; set; }
    }

    [DebuggerDisplay("{Keyword}- {ElementName} - {SchemaId} - {AwsBlock} - {ValueStr}")]
    public class TemplateKwElementModel
    {
        public int ElementId { get; set; }
        public int kwElementId { get; set; }
        public string ElementName { get; set; }
        public string ElementKey { get; set; }
        public string Keyword { get; set; }
        public int TemplateKeywordId { get; set; }
        public double? ValueNum { get; set; }
        public string ValueStr { get; set; }
        public string SchemaId { get; set; }
        public string AwsBlock { get; set; }
        public string ComparisonType { get; set; }
    }

    public enum IdenifyingElements
    { 
        width,
        HEIGHT,
        VALUE_POSITION,
        PREV_BLOCK_VALUE,
        NEXT_BLOCK_VALUE
    }

    public enum GetBlockValue
    {
        PREV,
        NEXT,
        BELOW,
        ABOVE
    }
}
