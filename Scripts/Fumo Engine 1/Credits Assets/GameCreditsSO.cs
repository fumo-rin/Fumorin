using UnityEngine;
using System.Collections.Generic;
using RinCore;
using UnityEditor;
namespace RinCore
{
#if UNITY_EDITOR
    public partial class GameCreditsSO
    {
        public void AddCredit(CreditsSO c)
        {
            credits.Add(c);
            AssetDatabase.SaveAssets();
        }
    }
#endif
    [CreateAssetMenu(menuName = "Bremsengine/Credits", fileName = "New Game Credits")]
    public partial class GameCreditsSO : ScriptableObject
    {
        [SerializeField] public string gameName;
        [SerializeField] public string gameCreator;
        [SerializeField] List<CreditsSO> credits = new();
        [SerializeField] bool showTitle;
        [SerializeField] bool doubleSpacing;
        [SerializeField] string spacingOverride = "";
        public string CompileCredits()
        {
            credits.Sort(CreditsSO.SortByPriority);
            string spam = "";
            if (showTitle) spam += $"{gameName}##by {gameCreator}####".ReplaceLineBreaks("##");
            if (string.IsNullOrWhiteSpace(spacingOverride))
            {
                foreach (var c in credits)
                {
                    spam += c.ToString();
                    spam += "##";
                    if (doubleSpacing)
                    {
                        spam += "##";
                    }
                }
                return spam.ReplaceLineBreaks("##");
            }
            else
            {
                foreach (var c in credits)
                {
                    spam += c.ToString();
                    spam += spacingOverride;
                    if (doubleSpacing)
                    {
                        spam += spacingOverride;
                    }
                }
                return spam;
            }
        }
    }
}