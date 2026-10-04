#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
[CanEditMultipleObjects]
public class FoldGroupMonoBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (!FoldGroupEditorUtility.HasFoldGroups(target.GetType()))
        {
            DrawDefaultInspector();
            return;
        }

        FoldGroupEditorUtility.DrawInspector(serializedObject);
    }
}

[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
[CanEditMultipleObjects]
public class FoldGroupScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (!FoldGroupEditorUtility.HasFoldGroups(target.GetType()))
        {
            DrawDefaultInspector();
            return;
        }

        FoldGroupEditorUtility.DrawInspector(serializedObject);
    }
}

public static class FoldGroupEditorUtility
{
    public class FieldGroupInfo
    {
        public string GroupName;
        public bool DefaultExpanded;
    }

    public class TypeGroupCache
    {
        public bool HasFoldGroups;
        public Dictionary<string, FieldGroupInfo> FieldGroups = new Dictionary<string, FieldGroupInfo>();
        public Dictionary<string, Type> FieldTypes = new Dictionary<string, Type>();
    }

    private static readonly Dictionary<Type, TypeGroupCache> Cache = new Dictionary<Type, TypeGroupCache>();

    public static bool HasFoldGroups(Type type)
    {
        if (type == null) return false;
        return GetCache(type).HasFoldGroups;
    }

    public static TypeGroupCache GetCache(Type type)
    {
        if (type == null) return new TypeGroupCache();

        if (Cache.TryGetValue(type, out var cached))
            return cached;

        var newCache = new TypeGroupCache();
        var types = new List<Type>();
        Type cur = type;

        while (cur != null && cur != typeof(MonoBehaviour) && cur != typeof(ScriptableObject) && cur != typeof(UnityEngine.Object))
        {
            types.Insert(0, cur);
            cur = cur.BaseType;
        }

        string currentGroup = null;
        bool currentDefaultExpanded = true;
        bool foldEverything = true;

        foreach (var t in types)
        {
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var f in fields)
            {
                newCache.FieldTypes[f.Name] = f.FieldType;

                var foldAttr = f.GetCustomAttribute<FoldGroupAttribute>();
                var endAttr = f.GetCustomAttribute<EndFoldGroupAttribute>();

                if (foldAttr != null)
                {
                    newCache.HasFoldGroups = true;
                    currentGroup = foldAttr.GroupName;
                    currentDefaultExpanded = foldAttr.DefaultExpanded;
                    foldEverything = foldAttr.FoldEverything;

                    newCache.FieldGroups[f.Name] = new FieldGroupInfo
                    {
                        GroupName = currentGroup,
                        DefaultExpanded = currentDefaultExpanded
                    };
                }
                else if (endAttr != null)
                {
                    currentGroup = null;
                }
                else if (!string.IsNullOrEmpty(currentGroup) && foldEverything)
                {
                    newCache.FieldGroups[f.Name] = new FieldGroupInfo
                    {
                        GroupName = currentGroup,
                        DefaultExpanded = currentDefaultExpanded
                    };
                }
            }
        }

