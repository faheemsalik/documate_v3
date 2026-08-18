using Amazon.Textract.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Documate.Models
{
    public class TemplateRulesModel
    {
        public string version { get; set; }
        public string doc_type { get; set; }
        public List<Rule> rules { get; set; }

        public class Rule
        {
            public string rule_type { get; set; } // action, identifier
            public string category { get; set; } // product_table, page, section
            public string schema_id { get; set; } // one of schema id stored in the queue schema json
            public Location location { get; set; }
            public ProductTable product_table { get; set; }  // null in case category is not product_table
            public PageAction page_actions { get; set; }  //remove_pages 

            public class Location
            {
                public string type { get; set; }  // relative_block , bounding box
                public string position { get; set; } // absolute | offset
                public string page { get; set; } // last, 2
                public RelativeBlock relative_block { get; set; }
                public BoundingBox bounding_box { get; set; }

                public class RelativeBlock
                {
                    public string block_type { get; set; } //word/line/tables/cell/row
                    public string block_value { get; set; }
                    public string block_comparison_type { get; set; } //equal | contains
                }
            }
            // keep the original tables as it is and create new tables to store the product tables data.
            public class ProductTable
            {
                public string[] column_captions_to_identify { get; set; } // column captions of the product tables
                public int skip_top_rows { get; set; }
                public int skip_bottom_rows { get; set; }
                public List<Column> columns { get; set; }
                public Row row { get; set; }

                public class Column
                {
                    public string col_index { get; set; } // for identification
                    public string col_caption { get; set; } // for identification
                    public Action actions { get; set; }

                    public class Action
                    {
                        public Split split { get; set; }
                        public Merge merge { get; set; }

                        public class Split
                        {
                            public string caption_reg_exp { get; set; }
                            public string value_reg_exp { get; set; }
                        }

                        public class Merge
                        {
                            public int[] col_index { get; set; } // array of column index to be merged
                            public string[] col_captions { get; set; } // array of column captions to be merged
                            public string separator { get; set; } // separator between the column values if required like adding "-", default is a space
                        }
                    }
                }
                public class Row
                {
                    public RowDivisionAction row_division_action { get; set; }
                    public RowOmitionAction row_omition_action { get; set; } // "first" | last

                    public class RowDivisionAction
                    {
                        public int base_column_index { get; set; }
                        public string value_alignment { get; set; } // top | bottom
                    }
                    public class RowOmitionAction
                    {
                        public int min_cols_having_values { get; set; } // at least x number of tables must have values otherwise ommit the row.
                        public string[] required_column_captions { get; set; } // Ommit the row if any of the given tables don't have value in it.
                    }
                    public class LastRow
                    {
                        public bool has_summary { get; set; }
                        public List<SummaryComposition> summary_compositions { get; set; }

                    }
                    public class SummaryComposition // how summary is composed and how should it be read by algorithm
                    {
                        public int col_index { get; set; }
                        public string schema_id { get; set; }
                    }
                }
            }
            public class PageAction
            {
                public string[] remove_pages { get; set; } // ["last", "2","3", "after_block"]
                public string cut_off_block_value { get; set; } // Block value for "after_block"

            }

        }
    }
}