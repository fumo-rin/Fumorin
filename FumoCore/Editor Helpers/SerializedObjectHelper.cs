using UnityEditor;
using UnityEngine;

namespace FumoCore.Tools
{
#if UNITY_EDITOR
    public static partial class FCEHelper
    {
        public static void CopySerializedProperties(SerializedObject source, SerializedObject destination)
        {
            var sourceProp = source.GetIterator();
            bool enterChildren = true;

            while (sourceProp.NextVisible(enterChildren))
            {
                enterChildren = false;
                var destProp = destination.FindProperty(sourceProp.name);
                if (destProp != null)
                {
                    switch (sourceProp.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            destProp.intValue = sourceProp.intValue;
                            break;
                        case SerializedPropertyType.Boolean:
                            destProp.boolValue = sourceProp.boolValue;
                            break;
                        case SerializedPropertyType.Float:
                            destProp.floatValue = sourceProp.floatValue;
                            break;
                        case SerializedPropertyType.String:
                            destProp.stringValue = sourceProp.stringValue;
                            break;
                        case SerializedPropertyType.Color:
                            destProp.colorValue = sourceProp.colorValue;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            destProp.objectReferenceValue = sourceProp.objectReferenceValue;
                            break;
                        case SerializedPropertyType.Enum:
                            destProp.enumValueIndex = sourceProp.enumValueIndex;
                            break;
                        case SerializedPropertyType.Vector2:
                            destProp.vector2Value = sourceProp.vector2Value;
                            break;
                        case SerializedPropertyType.Vector3:
                            destProp.vector3Value = sourceProp.vector3Value;
                            break;
                        case SerializedPropertyType.Vector4:
                            destProp.vector4Value = sourceProp.vector4Value;
                            break;
                        case SerializedPropertyType.Rect:
                            destProp.rectValue = sourceProp.rectValue;
                            break;
                        case SerializedPropertyType.AnimationCurve:
                            destProp.animationCurveValue = sourceProp.animationCurveValue;
                            break;
                        case SerializedPropertyType.Bounds:
                            destProp.boundsValue = sourceProp.boundsValue;
                            break;
                        case SerializedPropertyType.Quaternion:
                            destProp.quaternionValue = sourceProp.quaternionValue;
                            break;
                    }
                }
            }
            destination.ApplyModifiedProperties();
        }
    }
#endif
}
