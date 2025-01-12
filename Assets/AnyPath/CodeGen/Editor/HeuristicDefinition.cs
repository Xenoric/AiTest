using System;
using System.Collections.Generic;
using AnyPath.Native;
using UnityEngine;

namespace AnyPath.Managed.CodeGen
{

    public class HeuristicDefinition
    {
        public string heuristicName;
        public string nodeName;
        public string nameSpace;
        public bool isValid;
        public bool isOpen;
        public Type myType;

        public bool IsCompatible(GraphDefinition graphDefinition)
        {
            if (!isOpen)
                return nodeName == graphDefinition.nodeName;
            

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

        public HeuristicDefinition(Type heuristicType)
        {
            myType = heuristicType;
            if (myType.GenericTypeArguments.Length > 1)
            {
                Debug.Log($"Heuristic provider {heuristicType.Name} has more than one open generic type parameter. This is not supported for the" +
                          $"code generator.");
                return;
            }

            this.nameSpace = myType.Namespace;
            foreach(Type intType in heuristicType.GetInterfaces()) 
            {
                if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IHeuristicProvider<>))
                {
                    nameSpace = heuristicType.Namespace;
                    heuristicName = CodeGeneratorUtil.GetRealTypeName(heuristicType);
                  
                    
                    var genericArguments = intType.GetGenericArguments();
                    if (genericArguments.Length != 1)
                        return;

                    isOpen = genericArguments[0].IsGenericParameter;
                    nodeName = CodeGeneratorUtil.GetRealTypeName(genericArguments[0]);

                    isValid = true;
                    return;
                }
            }
        }
        
        public override string ToString()
        {
            return isValid ? $"{heuristicName} : IHeuristicProvider<{nodeName}>" : "?";
        }

        public string ToLabelString(string nodeType)
        {
            return isOpen ? heuristicName.Replace(nodeName, nodeType) : heuristicName;
        }
        
        public static void FindAll(List<HeuristicDefinition> definitions)
        {
            var findType = typeof(IHeuristicProvider<>);

            foreach (var type in CodeGeneratorUtil.GetAllTypesImplementingOpenGenericType(findType))
            {
                var definition = new HeuristicDefinition(type);
                if (definition.isValid)
                    definitions.Add(definition);
            }
        }
    }
}