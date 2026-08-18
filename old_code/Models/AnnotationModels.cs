using Amazon.Textract.Model;

using Documate.Domain;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Documate.Modelss
{
    //public class AnnotationModel
    //{
    //    public string DocUrl { get; set; }
    //    public GetDocumentAnalysisResponse Blocks { get; set; }
    //    public DocSchema_In Schema { get; set; }
    //    public DocSchema_Out annotationData { get; set; }
    //}

    //public class DocSchema_In
    //{
    //    public int id { get; set; }
    //    public string filename { get; set; }
    //    public List<UserSchemaSection> content { get; set; }
    //}
    //public class UserSchemaSection : AnnoSchemaSectionProps
    //{
    //    public List<UserSchemaChildNode> children { get; set; }
    //}
    //public class UserSchemaChildNode : UserSchemaDataPoint
    //{
    //    public List<UserSchemaChildNode> children { get; set; }
    //}
    //public class UserSchemaDataPoint: AnnoSchemaDataPoint
    //{
    //    public string label { get; set; }
    //    public int width { get; set; }
    //    public bool? hidden { get; set; }
    //    public DataPointConsrtaints constraints { get; set; }
    //    public List<string> rir_field_names { get; set; }
    //    public List<DataPointOption> options { get; set; }
    //}

    ////======================

    //public class AnnoSchema
    //{
    //    public AnnoSchema()
    //    {
    //        content = new List<AnnoSchemaSection>();
    //    }
    //    public List<AnnoSchemaSection> content { get; set; }
    //}
    //public class AnnoSchemaSection : AnnoSchemaSectionProps
    //{
    //    public List<AnnoSchemaChildNode> children { get; set; }
    //}
    //public class AnnoSchemaChildNode : AnnoSchemaDataPoint
    //{
    //    public List<AnnoSchemaChildNode> children { get; set; }
    //}
    //public class AnnoSchemaDataPoint
    //{
    //    public string id { get; set; }
    //    public string category { get; set; }
    //    public string schema_id { get; set; }
    //    public double confidence { get; set; }
    //    public string value { get; set; }
    //    public string type { get; set; }
    //    public string format { get; set; }
    //    public DataPointContent content { get; set; }
    //}

    //public class AnnoSchemaSectionProps
    //{
    //    public string id { get; set; }
    //    public string category { get; set; }
    //    public string schema_id { get; set; }
    //    public string label { get; set; }
    //    public bool? hidden { get; set; }
    //    public string type { get; set; }
    //    public string format { get; set; }
    //}
    
    ////=====================

    //public class DataPointContent
    //{
    //    public string value { get; set; }
    //    public double? confidence { get; set; }
    //    public string validation_source { get; set; }
        
    //}
    //public class DataPointConsrtaints
    //{
    //    public bool? required { get; set; }
    //}
    //public class DataPointOption
    //{
    //    public string label { get; set; }
    //    public string value { get; set; }
    //}

    //=====================

    public class AnnotationFileURLModel
    {
        public string SignedURL { get; set; }
        public string MimeType { get; set; }
    }

}
