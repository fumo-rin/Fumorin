using UnityEngine;
using UnityEngine.UIElements;

namespace rinCore
{
    public interface IGroundCheck
    {
        public bool IsGrounded { get; }
    }
    public interface IGizmosDrawable
    {
        public void IDrawGizmos();
        public void IDrawGizmosSelected();
    }
}
