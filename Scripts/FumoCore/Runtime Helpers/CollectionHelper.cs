using UnityEngine;
namespace rinCore
{
    #region Create Array
    public static partial class FCHelper
    {
        public static T[] CreateArray<T>(params T[] items)
        {
            return items;
        }
    }
    #endregion
}