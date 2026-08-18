using Documate.Domain;
using Documate.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Documate.Extensions
{
    public static class Extensions
    {
        public static List<string> ValidateEntity<TEntity>(this TEntity entity) where TEntity : BaseEntity
        {
            // check if primary key value is 0
            List<string> errorList = new List<string>();
            KeyAttribute keyAtt;
            DatabaseGeneratedAttribute dbCreatedAttribute;
            foreach (var prop in entity.GetType().GetProperties())
            {
                keyAtt = prop.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault() as KeyAttribute;
                dbCreatedAttribute = prop.GetCustomAttributes(typeof(DatabaseGeneratedAttribute), false).FirstOrDefault() as DatabaseGeneratedAttribute;

                if (keyAtt != null && (dbCreatedAttribute == null || dbCreatedAttribute.DatabaseGeneratedOption != DatabaseGeneratedOption.Identity))
                {
                    var value = prop.GetValue(entity, null);
                    if (value == null) errorList.Add(prop.Name + " is missing");
                    else if (Convert.ToInt32(value) == 0) errorList.Add(prop.Name + " can not be zero");
                    break;
                }
                //if (prop.PropertyType.FullName == "System.DateTime")
                //{
                //    if (prop.GetValue(entity, null).ToString() == DateTime.MinValue.ToString())
                //        errorList.Add(prop.Name + " is missing");
                //}
            }
            try
            {
                var context = new ValidationContext(entity, null, null);
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(entity, context, results, true))
                {
                    foreach (var error in results)
                    {
                        errorList.Add(error.ErrorMessage);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return errorList;
        }

        public static Func<T, bool> And<T>(this Func<T, bool> left, Func<T, bool> right)
    => a => left(a) && right(a);

        public static Func<T, bool> Or<T>(this Func<T, bool> left, Func<T, bool> right)
            => a => left(a) || right(a);

        public static async Task ForEachAsync<T>(this List<T> list, Func<T, Task> func)
        {
            foreach (var value in list)
            {
                await func(value);
            }
        }
    }
    //public class TokenValidateAttribute : ActionFilterAttribute
    //{
    //    //protected readonly IAccountService accountService;
    //    //public TokenValidateAttribute(IAccountService accountService)
    //    //{
    //    //    this.accountService = accountService;
    //    //}
    //    public override void OnActionExecuting(ActionExecutingContext filterContext)
    //    {
    //        ResponseModel returnValue = new ResponseModel();
    //        string authToken = string.Empty;
    //        var authHeader = filterContext.HttpContext.Request.Headers.Where(x => x.Key == "token");
    //        if (authHeader != null)
    //            authToken = authHeader.FirstOrDefault().Value.ToString();

    //        if (accountService.AuthValidility(authToken))
    //        {
    //            filterContext.Result = new RedirectToRouteResult("Default", new RouteValueDictionary
    //                                        {
    //                                            { "controller", "Home" },
    //                                            { "action", "FirstTime" }
    //                                        });
    //        }
    //        else
    //        {
    //            //what ever you want, or nothing at all
    //        }
    //    }
    //}
}
