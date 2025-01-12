using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AnyPath.Managed.CodeGen
{
    /// <summary>
    /// Utility window to quickly generate definition code for custom graph finders.
    /// </summary>
    public class FinderCodeGenerator : EditorWindow {
        
        [MenuItem ("Window/AnyPath Code Generator")]
        public static void  ShowWindow () {
            GetWindow(typeof(FinderCodeGenerator));
        }

        private List<GraphDefinition> graphDefinitions = new List<GraphDefinition>();
        private List<HeuristicDefinition> allHeuristicDefinitions = new List<HeuristicDefinition>();
        private List<EdgeModDefinition> allEdgeModDefinitions = new List<EdgeModDefinition>();
        private List<ProcessorDefinition> allProcessorDefinitions = new List<ProcessorDefinition>();

        private string[] graphLabels;
        private Dictionary<GraphDefinition, string[]> heuristicLabels = new Dictionary<GraphDefinition, string[]>();
        private Dictionary<GraphDefinition, string[]> edgeModLabels = new Dictionary<GraphDefinition, string[]>();
        private Dictionary<GraphDefinition, string[]> processorLabels = new Dictionary<GraphDefinition, string[]>();

        private Dictionary<GraphDefinition, HeuristicDefinition[]> graphToHeuristics =
            new Dictionary<GraphDefinition, HeuristicDefinition[]>();

        private Dictionary<GraphDefinition, EdgeModDefinition[]> graphToEdgeMods = new Dictionary<GraphDefinition, EdgeModDefinition[]>();

        private Dictionary<GraphDefinition, ProcessorDefinition[]> graphToProcessors =
            new Dictionary<GraphDefinition, ProcessorDefinition[]>();

        private int graphIndex;
        private int heuristicIndex;
        private int edgeModIndex;
        private int processorIndex;
        private bool useInterfaces;

        private string currentCode;
        private Vector2 scroll;
        private string namePrefix;
        private string optionType;

        void OnGUI () {
            
            bool regen = false;
            if (GUILayout.Button("Scan Graph Definitions"))
            {
                Scan();
                regen = true;
            }

            if (graphDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Scan your project for graph definitions first", MessageType.Info);
                return;
            }

           
            EditorGUI.BeginChangeCheck();
            
            graphIndex = EditorGUILayout.Popup("Graph", graphIndex, graphLabels);
            if (graphIndex < 0 || graphIndex >= graphDefinitions.Count)
            {
                EditorGUILayout.HelpBox("Select a graph definition", MessageType.Info);
                EditorGUI.EndChangeCheck();
                return;
            }

            var graph = graphDefinitions[graphIndex];
            if (EditorGUI.EndChangeCheck() || regen)
            {
                regen = true;
                
                // find best defaults for edgemod and heuristic
                var heuristics = heuristicLabels[graph];
                for (int i = 0; i < heuristics.Length; i++)
                {
                    if (heuristics[i].StartsWith(graph.graphName))
                    {
                        heuristicIndex = i;
                        break;
                    }
                }

                var edgeMods = edgeModLabels[graph];
                for (int i = 0; i < edgeMods.Length; i++)
                {
                    if (edgeMods[i].StartsWith("NoEdgeMod"))
                    {
                        edgeModIndex = i;
                        break;
                    }
                }
                
                var processors = processorLabels[graph];
                for (int i = 0; i < processors.Length; i++)
                {
                    if (processors[i].StartsWith(graph.graphName))
                    {
                        processorIndex = i;
                        break;
                    }
                }
            }
            
         
            
            EditorGUI.BeginChangeCheck();
            heuristicIndex = EditorGUILayout.Popup("Heuristic", heuristicIndex, heuristicLabels[graph]);
            processorIndex = EditorGUILayout.Popup("Path Processor", processorIndex, processorLabels[graph]);
            edgeModIndex = EditorGUILayout.Popup("Edge Modifier", edgeModIndex, edgeModLabels[graph]);
            heuristicIndex = Mathf.Clamp(heuristicIndex, 0, graphToHeuristics[graph].Length - 1);
            processorIndex = Mathf.Clamp(processorIndex, 0, graphToProcessors[graph].Length - 1);
            edgeModIndex = Mathf.Clamp(edgeModIndex, 0, graphToEdgeMods[graph].Length - 1);

            namePrefix = EditorGUILayout.TextField("Name Prefix", namePrefix);
            optionType = EditorGUILayout.TextField("Option Type", optionType);
            useInterfaces = EditorGUILayout.Toggle("Interfaces", useInterfaces);

            if (EditorGUI.EndChangeCheck() || regen)
            {
                if ((heuristicIndex >= 0 && heuristicIndex < graphToHeuristics[graph].Length) &&
                    (edgeModIndex >= 0 && edgeModIndex < graphToEdgeMods[graph].Length) &&
                    (processorIndex >= 0 && processorIndex < graphToProcessors[graph].Length))
                {
                    currentCode = GenerateCode(
                        graph,
                        graphToHeuristics[graph][heuristicIndex],
                        graphToProcessors[graph][processorIndex],
                        graphToEdgeMods[graph][edgeModIndex], namePrefix, optionType, useInterfaces);
                }
                else
                {
                    currentCode = string.Empty;
                }
            }
 
            scroll = EditorGUILayout.BeginScrollView(scroll);
            currentCode = EditorGUILayout.TextArea(currentCode, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void Scan()
        {
            graphDefinitions.Clear();
            GraphDefinition.FindAll(graphDefinitions);
            graphIndex = Mathf.Clamp(graphIndex, 0, graphDefinitions.Count - 1);
            graphLabels = graphDefinitions.Select(def => def.ToLabelString()).ToArray();
            
            allHeuristicDefinitions.Clear();
            HeuristicDefinition.FindAll(allHeuristicDefinitions);

            allEdgeModDefinitions.Clear();
            EdgeModDefinition.FindAll(allEdgeModDefinitions);

            allProcessorDefinitions.Clear();
            ProcessorDefinition.FindAll(allProcessorDefinitions);
            
            graphToHeuristics.Clear();
            graphToEdgeMods.Clear();
            graphToProcessors.Clear();

            foreach (var graph in graphDefinitions)
            {
                var compatibleHeuristics = allHeuristicDefinitions.Where(h => h.IsCompatible(graph)).ToArray();
                var compatibleEdgeMods = allEdgeModDefinitions.Where(h => h.IsCompatible(graph)).ToArray();
                var compatibleProcessors = allProcessorDefinitions.Where(h => h.IsCompatible(graph)).ToArray();

                graphToHeuristics[graph] = compatibleHeuristics;
                heuristicLabels[graph] = compatibleHeuristics.Select(h => h.ToLabelString(graph.nodeName)).ToArray();

                graphToEdgeMods[graph] = compatibleEdgeMods;
                edgeModLabels[graph] = compatibleEdgeMods.Select(h => h.ToLabelString(graph.nodeName)).ToArray();
                
                graphToProcessors[graph] = compatibleProcessors;
                processorLabels[graph] = compatibleProcessors.Select(h => h.ToLabelString(graph.nodeName)).ToArray();
            }
        }

        private static string GenerateCode(
            GraphDefinition graph, 
            HeuristicDefinition heuristic, 
            ProcessorDefinition processor, 
            EdgeModDefinition edgeMod, 
            string namePrefix, string optionType, bool useInterfaces)
        {
            string segName = processor.GetSegmentName(graph.nodeName);
            string template = (useInterfaces ? Template : TemplateNoInterfaces).Replace("{Name}", string.IsNullOrEmpty(namePrefix) ? graph.graphName : namePrefix);

            if (string.IsNullOrEmpty(optionType))
            {
                // no option finders
                template = template.Split('!')[0];
            }
            else
            {
                template = template.Replace("!", string.Empty);
                template = template.Replace("{TOption}", optionType);
            }

            HashSet<string> nameSpaces = new HashSet<string>()
                { "AnyPath.Native", "AnyPath.Managed", "AnyPath.Managed.Finders", "AnyPath.Managed.Results" };
            nameSpaces.Add(graph.nameSpace);
            nameSpaces.Add(heuristic.nameSpace);
            nameSpaces.Add(processor.nameSpace);
            nameSpaces.Add(edgeMod.nameSpace);
            StringBuilder namespaceSb = new StringBuilder();
            foreach (string nameSpace in nameSpaces)
                namespaceSb.AppendLine($"using {nameSpace};");
            
            template = template.Replace("{Namespaces}", namespaceSb.ToString());
            template = template.Replace("{TGraph}", graph.graphName);
            template = template.Replace("{TNode}", graph.nodeName);
            template = template.Replace("{TH}", heuristic.ToLabelString(graph.nodeName));
            template = template.Replace("{TProc}", processor.ToLabelString(graph.nodeName));
            template = template.Replace("{TMod}", edgeMod.ToLabelString(graph.nodeName));
            template = template.Replace("{TSeg}", segName);

            return template;
        }

private const string Template =
            @"{Namespaces}
public interface I{Name}PathFinder<out TResult> : IPathFinder<{TGraph}, {TNode}, TResult> { }
public interface I{Name}MultiFinder<out TResult> : IMultiFinder<{TGraph}, {TNode}, TResult> { }

public class {Name}PathFinder : PathFinder<{TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}>, I{Name}PathFinder<Path<{TSeg}>> { }
public class {Name}PathEvaluator: PathEvaluator<{TGraph}, {TNode}, {TH}, {TMod}>, I{Name}PathFinder<Eval> { }

public class {Name}MultiPathFinder : MultiPathFinder<{TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}>, I{Name}MultiFinder<MultiPathResult<{TSeg}>> { }
public class {Name}MultiPathEvaluator : MultiPathEvaluator<{TGraph}, {TNode}, {TH}, {TMod}>, I{Name}MultiFinder<MultiEvalResult>  { }

public class {Name}DijkstraFinder : DijkstraFinder<{TGraph}, {TNode}, {TMod}> { }
!
public interface I{Name}OptionFinder<out TResult> : IOptionFinder<{TOption}, {TGraph}, {TNode}, TResult>  { }

public class {Name}OptionFinder : OptionFinder<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}>, I{Name}OptionFinder<Path<{TOption}, {TSeg}>> { }
public class {Name}OptionEvaluator : OptionEvaluator<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}>, I{Name}OptionFinder<Eval<{TOption}>> { }

public class {Name}CheapestOptionFinder : CheapestOptionFinder<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}>, I{Name}OptionFinder<Path<{TOption}, {TSeg}>> { }
public class {Name}CheapestOptionEvaluator : CheapestOptionEvaluator<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}>, I{Name}OptionFinder<Eval<{TOption}>>  { }

