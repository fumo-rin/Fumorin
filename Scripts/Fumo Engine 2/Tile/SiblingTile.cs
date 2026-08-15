using UnityEngine;
using UnityEngine.Tilemaps;

namespace rinCore
{
    [CreateAssetMenu(menuName = "rinCore/Tile/Sibling Ruletile")]
    public class SiblingTile : RuleTile
    {
        public RuleTile[] siblingTiles;
        public override bool RuleMatch(int neighbor, TileBase other)
        {
            bool isSibling = false;
            if (other == this)
            {
                isSibling = true;
            }
            else if (siblingTiles != null)
            {
                for (int i = 0; i < siblingTiles.Length; i++)
                {
                    if (other == siblingTiles[i])
                    {
                        isSibling = true;
                        break;
                    }
                }
            }
            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This:
                    return isSibling;

                case TilingRuleOutput.Neighbor.NotThis:
                    return !isSibling;
            }
            return base.RuleMatch(neighbor, other);
        }
    }
}