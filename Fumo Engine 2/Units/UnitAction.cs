using System;
using UnityEngine;

namespace Fumorin.Units
{
    [System.Serializable]
    public abstract class UnitAction
    {
        public enum ActionResult
        {
            Cancelled,
            Stall,
            Performed,
            End
        }
        public float duration;
        protected FumoUnit Owner;
        public abstract bool IsRunning();
        public UnitAction()
        {

        }
        public UnitAction(FumoUnit owner, float duration)
        {
            Owner = owner;
            this.duration = duration;
        }
        public void SetNewOwner(FumoUnit NewOwner)
        {
            Owner = NewOwner;
        }
        public ActionResult PerformAction()
        {
            return RunAction(Owner);
        }
        protected abstract ActionResult RunAction(FumoUnit Owner);
    }
}
