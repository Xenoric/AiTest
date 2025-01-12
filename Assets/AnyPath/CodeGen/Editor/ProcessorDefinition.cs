using System;
using System.Collections.Generic;
using AnyPath.Native;

namespace AnyPath.Managed.CodeGen
{
    public class ProcessorDefinition
    {
        public string processorName;
        public string nodeName;
        public string segmentName;
        public string nameSpace;
        
        public bool isValid;
        public bool nodeIsOpen;
        public bool segmentIsOpen;

        public bool IsCompatible(GraphDefinition graphDefinition)
        {
            return nodeIsOpen || graphDefinition.nodeName == nodeName;
        }

        public ProcessorDefinition(Type procType)
        {
            this.nameSpace = procType.Namespace;
            
            foreach(Type intType in procType.GetInterfaces()) 
            {
                if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IPathProcessor<,>))
                {
                    var genericArguments = intType.GetGenericArguments();
                    if (genericArguments.Length != 2)
                        continue;

                    // Can't have open types for path processing
                    nodeIsOpen = genericArguments[0].IsGenericParameter;
                    segmentIsOpen = genericArguments[1].IsGenericParameter;
                   
                    
                    processorName = CodeGeneratorUtil.GetRealTypeName(procType);
                    nodeName = CodeGeneratorUtil.GetRealTypeName(genericArguments[0]);
                    segmentName = CodeGeneratorUtil.GetRealTypeName(genericArguments[1]);

                    if (segmentIsOpen && nodeName != segmentName)
                    {
                        continue;
                    }

                    isValid = true;
                    return;
                }
            }
        }
        

        public override string ToString()
        {
            return isValid ? $"{processorName} : IPathProcessor<{nodeName}, {segmentName}>" : "?";
        }
        
        public string ToLabelString(string nodeType)
        {
            return nodeIsOpen ? processorName.Replace(nodeName, nodeType) : processorName;
        }

        public string GetSegmentName(string nodeType)
        {
            return segmentIsOpen ? nodeType : segmentName;
        }

        public static void FindAll(List<ProcessorDefinition> definitions)
        {
            foreach (var modType in CodeGeneratorUtil.GetAllTypesImplementingOpenGenericType(typeof(IPathProcessor<,>)))
            {
                var definition = new ProcessorDefinition(modType);
                if (definition.isValid)
                    definitions.Add(definition);
            }
        }
    }
}