        Cache[type] = newCache;
        return newCache;
    }

    public static Type GetFieldType(Type parentType, string fieldName)
    {
        if (parentType == null) return null;
        var cache = GetCache(parentType);
        if (cache.FieldTypes.TryGetValue(fieldName, out var ft))
            return ft;
        return null;
    }

    public static Type GetElementType(Type type)
    {
        if (type == null) return null;
        if (type.IsArray) return type.GetElementType();
        if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            return type.GetGenericArguments()[0];
        return null;
    }

    public static void DrawInspector(SerializedObject serializedObject)
    {
        serializedObject.Update();

        Type targetType = serializedObject.targetObject.GetType();
        var cache = GetCache(targetType);

        SerializedProperty iterator = serializedObject.GetIterator();
        bool hasNext = iterator.NextVisible(true);

        string activeGroup = null;
        bool isGroupExpanded = true;
        bool inGroupBlock = false;

        while (hasNext)
        {
            if (iterator.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(iterator);
                }
                hasNext = iterator.NextVisible(false);
                continue;
            }

            cache.FieldGroups.TryGetValue(iterator.name, out FieldGroupInfo groupInfo);
            string fieldGroup = groupInfo != null ? groupInfo.GroupName : null;

            if (fieldGroup != activeGroup)
            {
                if (inGroupBlock)
                {
                    EndGroupBlock();
                    inGroupBlock = false;
                }

                activeGroup = fieldGroup;

                if (!string.IsNullOrEmpty(activeGroup))
                {
                    bool defaultExpanded = groupInfo != null && groupInfo.DefaultExpanded;
                    isGroupExpanded = GetFoldState(targetType, activeGroup, defaultExpanded);

                    bool newExpanded = DrawHeader(activeGroup, isGroupExpanded);
                    if (newExpanded != isGroupExpanded)
                    {
                        isGroupExpanded = newExpanded;
                        SetFoldState(targetType, activeGroup, isGroupExpanded);
                    }

                    if (isGroupExpanded)
                    {
                        BeginGroupBlock();
                        inGroupBlock = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(activeGroup) || isGroupExpanded)
            {
                Type fieldType = GetFieldType(targetType, iterator.name);
                DrawPropertyDispatch(iterator, fieldType);
            }

            hasNext = iterator.NextVisible(false);
        }

        if (inGroupBlock)
        {
            EndGroupBlock();
        }

        serializedObject.ApplyModifiedProperties();
    }

    public static void DrawPropertyDispatch(SerializedProperty prop, Type fieldType)
    {
        if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
        {
            Type elemType = GetElementType(fieldType);
            if (elemType != null && HasFoldGroups(elemType))
            {
                DrawListWithFoldGroups(prop, elemType);
                return;
            }
        }
        else if (fieldType != null && HasFoldGroups(fieldType))
        {
            DrawClassWithFoldGroups(prop, fieldType);
            return;
        }

        EditorGUILayout.PropertyField(prop, true);
    }

    private static void DrawListWithFoldGroups(SerializedProperty listProp, Type elemType)
    {
        listProp.isExpanded = EditorGUILayout.Foldout(listProp.isExpanded, $"{listProp.displayName} (Count: {listProp.arraySize})", true, EditorStyles.foldoutHeader);
        if (!listProp.isExpanded) return;

        EditorGUI.indentLevel++;
        int newSize = EditorGUILayout.IntField("Size", listProp.arraySize);
        if (newSize != listProp.arraySize && newSize >= 0)
        {
            listProp.arraySize = newSize;
        }

        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
            string elemLabel = $"Element {i}";

            SerializedProperty nameProp = elementProp.FindPropertyRelative("pieceTransform") 
                                       ?? elementProp.FindPropertyRelative("target") 
                                       ?? elementProp.FindPropertyRelative("name");
            if (nameProp != null)
            {
                if (nameProp.propertyType == SerializedPropertyType.ObjectReference && nameProp.objectReferenceValue != null)
                    elemLabel = $"Element {i} ({nameProp.objectReferenceValue.name})";
                else if (nameProp.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(nameProp.stringValue))
                    elemLabel = $"Element {i} ({nameProp.stringValue})";
            }

            EditorGUILayout.Space(2f);
            elementProp.isExpanded = EditorGUILayout.Foldout(elementProp.isExpanded, elemLabel, true, EditorStyles.boldLabel);

            if (elementProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawChildrenWithFoldGroups(elementProp, elemType);
                EditorGUI.indentLevel--;
            }
        }
        EditorGUI.indentLevel--;
    }

    private static void DrawClassWithFoldGroups(SerializedProperty classProp, Type classType)
    {
        classProp.isExpanded = EditorGUILayout.Foldout(classProp.isExpanded, classProp.displayName, true, EditorStyles.boldLabel);
        if (!classProp.isExpanded) return;

        EditorGUI.indentLevel++;
        DrawChildrenWithFoldGroups(classProp, classType);
        EditorGUI.indentLevel--;
    }

    public static void DrawChildrenWithFoldGroups(SerializedProperty parentProp, Type parentType)
    {
        var cache = GetCache(parentType);
        SerializedProperty iterator = parentProp.Copy();
        SerializedProperty endProp = parentProp.GetEndProperty();

        bool enterChildren = true;
        string activeGroup = null;
        bool isGroupExpanded = true;
        bool inGroupBlock = false;

        while (iterator.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(iterator, endProp))
                break;

            enterChildren = false;

            cache.FieldGroups.TryGetValue(iterator.name, out FieldGroupInfo groupInfo);
            string fieldGroup = groupInfo != null ? groupInfo.GroupName : null;

            if (fieldGroup != activeGroup)
            {
                if (inGroupBlock)
                {
                    EndGroupBlock();
                    inGroupBlock = false;
                }

                activeGroup = fieldGroup;

                if (!string.IsNullOrEmpty(activeGroup))
                {
                    bool defaultExpanded = groupInfo != null && groupInfo.DefaultExpanded;
                    string prefsKey = $"{parentProp.propertyPath}_{activeGroup}";
                    isGroupExpanded = GetFoldState(parentType, prefsKey, defaultExpanded);

                    bool newExpanded = DrawHeader(activeGroup, isGroupExpanded);
                    if (newExpanded != isGroupExpanded)
                    {
                        isGroupExpanded = newExpanded;
                        SetFoldState(parentType, prefsKey, isGroupExpanded);
                    }

                    if (isGroupExpanded)
                    {
                        BeginGroupBlock();
                        inGroupBlock = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(activeGroup) || isGroupExpanded)
            {
                Type childFieldType = GetFieldType(parentType, iterator.name);
                DrawPropertyDispatch(iterator, childFieldType);
            }
        }

        if (inGroupBlock)
        {
            EndGroupBlock();
        }
    }

    private static void BeginGroupBlock()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.indentLevel++;
        EditorGUILayout.Space(2f);
    }

    private static void EndGroupBlock()
    {
        EditorGUILayout.Space(2f);
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    private static bool DrawHeader(string title, bool isExpanded)
    {
        EditorGUILayout.Space(3f);
        Rect rect = GUILayoutUtility.GetRect(16f, 22f, GUILayout.ExpandWidth(true));
        Rect indented = EditorGUI.IndentedRect(rect);

        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = isExpanded ? new Color(0.85f, 0.92f, 1f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
        GUI.Box(indented, GUIContent.none, EditorStyles.toolbarButton);
        GUI.backgroundColor = prevColor;

        Rect foldoutRect = new Rect(indented.x + 8f, indented.y + 2f, indented.width - 16f, indented.height - 4f);
        bool newExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, title, true, EditorStyles.boldLabel);
        return newExpanded;
    }

    private static string GetPrefsKey(Type type, string identifier)
    {
        return $"FoldGroup_{type.FullName}_{identifier}";
    }

    private static bool GetFoldState(Type type, string identifier, bool defaultExpanded)
    {
        return EditorPrefs.GetBool(GetPrefsKey(type, identifier), defaultExpanded);
    }

    private static void SetFoldState(Type type, string identifier, bool isExpanded)
    {
        EditorPrefs.SetBool(GetPrefsKey(type, identifier), isExpanded);
    }
}
#endif
