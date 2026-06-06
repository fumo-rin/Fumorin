using UnityEngine;

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
