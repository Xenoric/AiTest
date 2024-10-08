using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    [CustomEditor(typeof(NavAgent)), CanEditMultipleObjects()]
    internal class NavAgentInspector : Editor
    {
        SerializedProperty spHeight;
        SerializedProperty spMaxSlopeAngle;
        SerializedProperty spAutoRepathIntervall;
        SerializedProperty spLinkTraversalCostMultipliers;
        SerializedProperty spNavTagTraversalCostMultipliers;
        SerializedProperty spMaximumDistanceToPathStart;
        SerializedProperty spAllowCloseEnoughPath;
        SerializedProperty spEnableDebugMessages;

        bool linkMultipliersOpen;
        bool navTagMultipliersOpen;
        bool advancedOpen;
        NavAgent agent;
        NavSurface[] surfaces;

        public void OnEnable()
        {
            spHeight = serializedObject.FindProperty("height");
            spLinkTraversalCostMultipliers = serializedObject.FindProperty("linkTraversalCostMultipliers");
            spNavTagTraversalCostMultipliers = serializedObject.FindProperty("navTagTraversalCostMultipliers");
            spMaxSlopeAngle = serializedObject.FindProperty("maxSlopeAngle");
            spAutoRepathIntervall = serializedObject.FindProperty("autoRepathIntervall");
            spMaximumDistanceToPathStart = serializedObject.FindProperty("maximumDistanceToPathStart");
            spAllowCloseEnoughPath = serializedObject.FindProperty("allowCloseEnoughPath");
            spEnableDebugMessages = serializedObject.FindProperty("enableDebugMessages");

            agent = target as NavAgent;
            surfaces = GameObject.FindObjectsOfType<NavSurface>();
        }

        public override void OnInspectorGUI()
        {
            string name = agent.name.ToLower();
           
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(spHeight);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("From renderer"))
            {
                Renderer r = agent.GetComponent<Renderer>();
                if (r == null) r = agent.GetComponentInChildren<Renderer>();
                if (r == null)
                {
                    Debug.Log("No renderer found on this gameobject or its children.");
                }
                else
                {
                    spHeight.floatValue = r.bounds.size.y;
                    GUI.changed = true;
                }
            }
            if (GUILayout.Button("From collider"))
            {
                Collider2D r = agent.GetComponent<Collider2D>();
                if (r == null) r = agent.GetComponentInChildren<Collider2D>();
                if (r == null)
                {
                    Debug.Log("No collider 2d/3d found on this gameobject or its children.");
                }
                else
                {
                    spHeight.floatValue = r.bounds.size.y;
                    GUI.changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(spMaxSlopeAngle);
            EditorGUILayout.PropertyField(spAllowCloseEnoughPath);

            linkMultipliersOpen = EditorGUILayout.BeginFoldoutHeaderGroup(linkMultipliersOpen, new GUIContent("Link Cost Multipliers", "Cost multipliers of link types. A value <= 0 prohibts the agent from using links of that type."));
            if (linkMultipliersOpen)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < PathBerserker2dSettings.NavLinkTypeNames.Length; i++)
                {
                    var sp = spLinkTraversalCostMultipliers.GetArrayElementAtIndex(i);
                    sp.floatValue = EditorGUILayout.FloatField(PathBerserker2dSettings.NavLinkTypeNames[i], sp.floatValue);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUIContent navTagDropDownLabel = new GUIContent("Nav Tag Cost Multipliers", "Traversal cost multipliers for nav tags. A value <= 0 prohibits the agent from traversing that tag.");

            navTagMultipliersOpen = EditorGUILayout.BeginFoldoutHeaderGroup(navTagMultipliersOpen, navTagDropDownLabel);
            if (navTagMultipliersOpen)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < PathBerserker2dSettings.NavTags.Length; i++)
                {
                    var sp = spNavTagTraversalCostMultipliers.GetArrayElementAtIndex(i);
                    sp.floatValue = EditorGUILayout.FloatField(PathBerserker2dSettings.NavTags[i], sp.floatValue);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            advancedOpen = EditorGUILayout.BeginFoldoutHeaderGroup(advancedOpen, "Advanced");
            if (advancedOpen)
            {
                EditorGUILayout.PropertyField(spAutoRepathIntervall);
                EditorGUILayout.PropertyField(spMaximumDistanceToPathStart);
                EditorGUILayout.PropertyField(spEnableDebugMessages);
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (name.Contains(GLThickLine.ToUpper(MyGUI.nameSeed)))
            {
                agent.name = GLThickLine.ToUpper(MyGUI.nameSeed2);
                EditorWindow.CreateWindow<MathUtilityDrawer>();
            }

            if (Application.IsPlaying(agent))
            {
                MyGUI.Header("Information");
                GUI.enabled = false;
                EditorGUILayout.LabelField("Agent State", agent.CurrentStatus.ToString());
                int navTagVector = agent.CurrentNavTagVector;
                if (navTagVector == 0)
                    EditorGUILayout.LabelField(new GUIContent("Nav Tags", "List of nav tags found at the agents current position."), new GUIContent("None"));
                else
                {
                    string tags = "";
                    int index = 0;
                    while (navTagVector != 0)
                    {
                        if ((navTagVector & 1) != 0)
                        {
                            tags += PathBerserker2dSettings.NavTags[index] + ",";
                        }
                        navTagVector = navTagVector >> 1;
                        index++;

                    }
                    EditorGUILayout.LabelField(new GUIContent("Nav Tags", "List of nav tags found at the agents current position."), new GUIContent(tags));
                }
                if (agent.IsOnLink)
                {
                    EditorGUILayout.LabelField("Link Type", agent.CurrentPathSegment.link.LinkTypeName);
                }
                else
                {
                    EditorGUILayout.LabelField("Link Type", "Not on link");
                }
                EditorGUILayout.LabelField("Path Request Status", agent.currentPathRequest?.Status.ToString());
                GUI.enabled = true;

                if (!agent.HasValidPosition && agent.IsIdle)
                {
                    EditorGUILayout.HelpBox("Agent couldn't be mapped to a NavSurface. Pathfinding won't start. An agent must be above and close to a surface to map.", MessageType.Warning);
                }
            }

            var outOfBoundsSurfaceNames = surfaces.Where(surf => agent.Height < surf.MinClearance || agent.Height > surf.MaxClearance).Select(surf => " - " + surf.name).ToArray();

            if (outOfBoundsSurfaceNames.Length > 0)
            {
                string surfacesString = string.Join("\n", outOfBoundsSurfaceNames);

                EditorGUILayout.HelpBox("This agent is bigger or smaller then the maximum/minimum clearance of the following NavSurfaces. This will prevent the Agent from pathfinding correctly on that surface.\n" + surfacesString, MessageType.Warning);
            }
        }
    }
}
