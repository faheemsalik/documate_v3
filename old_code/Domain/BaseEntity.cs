using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Documate.Domain
{
    /// <summary>
    /// Base class for entities
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public int Id { get; set; }

        //[ForeignKey(nameof(Acc))]
        //public virtual int CreatedByUserId { get; set; }
        //public virtual Tenant Acc { get; set; }
        [DefaultValue(0)]
        public bool FlgDeleted { get; set; }
        public DateTime CreatedOnUtc 
        {
            get
            {
                return this.dateCreated.HasValue
                   ? this.dateCreated.Value
                   : DateTime.Now;
            }

            set { this.dateCreated = value; }
        }
        public DateTime UpdatedOnUtc { get; set; }
        private DateTime? dateCreated = null;
    }

}
