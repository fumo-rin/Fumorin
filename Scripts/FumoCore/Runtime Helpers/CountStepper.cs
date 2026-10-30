using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    public class CountStepper : IEnumerable<int>
    {
        public int Pool;
        public int LoopCount;
        public int Step;
        public bool IsDepleted => Step >= Pool;
        public CountStepper(int pool, int loopCount)
        {
            this.Step = 0;
            this.Pool = pool;
            this.LoopCount = loopCount;
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
            Step = 0;
            return this;
        }
        public IEnumerable<int> ForeachOnce
        {
            get
            {
                int steps = LoopCount;
                while (steps > 0 && !IsDepleted)
                {
                    Step++;
                    steps--;

                    yield return Step;
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
                    Step++;
                    steps--;

                    yield return Step;
                }
            }
        }
    }
}
