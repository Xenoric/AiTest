using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AnyPath.Managed.CodeGen
{
    public static class CodeGeneratorUtil
    {
        public static IEnumerable<Type> GetAllTypesImplementingOpenGenericType(Type openGenericType, Assembly assembly)
        {
            return from x in assembly.GetTypes()
                from z in x.GetInterfaces()
                let y = x.BaseType
                where
                    (y != null && y.IsGenericType &&
                     openGenericType.IsAssignableFrom(y.GetGenericTypeDefinition())) ||
                    (z.IsGenericType &&
                     openGenericType.IsAssignableFrom(z.GetGenericTypeDefinition()))
                select x;
        }
        
        public static IEnumerable<Type> GetAllTypesImplementingOpenGenericType(Type openGenericType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in GetAllTypesImplementingOpenGenericType(openGenericType, assembly))
                    yield return type;
        }
        
        public static string GetRealTypeName(Type t)
        {
            if (!t.IsGenericType)
                return GetNestedTypeName(t);

            StringBuilder sb = new StringBuilder();
            var nestFixedName = GetNestedTypeName(t);
            
            sb.Append(nestFixedName.Substring(0, nestFixedName.IndexOf('`')));
            sb.Append('<');
            bool appendComma = false;
            foreach (Type arg in t.GetGenericArguments())
            {
                if (appendComma) sb.Append(',');
                sb.Append(GetRealTypeName(arg));
                appendComma = true;
            }
            sb.Append('>');
            return sb.ToString();
        }
        
        /// <summary>
        /// Adds parent class before the name
        /// </summary>
        private static string GetNestedTypeName(Type type, string postfix = "")
        {
            if (type.IsGenericParameter)
                return "{" + type.Name + "}" + postfix;
            if (type.DeclaringType == null)
                return type.Name + postfix;
            
            return GetNestedTypeName(type.DeclaringType, "." + type.Name + postfix);
        }
    }
}