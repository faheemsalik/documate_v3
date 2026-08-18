using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Documate.Domain
{
    public class CreditPurchase : BaseEntity
    {
        public DateTime PurchaseDate { get; set; }
        public int NoOfCredits { get; set; }
        public float PricePerDoc { get; set; }
        public int AmountPaid { get; set; }
        [MaxLength(200)]
        public string Desc { get; set; }
        public DateTime ExpireOn { get; set; }

        [ForeignKey(nameof(Acc))]
        public int AccountId { get; set; }
        public virtual Account Acc { get; set; }
    }

    public class CustomerInvoice : BaseEntity
    {
        [MaxLength(20)]
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int CreditUsed { get; set; }
        public float PricePerDoc { get; set; }
        [MaxLength(200)]
        public string Desc { get; set; }
        public int ExportedDocsCount { get; set; }

        [ForeignKey(nameof(Acc))]
        public int AccountId { get; set; }
        public virtual Account Acc { get; set; }
    }

    public class InvoiceDoc : BaseEntity
    {
        [ForeignKey(nameof(InvoiceObj))]
        public virtual int InvoiceId { get; set; }
        public virtual CustomerInvoice InvoiceObj { get; set; }

        [ForeignKey(nameof(DocObj))]
        public int DocId { get; set; }
        public virtual Document DocObj { get; set; }
    }
}
