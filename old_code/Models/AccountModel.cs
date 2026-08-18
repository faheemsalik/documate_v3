using Documate.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Models
{
    [NotMapped]
    public class AccountModel:Account
    {
    }
    /* Naming Convention for Models
     * We have three areas of the application:
     * 1- Internal models for services
     * 2- Models for App UI
     * 3- Models for API
    */
    [NotMapped]
    public class UserModel : User
    {
        public string AccountName { get; set; }
        public string AccountLogo { get; set; }
    }

    public class ChangePasswordVM 
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class CredentialModel
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class LoginReturnModel
    {
        public string Token { get; set; }
        public DateTime TokenExpiry { get; set; }
        public string LoginId { get; set; }
        public string UserName { get; set; }
        public bool IsSuccfull { get; set; }
        public string Message { get; set; }
    }

    public class JWTAuthentications
    {
        public static string ValidAudience { get; set; }
        public static string ValidIssuer { get; set; }
        public static string Secret { get; set; }
        public static string TokenExpiry { get; set; }
    }
}
