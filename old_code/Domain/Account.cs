using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Documate.Domain
{

    public class Account : BaseEntity
    {
        [MaxLength(50)]
        public string Name { get; set; }
        //
        [DefaultValue(0)]
        public int CreditBalance { get; set; }

        [DefaultValue(0)]
        public bool FlgActive { get; set; }
        public int AiServiceSource { get; set; }
        [MaxLength(256)]
        public string Logo { get; set; }

        //[ForeignKey(nameof(Acc))]
        //public new int? EmployeeId { get; set; }
        //public virtual Tenant Acc { get; set; }
    }

    public class User : BaseEntity
    {
        [MaxLength(50)]
        public string UserName { get; set; }       
        [MaxLength(30)]
        public string LoginId { get; set; }    // Email
        [MaxLength(15)]
        public string Password { get; set; }       
        [MaxLength(16)]
        public string AuthKey { get; set; }
        [MaxLength(32)]
        public string AuthSecret { get; set; }
        [MaxLength(256)]
        public string Avatar { get; set; }

        [ForeignKey(nameof(Acc))]
        public int AccountId { get; set; } 
        public virtual Account Acc { get; set; }
    }

    public class AuthToken : BaseEntity
    {
        public string Token { get; set; }
        public int ExpiryHours { get; set; }

        [ForeignKey(nameof(UserObj))]
        public int UserId { get; set; }
        public virtual User UserObj{ get; set; }
    }

}
