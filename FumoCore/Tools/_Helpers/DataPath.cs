using UnityEngine;

namespace RinCore
{
    public static partial class Helper
    {
        public static string DataPath =>
#if UNITY_EDITOR
            Application.dataPath;
#else
            Application.persistentDataPath;
#endif
    }
}
