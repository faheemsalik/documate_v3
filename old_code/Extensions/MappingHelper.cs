using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;


namespace Documate.Extensions
{
    public static class MappingHelper
    {
        public static void CopyPropertyValues(this object destination, object source, bool includeVirtualObject = false, List<string> ignoreProperties = null)
        {
            if (source == null || destination == null) 
                return;
            if (destination is IEnumerable)
            {
                var dest_enumerator = (destination as IEnumerable).GetEnumerator();
                var src_enumerator = (source as IEnumerable).GetEnumerator();

                while (dest_enumerator.MoveNext() && src_enumerator.MoveNext())
                {
                    dest_enumerator.Current.CopyPropertyValues(src_enumerator.Current, includeVirtualObject);
                }
            }
            else
            {
                var destProperties = destination.GetType().GetRuntimeProperties();

                foreach (var sourceProperty in source.GetType().GetRuntimeProperties())
                {
                    foreach (var destProperty in destProperties)
                    {
                        if (ignoreProperties == null || ignoreProperties.Contains(destProperty.Name) == false)
                        {                           
                            if (destProperty.CanWrite
                                && destProperty.Name == sourceProperty.Name
                                && destProperty.PropertyType.GetTypeInfo().IsAssignableFrom(sourceProperty.PropertyType.GetTypeInfo())
                                && destination.GetType().GetProperty(destProperty.Name).GetSetMethod() != null
                                && (includeVirtualObject == true || destination.GetType().GetProperty(destProperty.Name).GetSetMethod().IsVirtual == false)
                                && destination.GetType().GetProperty(destProperty.Name).GetCustomAttributes(typeof(NotMappedAttribute), false).Length == 0)
                            {
                                try
                                {
                                    destProperty.SetValue(destination, sourceProperty.GetValue(source, new object[] { }), new object[] { });
                                }
                                catch { }
                                break;
                            }
                        }
                    }
                }
            }

            
        }
    }
}