using Amazon.Textract.Model;
using Documate.Domain;
using System.Collections.Generic;

namespace Documate.Models
{
    public class QueueUpdatePublic
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<DocSchemaSection_In> content { get; set; }
        public string metadata { get; set; }
    }

    public class AnnotationModel
    {
        public string DocUrl { get; set; }
        public string MimeType { get; set; }
        public GetDocumentAnalysisResponse Blocks { get; set; }
        public DocSchema_In Schema { get; set; }
        public DocSchema_Out annotationData { get; set; }
        public Template template { get; set; }
    }

    public class QueueAppVM
    {
        public int Id { get; set; }
        public string QueueName { get; set; }
        public string Description { get; set; }
        public bool FlgActive { get; set; }
        public string WebhookURL { get; set; }
        public int AutomationLevel { get; set; } // 0=Never, 1= Confidence_Level, 3= Always
        public float ConfidenceScoreThresold { get; set; }
        public int DocTypeId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
    }
    #region Schema
    public class DocSchema_In
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<DocSchemaSection_In> content { get; set; }
    }
    public class DocSchemaSection_In : DocSchemaDataPoint_In
    {
        public List<ChildNode_In> children { get; set; }
    }
    public class ChildNode_In : DocSchemaDataPoint_In
    {
        public List<ChildNode_In> children { get; set; }
    }
    public class DocSchemaDataPoint_In
    {
        public string id { get; set; }
        public string category { get; set; }
        public string schema_id { get; set; }
        public string nano_label { get; set; }
        public string label { get; set; }
        public int? width { get; set; }
        public bool? hidden { get; set; }
        public string type { get; set; }
        public string format { get; set; }
        public DataPointContent content { get; set; }
        public DataPointConsrtaints constraints { get; set; }
        public double score_threshold { get; set; }
        public List<string> rir_field_names { get; set; }
        public List<DataPointOption> options { get; set; }
    }

    public class DocSchemaSectionProps_In
    {
        public string id { get; set; }
        public string category { get; set; }
        public string schema_id { get; set; }
        public string label { get; set; }
        public bool? hidden { get; set; }
    }

    //======================

    public class DocSchema_Out
    {
        public DocSchema_Out()
        {
            content = new List<DocSchemaSection_Out>();
        }
        public List<DocSchemaSection_Out> content { get; set; }
        public int doc_id { get; set; } // Template id
        public int user_meta_data { get; set; }
    }
    public class DocSchemaSection_Out : DocSchemaSectionProps_Out
    {
        public List<ChildNode_Out> children { get; set; }
    }
    public class ChildNode_Out : DocSchemaDataPoint_Out
    {
        public List<ChildNode_Out> children { get; set; }
    }
    public class DocSchemaDataPoint_Out
    {
        public string id { get; set; }
        public string block_id { get; set; }
        public string category { get; set; }
        public string schema_id { get; set; }
        public double confidence { get; set; }
        public string value { get; set; }
        public string type { get; set; }
        public string format { get; set; }
        public DataPointContent content { get; set; }
    }

    public class DocSchemaSectionProps_Out
    {
        public string category { get; set; }
        public string schema_id { get; set; }
    }

    //=====================

    public class DataPointContent
    {
        public string value { get; set; }
        public double? confidence { get; set; }
        public string validation_source { get; set; }
    }
    public class DataPointConsrtaints
    {
        public bool? required { get; set; }
        public string regexp { get; set; }
        public DataPointContraintLength length { get; set; }
    }
    public class DataPointContraintLength
    {
        public int? min { get; set; }
        public int? max { get; set; }
        public int? exact { get; set; }
    }

    public class DataPointOption
    {
        public string value { get; set; }
        public string label { get; set; }

    }
    #endregion

    #region Generative_Schema

    public class DocSchema
    {
        public int doc_id { get; set; }
        public string version { get; set; }
        public List<SchemaContent> content { get; set; }
        public string user_meta_data { get; set; }

        public class SchemaContent : DataPoint
        {
            public List<Table> tables { get; set; } // Used for tables category
        }

        public class Table
        {
            public string category { get; set; } = "table";
            public List<Row> rows { get; set; }
        }
        public class Row
        {
            public string category { get; set; } = "row";
            public List<DataPoint> cells { get; set; }
        }

        public class DataPoint
        {
            public string category { get; set; }
            public string schema_id { get; set; }
            public string type { get; set; }
            public string value { get; set; }
        }
    }

    public class DocumateMetaData
    {
        public int doc_id { get; set; }
        public string version { get; set; }
        public string user_meta_data { get; set; }
        public dynamic content { get; set; }
    }
    #endregion
}

