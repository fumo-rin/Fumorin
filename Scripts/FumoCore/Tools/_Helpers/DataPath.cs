using UnityEngine;

namespace rinCore
{
    public static partial class RinHelper
    {
        public static string DataPath =>
#if UNITY_EDITOR
            Application.dataPath;
#else
            Application.persistentDataPath;
#endif
    }
}
