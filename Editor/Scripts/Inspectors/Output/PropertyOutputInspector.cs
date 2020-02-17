﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OscCore
{
    [CustomEditor(typeof(PropertyOutput), true)]
    class PropertyOutputInspector : Editor
    {
        SerializedProperty m_AddressProp;
        SerializedProperty m_SenderProp;
        SerializedProperty m_ObjectProp;
        SerializedProperty m_PropertyNameProp;
        SerializedProperty m_PropertyTypeNameProp;
        SerializedProperty m_SourceComponentProp;

        Component[] m_CachedComponents;
        string[] m_CachedComponentNames;
        string m_PreviousComponentName;
        int m_ComponentIndex;

        PropertyInfo[] m_Properties;
        string[] m_PropertyNames;
        int m_PropertyIndex;

        static readonly HashSet<string> k_SupportedTypes = new HashSet<string>()
        {
            "System.SByte", "System.Byte", "System.Int16", "System.UInt16", "System.Int32", "System.Int64",
            "System.Single", "System.Double", "System.String", "System.Boolean",
            "UnityEngine.Vector2", "UnityEngine.Vector3", "UnityEngine.Color", "UnityEngine.Color32"
        };
        
        PropertyOutput m_Target;
        bool m_ObjectPreviouslyNotNull;
        
        void OnEnable()
        {
            m_Target = target as PropertyOutput;
            m_AddressProp = serializedObject.FindProperty("m_Address");
            m_SenderProp = serializedObject.FindProperty("m_Sender");
            m_ObjectProp = serializedObject.FindProperty("m_Object");
            m_SourceComponentProp = serializedObject.FindProperty("m_SourceComponent");
            m_PropertyNameProp = serializedObject.FindProperty("m_PropertyName");
            m_PropertyTypeNameProp = serializedObject.FindProperty("m_PropertyTypeName");

            if (m_Target == null) return;

            m_CachedComponents = m_Target.GetObjectComponents();
            if (m_CachedComponents == null) return;
            
            m_CachedComponentNames = m_CachedComponents.Select(c => c.GetType().Name).ToArray();
            
            var sourceCompRef = m_SourceComponentProp.objectReferenceValue;
            if (sourceCompRef == null) 
                sourceCompRef = m_SourceComponentProp.objectReferenceValue = m_Target.gameObject;
            
            m_ComponentIndex = Array.IndexOf(m_CachedComponentNames, sourceCompRef.GetType().Name);
            if(m_ComponentIndex >= 0)
                GetComponentProperties();

            if (sourceCompRef != null)
            {
                m_ComponentIndex = Array.IndexOf(m_CachedComponentNames, sourceCompRef.GetType().Name);

                var serializedPropName = m_PropertyNameProp.stringValue;
                if(m_PropertyNames != null)
                    m_PropertyIndex = Array.IndexOf(m_PropertyNames, serializedPropName);
                //Debug.Log($"serialized prop name : {serializedPropName} @ index {m_PropertyIndex}");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("OSC Destination", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_SenderProp);
            EditorGUILayout.PropertyField(m_AddressProp);
            
            EditorGUILayout.LabelField("Property Source", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ObjectProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                var objValue = m_ObjectProp.objectReferenceValue;
                if (objValue == null)
                {
                    CleanComponents();
                    return;
                }

                m_PropertyIndex = -1;
                m_ComponentIndex = -1;
                m_CachedComponents = m_Target.GetObjectComponents();
                if (m_CachedComponents != null)
                    m_CachedComponentNames = m_CachedComponents.Select(c => c.GetType().Name).ToArray();
            }

            ComponentDropdown();
            PropertyDropdown();

            if (m_PropertyIndex >= 0)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Type", m_PropertyTypeNameProp.stringValue, EditorStyles.whiteLabel);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void ComponentDropdown()
        {
            if (m_CachedComponentNames == null) return;
            // TODO - tooltips here
            var newIndex = EditorGUILayout.Popup("Component", m_ComponentIndex, m_CachedComponentNames);
            if (newIndex != m_ComponentIndex)
            {
                m_ComponentIndex = newIndex;
                var compName = m_CachedComponentNames[newIndex];
                if (compName != m_PreviousComponentName)
                    GetComponentProperties();

                m_PropertyIndex = -1;
                m_PreviousComponentName = compName;
                m_PropertyTypeNameProp.stringValue = null;
                m_SourceComponentProp.objectReferenceValue = m_CachedComponents[newIndex];
            }
        }
        
        void PropertyDropdown()
        {
            // TODO - tooltips here
            if (m_PropertyNames == null) return;
            
            var newIndex = EditorGUILayout.Popup("Property", m_PropertyIndex, m_PropertyNames);
            if (newIndex != m_PropertyIndex)
            {
                m_PropertyIndex = newIndex;
                m_PropertyNameProp.stringValue = m_PropertyNames[m_PropertyIndex];

                var info = m_Properties[m_PropertyIndex];
                var type = info.PropertyType;
                m_PropertyTypeNameProp.stringValue = type.Name;
                m_Target.Property = info;
            }
        }

        void GetComponentProperties()
        {
            var comp = m_CachedComponents[m_ComponentIndex];
            var properties = comp.GetType().GetProperties();
            m_Properties = properties.Where(p => k_SupportedTypes.Contains(p.PropertyType.FullName)).ToArray();
            m_PropertyNames = m_Properties.Select(m => m.Name).ToArray();
        }

        void CleanComponents()
        {
            m_CachedComponents = null;
            m_CachedComponentNames = null;
            m_Properties = null;
            m_PropertyNames = null;
            m_ComponentIndex = -1;
            m_PropertyIndex = -1;
            m_PreviousComponentName = null;
            m_PropertyTypeNameProp.stringValue = null;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
