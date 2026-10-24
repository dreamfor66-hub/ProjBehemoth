using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable] public sealed class DataTextBinding { public string propertyPath; public string key; [HideInInspector] public string lastText; }
    public abstract class LocalizedData : ScriptableObject
    {
        [HideInInspector] public List<DataTextBinding> textKeys = new();
        [HideInInspector] public string textOwnerGuid;
        // Runtime values are materialized from stable keys, while Inspector fields remain directly editable.
        protected virtual void OnEnable()
        {
            if (Application.isEditor && !Application.isPlaying) return;
            var table = GameText.Database; if (table == null) return;
            foreach (var binding in textKeys)
            {
                if (!table.Contains(binding.key)) continue;
                try
                {
                    object target = this; var path = binding.propertyPath.Replace(".Array.data[", "[").Split('.');
                    for (var i = 0; i < path.Length; i++)
                    {
                        var component = path[i]; var bracket = component.IndexOf('['); var fieldName = bracket < 0 ? component : component.Substring(0, bracket);
                        var field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field == null) break;
                        if (i == path.Length - 1) { field.SetValue(target, table.Get(binding.key)); break; }
                        target = field.GetValue(target);
                        if (bracket >= 0) target = ((IList)target)[int.Parse(component.Substring(bracket + 1).TrimEnd(']'))];
                    }
                }
                catch (Exception) { /* A designer may have removed an indexed choice; editor synchronization prunes its binding. */ }
            }
        }
    }
}