public class {Name}PriorityOptionFinder : PriorityOptionFinder<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}>, I{Name}OptionFinder<Path<{TOption}, {TSeg}>> { }
public class {Name}PriorityOptionEvaluator : PriorityOptionEvaluator<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}>, I{Name}OptionFinder<Eval<{TOption}>>  { }";
        
private const string TemplateNoInterfaces =
            @"{Namespaces}
public class {Name}PathFinder : PathFinder<{TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}>  { }
public class {Name}PathEvaluator: PathEvaluator<{TGraph}, {TNode}, {TH}, {TMod}> { }

public class {Name}MultiPathFinder : MultiPathFinder<{TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}> { }
public class {Name}MultiPathEvaluator : MultiPathEvaluator<{TGraph}, {TNode}, {TH}, {TMod}> { }

public class {Name}DijkstraFinder : DijkstraFinder<{TGraph}, {TNode}, {TMod}> { }
!
public class {Name}OptionFinder: OptionFinder<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}> { }
public class {Name}OptionEvaluator : OptionEvaluator<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}> { }

public class {Name}CheapestOptionFinder : CheapestOptionFinder<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}> { }
public class {Name}CheapestOptionEvaluator : CheapestOptionEvaluator<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}> { }

public class {Name}PriorityOptionFinder : PriorityOptionFinder<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}, {TProc}, {TSeg}> { }
public class {Name}PriorityOptionEvaluator : PriorityOptionEvaluator<{TOption}, {TGraph}, {TNode}, {TH}, {TMod}> { }";
    }
}