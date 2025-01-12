// using System;
// using System.Text;
// using AnyPath.Managed.Finders;
// using UnityEditor.Graphs;
//
// namespace AnyPath.Managed.CodeGen
// {
//     public enum DefinitionMode
//     {
//         NoMod,
//         ModOnly,
//         Both
//     }
//     
//     public abstract class FinderDefinition
//     {
//         public string baseClass;
//         public string resultType;
//
//
//         public abstract void Append(string asName, DefinitionMode mode, GraphDefinition graphDefintion, InterfaceDef interfaceDef,
//             StringBuilder stringBuilder);
//
//         protected string Replace(string s, string asName, GraphDefinition graphDefintion, InterfaceDef interfaceDef)
//         {
//             s = s.Replace("$Name", asName);
//             s = s.Replace("$TFinder", baseClass);
//             s = s.Replace("$TGraph", graphDefintion.fullGraphName);
//             s = s.Replace("$TNode", graphDefintion.fullNodeName);
//             s = s.Replace("$TEdge", graphDefintion.fullEdgeName);
//
//             if (interfaceDef == null)
//             {
//                 s = s.Replace("$Interface", "");
//             }
//             else
//             {
//                 s = interfaceDef.Replace(s);
//             }
//             
//             return s;
//         }
//     }
//
//     public class FinderDefinitionDefault : FinderDefinition
//     {
//         private const string WithoutMods = "public class $Name : $TFinder<$TGraph, $TNode, $TEdge, NoMod<TEdge>> { }";
//         private const string OnlyMods = "public class $Name : $TFinder<$TGraph, $TNode, $TEdge, TMod> { }";
//         private const string Both =  OnlyMods + "\npublic class $Name : $Name<NoMod<$TEdge>> { }";
//         
//         public override void Append(string asName, DefinitionMode mode, GraphDefinition graphDefintion, InterfaceDef interfaceDef, StringBuilder stringBuilder)
//         {
//             string s = string.Empty;
//             switch (mode)
//             {
//                
//                 case DefinitionMode.NoMod:
//                     s = WithoutMods;
//                     break;
//                 case DefinitionMode.Both:
//                     s = Both;
//                     break;
//                 case DefinitionMode.ModOnly:
//                     s = OnlyMods;
//                     break;
//             }
//
//             s = Replace(s, asName, graphDefintion, interfaceDef);
//             stringBuilder.AppendLine(s);
//         }
//     }
//
//     public class FinderDefinitionOption : FinderDefinition
//     {
//         private const string WithoutMods = "public class $Name<TOption> : $TFinder<TOption, $TGraph, $TNode, $TEdge, NoMod<$TEdge>> { }";
//         private const string OnlyMods = "public class $Name<TOption, TMod> : $TFinder<TOption, $TGraph, $TNode, $TEdge, TMod>";
//         private const string Both =  OnlyMods + "\npublic class $Name<TOption> : $Name<TOption, NoMod<$TEdge>> { }";
//
//         public override void Append(string asName, DefinitionMode mode, GraphDefinition graphDefintion, InterfaceDef interfaceDef, StringBuilder stringBuilder)
//         {
//             string s = string.Empty;
//             switch (mode)
//             {
//                
//                 case DefinitionMode.NoMod:
//                     s = WithoutMods;
//                     break;
//                 case DefinitionMode.Both:
//                     s = Both;
//                     break;
//                 case DefinitionMode.ModOnly:
//                     s = OnlyMods;
//                     break;
//             }
//
//             s = Replace(s, asName, graphDefintion);
//             stringBuilder.AppendLine(s);
//         }
//     }
//
//     public abstract class InterfaceDef
//     {
//         public string baseInterface;
//         public abstract string GetName(string asName, DefinitionMode mode, GraphDefinition graphDefintion);
//
//         public abstract string Replace(string s)
//         {
//             return s;
//         }
//
//         public void Append(string asName, DefinitionMode mode, GraphDefinition graphDefintion, StringBuilder stringBuilder)
//         {
//             switch (mode)
//             {
//                 case DefinitionMode.NoMod:
//                     stringBuilder.AppendLine($"{asName} : {baseClass}<>")
//                     break;
//             }
//             
//         }
//     }
//
//     public class InterfaceOptionDef : InterfaceDef
//     {
//         private const string WithoutMod = "public interface I$NameOptionFinder<TOption, out TResult> : IOptionsFinder<TOption, $TGraph, $TNode, TResult> { }";
//         
//         
//         public override string GetName(string asName, DefinitionMode mode, GraphDefinition graphDefintion)
//         {
//             return $"I{asName}<TOption,"
//         }
//     }
// }