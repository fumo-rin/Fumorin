using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace rinCore
{
    public interface ITagTile
    {
        public string Tag { get; }
        public bool IsOfTag(params string[] other)
        {
            foreach (var item in other)
            {
                if (string.Intern(item).Equals(Tag))
                    return true;
            }
            return false;
        }
    }
    [CreateAssetMenu(menuName = "rinCore/Tile/Sibling Ruletile")]
    public class SiblingTile : RuleTile, ITagTile
    {
        [field: SerializeField] public string Tag { get; private set; } = "";
        [SerializeField] RuleTile[] siblingTiles = Array.Empty<RuleTile>();
        public override bool RuleMatch(int neighbor, TileBase other)
        {
            bool isSibling = IsSibling(other);
            return neighbor switch
            {
                TilingRuleOutput.Neighbor.This => isSibling,
                TilingRuleOutput.Neighbor.NotThis => !isSibling,
                _ => base.RuleMatch(neighbor, other)
            };
        }
        private bool IsSibling(TileBase other)
        {
            if (other == this) return true;
            if (other == null || siblingTiles == null) return false;
            ReadOnlySpan<RuleTile> siblings = siblingTiles;
            for (int i = 0; i < siblings.Length; i++)
            {
                if (siblings[i] == other) return true;
            }
            return false;
        }
    }
}