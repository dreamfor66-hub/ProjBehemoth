using UnityEditor;
using UnityEngine;

namespace DungeonSlash.EditorTools
{
    [CustomEditor(typeof(MonsterAttackData))]
    [CanEditMultipleObjects]
    public sealed class MonsterAttackDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Draw("type");
            Draw("displayName");
            Draw("damage");
            Draw("shieldDamage");
            Draw("windupDuration");
            Draw("recoveryDuration");
            Draw("guardable");

            var isCharge = targets.Length == 1 && ((MonsterAttackData)target).type == MonsterAttackType.Charge;
            if (isCharge)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Charge", EditorStyles.boldLabel);
                Draw("chargeTimeLimit");
                Draw("chargeWeakPoints", includeChildren: true);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Charge Summon Mechanics", EditorStyles.boldLabel);
                Draw("chargeSummonMechanics", includeChildren: true);
            }
            else if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Charge-specific fields are shown only when one Charge attack is selected.", MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Combat Mechanics", EditorStyles.boldLabel);
            Draw("combatMechanics", includeChildren: true);

            serializedObject.ApplyModifiedProperties();
        }

        private void Draw(string propertyName, bool includeChildren = false)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null) EditorGUILayout.PropertyField(property, includeChildren);
        }
    }
}
