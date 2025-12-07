using UnityEngine;
namespace FumoCore.Tools
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