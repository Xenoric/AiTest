using System;
using System.Collections.Generic;
using AnyPath.Native;
using UnityEngine;

namespace AnyPath.Managed.CodeGen
{
    public class EdgeModDefinition
    {
        public string edgeModName;
        public string nodeName;
        public string nameSpace;
        
        public bool isValid;
        public bool isOpen;

        private Type myType;
        
        public bool IsCompatible(GraphDefinition graphDefinition)
        {
            if (!isOpen)
                return graphDefinition.nodeName == nodeName;

            // we assume the open generic param is indeed the node type
            // https://stackoverflow.com/questions/4864496/checking-if-an-object-meets-a-generic-parameter-constraint
            try
            {
                var check = myType.MakeGenericType(graphDefinition.nodeType);
                return true;
            }
            catch
            {
                // Debug.Log($"Edge modifier {modType.Name} is incompatible with {graphDefinition.graphName}");
                return false;
            }
        }

        public EdgeModDefinition(Type modType)
        {
            this.myType = modType;
            if (modType.GenericTypeArguments.Length > 1)
            {
                Debug.Log($"Edge modifier {modType.Name} has more than one open generic type parameter. This is not supported for the" +
                          $"code generator.");
                return;
            }

            this.nameSpace = modType.Namespace;  
            foreach(Type intType in modType.GetInterfaces()) 
            {
                if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IEdgeMod<>))
                {
                    var genericArguments = intType.GetGenericArguments();
                    if (genericArguments.Length != 1)
                        continue;
                    
                    edgeModName = CodeGeneratorUtil.GetRealTypeName(modType);

                    isOpen = genericArguments[0].IsGenericParameter;
                    nodeName = CodeGeneratorUtil.GetRealTypeName(genericArguments[0]);
                    isValid = true;
                    return;
                }
            }
        }
        

        public override string ToString()
        {
            return isValid ? $"{edgeModName} : IEdgeMod<{nodeName}>" : "?";
        }
        
        public string ToLabelString(string nodeType)
        {
            return isOpen ? edgeModName.Replace(nodeName, nodeType) : edgeModName;
        }
        
        public static void FindAll(List<EdgeModDefinition> definitions)
        {
            foreach (var modType in CodeGeneratorUtil.GetAllTypesImplementingOpenGenericType(typeof(IEdgeMod<>)))
            {
                var definition = new EdgeModDefinition(modType);
                if (definition.isValid)
                    definitions.Add(definition);
            }
        }
    }
}