using Documate.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DBContext context)
        {
            context.Database.EnsureCreated();

            // Look for any Tenant
            if (context.Accounts.Any())
                return;   // DB has been seeded

            //var docTypes = new SysDocType[]
            //{
            //    //new SysDocType { DocType = "Invoice", DocTypeKey = "invoice", CreatedOnUtc=DateTime.Now.ToUniversalTime(), SchemaJSON= "{\"id\":13}"},
            //    //new SysDocType { DocType = "Delivery Note", DocTypeKey = "delivery_note" , CreatedOnUtc=DateTime.Now.ToUniversalTime()},
            //    //new SysDocType { DocType = "Credit Note", DocTypeKey = "credit_note" , CreatedOnUtc=DateTime.Now.ToUniversalTime()},
            //    //new SysDocType { DocType = "Receipt", DocTypeKey = "receipt", CreatedOnUtc=DateTime.Now.ToUniversalTime() },
            //    //new SysDocType { DocType = "Performa Invoice", DocTypeKey = "performa_invoice" , CreatedOnUtc=DateTime.Now.ToUniversalTime()},
            //    //new SysDocType { DocType = "Purchase Order", DocTypeKey = "purchase_order" , CreatedOnUtc=DateTime.Now.ToUniversalTime()},
            //    //new SysDocType { DocType = "Bank Statement", DocTypeKey = "bank_statement", CreatedOnUtc=DateTime.Now.ToUniversalTime() }
            //};
            //foreach (SysDocType s in docTypes)
            //{
            //    context.SysDocTypes.Add(s);
            //}
            context.SaveChanges();
            context.Accounts.AddRange(
                new Account { Name = "Developer", CreditBalance = 10000, CreatedOnUtc = DateTime.Now.ToUniversalTime() },
                new Account { Name = "Simplicity", CreditBalance = 10000, CreatedOnUtc = DateTime.Now.ToUniversalTime() }
                );
            context.SaveChanges();
            context.Users.AddRange(
                new User { UserName = "Faheem", LoginId = "faheem@manticsoftware.com", Password = "1", AccountId = 1, CreatedOnUtc = DateTime.Now.ToUniversalTime() },
                new User { UserName = "Simplicity", LoginId = "ocr@simplicityOCR.com", Password = "1", AccountId = 2, CreatedOnUtc = DateTime.Now.ToUniversalTime() }
                );
            context.SaveChanges();
            context.UserQueues.AddRange(
                new UserQueue { UserId = 1, QueueId = 1, CreatedOnUtc = DateTime.Now.ToUniversalTime() },
                new UserQueue { UserId = 2, QueueId = 2, CreatedOnUtc = DateTime.Now.ToUniversalTime() }
                );
            context.SaveChanges();
            //context.SchemaNodeCategories.Add(new SchemaNodeCategory { Category = "datapoint", CreatedOnUtc = DateTime.Now.ToUniversalTime(), UpdatedOnUtc = DateTime.Now.ToUniversalTime()});
            //context.SchemaNodeCategories.Add(new SchemaNodeCategory { Category = "tuple", CreatedOnUtc = DateTime.Now.ToUniversalTime(), UpdatedOnUtc = DateTime.Now.ToUniversalTime()});
            //context.SaveChanges();
            context.AuthTokens.AddRange(
                new AuthToken { FlgDeleted = false, Token = "123456789", ExpiryHours = 1000, UserId = 1, CreatedOnUtc = DateTime.Now.ToUniversalTime(), UpdatedOnUtc = DateTime.Now.ToUniversalTime() },
                new AuthToken { FlgDeleted = false, Token = "123456789", ExpiryHours = 1000, UserId = 2, CreatedOnUtc = DateTime.Now.ToUniversalTime(), UpdatedOnUtc = DateTime.Now.ToUniversalTime() }
                );
            context.SaveChanges();
            context.SysDocStatuses.AddRange(
                new SysDocStatus { FlgDeleted = false, Status = "NOT_SET", StatusKey = "NOT_SET", Order = 0 },
                new SysDocStatus { FlgDeleted = false, Status = "IMPORTING", StatusKey = "IMPORTING", Order = 1 },
                new SysDocStatus { FlgDeleted = false, Status = "FAILED_IMPORT", StatusKey = "FAILED_IMPORT", Order = 2 },
                new SysDocStatus { FlgDeleted = false, Status = "TO_REVIEW", StatusKey = "TO_REVIEW", Order = 3 },
                new SysDocStatus { FlgDeleted = false, Status = "REVIEWING", StatusKey = "REVIEWING", Order = 4 },
                new SysDocStatus { FlgDeleted = false, Status = "EXPORTING", StatusKey = "EXPORTING", Order = 5 },
                new SysDocStatus { FlgDeleted = false, Status = "EXPORTED", StatusKey = "EXPORTED", Order = 6 },
                new SysDocStatus { FlgDeleted = false, Status = "FAILED_EXPORT", StatusKey = "FAILED_EXPORT", Order = 7 },
                new SysDocStatus { FlgDeleted = false, Status = "POSTPONED", StatusKey = "POSTPONED", Order = 8 },
                new SysDocStatus { FlgDeleted = false, Status = "DELETED", StatusKey = "DELETED", Order = 9 },
                new SysDocStatus { FlgDeleted = false, Status = "PURGED", StatusKey = "PURGED", Order = 10 }
            );
            //context.SaveChanges();
        }
    }
}