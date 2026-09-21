using UnityEngine;

namespace rinCore
{
    public class UINestDefaultObject : MonoBehaviour, IUINestRunable
    {
        /// <summary>
        /// you might expect this to be max value, 
        /// but being the last object in the queue,
        /// it will run last with the min value instead.
        /// Effectively queuing up the object on top of the frame.
        /// </summary>
        public int RunnerPriority => int.MaxValue;
        public void RunNestComponent(UINest nest)
        {
            new FEB_EventSystem_SelectBuffered(gameObject).Publish();
        }
    }
}
