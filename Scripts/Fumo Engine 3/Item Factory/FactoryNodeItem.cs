using UnityEngine;

namespace rinCore
{
    public interface IFumoItem_Factory
    {
        public void ProcessItem(FumoItemPacket packet, FactoryNode nextNode);
    }
    public class FactoryNodeItem : FumoItem, IFumoItem_Factory
    {
        public void ProcessItem(FumoItemPacket packet, FactoryNode nextNode)
        {

        }
    }
}