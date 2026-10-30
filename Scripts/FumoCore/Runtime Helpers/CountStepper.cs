using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    public record CountStepper : IEnumerable<int>
    {
        public int Pool;
        public int CycleCount;
        public int CurrentStep;
        public bool IsDepleted => CurrentStep >= Pool;
        public CountStepper(int pool, float loopCount)
        {
            this.CurrentStep = 0;
            this.Pool = pool;
            this.CycleCount = loopCount.Floor().ToInt().Clamp(1, int.MaxValue);
        }
        #region Enumerator
        public IEnumerator<int> GetEnumerator()
        {
            foreach (var item in ForeachOnce)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
        public CountStepper Restart()
        {
            CurrentStep = 0;
            return this;
        }
        public IEnumerable<int> ForeachOnce
        {
            get
            {
                int steps = CycleCount;
                while (steps > 0 && !IsDepleted)
                {
                    yield return CurrentStep;

                    CurrentStep++;
                    steps--;
                }
            }
        }
        public IEnumerable<int> ForeachPool
        {
            get
            {
                int steps = Pool;
                while (steps > 0 && !IsDepleted)
                {
                    yield return CurrentStep;

                    CurrentStep++;
                    steps--;
                }
            }
        }
    }
}
