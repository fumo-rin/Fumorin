using UnityEngine;

namespace rinCore
{
    public static partial class RinHelper
    {
        public static bool ValidGameObjects(params Component[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null || obj.gameObject == null)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool ValidGameObjects(params GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null || obj.gameObject == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
