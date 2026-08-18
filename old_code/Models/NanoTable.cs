using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Models
{

    public class NanoTable
    {
        public List<NanoRow> Rows { get; set; }
        public string Id { get; set; }
        public int xmin { get; set; }
        public int ymin { get; set; }
        public int xmax { get; set; }
        public int ymax { get; set; }
        public double Score { get; set; }
        public string OcrText { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }

        public NanoTable(NanoPrediction predictionData)
        {
            this.Id = predictionData.id;
            this.xmin = predictionData.xmin;
            this.ymin = predictionData.ymin;
            this.xmax = predictionData.xmax;
            this.xmax = predictionData.xmax;
            this.Score = predictionData.score;
            this.OcrText = predictionData.ocr_text;
            this.Type = predictionData.type;
            this.Label = predictionData.label;
            this.Status = predictionData.status;
            this.Rows = new List<NanoRow>();
            if (predictionData == null)
            {
                this.Rows.Add(new NanoRow());
                return;
            }
            if (predictionData.cells != null && predictionData.cells.Count > 0)
            {
                int row = 0; int col = 0;
                int NoOfColumns = predictionData.cells.Max(x => Convert.ToInt32(x.col));
                int NoOfRows = predictionData.cells.Max(x => Convert.ToInt32(x.row));
                //creating empty tables structure
                for (int r = 1; r <= NoOfRows; r++)
                {
                    NanoRow nRow = new NanoRow();
                    for (int c = 1; c <= NoOfColumns; c++)
                    {
                        nRow.Cells.Add(new NanoCell());
                    }
                    this.Rows.Add(nRow);
                }
                // Filling in Nano tables with data
                int prevRowNo = 0; int prevColNo = 0;
                foreach (NanoCell cell in predictionData.cells)
                {
                    row = (int)cell.row;
                    col = (int)cell.col;
                    if (col == 0) col = prevColNo+1; if (row == 0) row = prevRowNo; //special dealing for nano bug. when cell/row is zero
                    this.Rows[row - 1].Cells[col - 1] = cell;
                    this.Rows[row - 1].RowNo = cell.row;
                    this.Rows[row - 1].RowSpan = cell.row_span;
                    this.Rows[row - 1].RowLabel = cell.row_label;
                    prevRowNo = row; prevColNo = col; //special dealing for nano bug. when cell/row is zero
                }

                //NanoRow nanoRow = null;
                //NanoRow nanoRowPrev = null;
                //foreach (NanoCell cell in predictionData.cells)
                //{
                //    if (row != (int)cell.row)
                //    {
                //        row = (int)cell.row;
                //        if (row > 1) this.Rows.Add(nanoRow); //  all the rows except the last
                //        nanoRow = new NanoRow();
                //        nanoRow.RowNo = cell.row;
                //        nanoRow.RowSpan = cell.row_span;
                //        nanoRow.RowLabel = cell.row_label;
                //        col = 0;
                //    }
                //    nanoRow.Cells.Add(cell);
                //    col++;
                //}
                //this.Rows.Add(nanoRow);// the last row
            }
        }
    }
    public class NanoRow
    {
        public List<NanoCell> Cells { get; set; }
        public string RowLabel { get; set; }
        public int RowSpan { get; set; }
        public int RowNo { get; set; }
        public NanoRow()
        {
            this.Cells = new List<NanoCell>();
        }
    }

    public class NanoCell
    {
        public int row { get; set; }
        public int col { get; set; }
        public int row_span { get; set; }
        public int col_span { get; set; }
        public string text { get; set; }
        public string row_label { get; set; }
        public string id { get; set; }
        public string label { get; set; }
        public int xmin { get; set; }
        public int ymin { get; set; }
        public int xmax { get; set; }
        public int ymax { get; set; }
        public double score { get; set; }
        public string ocr_text { get; set; }
        public string type { get; set; }
        public string status { get; set; }

    }
    public class NanoGeometry
    {
        public double xmin { get; set; }
        public double xmax { get; set ;}
        public double ymin { get; set; }
        public double ymax { get; set; }
    }
}

