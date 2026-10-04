using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    [DefaultExecutionOrder(-100), System.Serializable]
    public class ActionFrameSender_STG : IFumoUnit_STG_PlayerActionFrame
    {
        STG_ActionFrame action;
        bool wasShooting, wasFocused;

        [SerializeField] InputActionReference stgMove, shoot, focus, hyper, bomb, extra1, extra2, extra3;

        public FEB_STG_ActionFrame Frame => new(STG_Action);
        public STG_ActionFrame STG_Action => action;

        public void ActionFrame(out FEB_STG_ActionFrame frame)
        {
            action = STG_ActionFrame.None;

            Vector2 input = stgMove.ReadRawVector2(true).QuantizeToStepSize(45f);
            action = action.SetMatch(STG_ActionFrame.Right, input.x > 0f);
            action = action.SetMatch(STG_ActionFrame.Left, input.x < 0f);
            action = action.SetMatch(STG_ActionFrame.Up, input.y > 0f);
            action = action.SetMatch(STG_ActionFrame.Down, input.y < 0f);

            bool shooting = shoot.IsPressed();
            action = action.SetMatch(STG_ActionFrame.ShootHeld, shooting);
            action = action.SetMatch(STG_ActionFrame.ShootStarted, shooting && !wasShooting);

            if (!shooting && wasShooting)
                action = action.SetMatch(STG_ActionFrame.ShootEnd, true);
            wasShooting = shooting;

            bool focused = focus.PressedLongerThan(0.15f);
            action = action.SetMatch(STG_ActionFrame.FocusHeld, focus.IsPressed());
            action = action.SetMatch(STG_ActionFrame.FocusProcessed, focused);
            action = action.SetMatch(STG_ActionFrame.FocusStarted, !wasFocused && focused);

            if (!focused && wasFocused)
                action = action.SetMatch(STG_ActionFrame.FocusEnd, true);
            wasFocused = focused;

            action = action.SetMatch(STG_ActionFrame.BombPressed, bomb.IsPressed());
            action = action.SetMatch(STG_ActionFrame.HyperPressed, hyper.IsPressed());
            action = action.SetMatch(STG_ActionFrame.Extra1, extra1.IsPressed());
            action = action.SetMatch(STG_ActionFrame.Extra2, extra2.IsPressed());
            action = action.SetMatch(STG_ActionFrame.Extra3, extra3.IsPressed());

            frame = Frame;
        }
    }
}