#region
/*
 {
    "content": [
        {
            "category": "data_point",
            "schema_id": "seller_name",
            "type": "string",
            "format": ""
            "value": ""
        },
        {
            "category": "data_point",
            "schema_id": "delivery_date",
            "type": "string",
            "format": "dd/mm/yyyy"
            "value": ""
        },
        {
            "category": "data_point",
            "schema_id": "delivery_no",
            "type": "string",
            "format": ""
            "value": ""
        },
        {
            "caegory": "data_point",
            "schema_id": "po_no",
            "type": "string",
            "format": ""
            "value": ""
        },
        {
            "caegory": "tables",
            "schema_id": "na",
            "type": "",
            "format": ""
            "value": ""
        }
    ]
}


=====

 {
     "Version": "2025-02-02",
     "Name": "MCM DN Schema",
     "Description": "MCM DN Schema",
     "UserMetaData": "project id etc.",
     "Content": [
         {
             "Caegory": "data_point",
             "SchemaId": "seller_name",
             "Label": "Seller Name",
             "Type": "string",
             "Format": "",
             "IsRequired": "false",
             "InstructionsForAi": ""
         },
         {
             "Caegory": "data_point",
             "SchemaId": "delivery_date",
             "Label": "Delivery Date",
             "Type": "string",
             "Format": "dd/mm/yyyy",
             "IsRequired": "false",
             "InstructionsForAi": ""
         },
         {
             "Caegory": "data_point",
             "SchemaId": "delivery_no",
             "Label": "Delivery Note No",
             "Type": "string",
             "Format": "",
             "IsRequired": "false",
             "InstructionsForAi": ""
         },
         {
             "Caegory": "data_point",
             "SchemaId": "po_no",
             "Label": "PO Number",
             "Type": "string",
             "Format": "",
             "IsRequired": "false",
             "InstructionsForAi": ""
         },
         {
             "Caegory": "data_point",
             "SchemaId": "seller_name",
             "Label": "Seller Name",
             "Type": "string",
             "Format": "",
             "IsRequired": "true",
             "InstructionsForAi": ""
         }
     ]
 }

=========================================== INVOICE specific
  {
  "doc_type": "invoice",
  "doc_id": 0,
  "user_meta_data": "keep it empty",
  "content": {
    "supplier": "",
    "invoice_no": "Invoice number",
    "invoice_date": "Date of the invoice",
    "due_date": "Payment Due date",
    "po_no": "Purchase order number",
    "delivery_no": "Delivery note number",
    "job_reference": "Special field contains the customer order number",
    "delivery_date": "Date of the delivery",
    "invoice_subtotal": "Invoice Total Before Tax",
    "invoice_carriage ": "Total Carriage applied in invoice footer",
    "invoice_vat ": "Total invoice VAT amount ",
    "invoice_total": "Invoice Grand Total",
    "items": [
      {
        "item_date": "Date column in line items",
        "item_code": "Product or Item Code",
        "item_item_uom": "Unit of measurement",
        "item_po_no": "PO number found in line items",
        "item_delivery_no": "Delivery note or ticket number found in line items",
        "description": "Item Description",
        "quantity": "Decimal",
        "unitPrice": "Price per Unit in decimals",
        "discount_percent": "Price per Unit in decimals",
        "discount_amount": "Price per Unit in decimals",
        "vat_percent": "Price per Unit in decimals",
        "vat_amount": "Price per Unit in decimals",
        "total_excluding_vat": "Total Item Price",
        "total_including_vat": "Total Item Price"
      }
    ]
  }
}
 */
#endregion