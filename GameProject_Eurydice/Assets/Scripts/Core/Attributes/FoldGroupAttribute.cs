using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class FoldGroupAttribute : PropertyAttribute
{
    public string GroupName { get; }
    public bool DefaultExpanded { get; }
    public bool FoldEverything { get; }

    public FoldGroupAttribute(string groupName, bool defaultExpanded = true, bool foldEverything = true)
    {
        GroupName = groupName;
        DefaultExpanded = defaultExpanded;
        FoldEverything = foldEverything;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class EndFoldGroupAttribute : PropertyAttribute
{
    public EndFoldGroupAttribute() { }
}
