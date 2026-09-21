using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    [DefaultExecutionOrder(-100), System.Serializable]
    public class ActionFrameSender_STG : IFumoUnit_STG_PlayerActionFrame
    {
        STG_Frame_Action action;
        bool wasShooting, wasFocused;

        [SerializeField] InputActionReference stgMove, shoot, focus, hyper, bomb, extra1, extra2, extra3;

        public FEB_STG_ActionFrame Frame => new(STG_Action);
        public STG_Frame_Action STG_Action => action;

        public void ActionFrame(out FEB_STG_ActionFrame frame)
        {
            action = STG_Frame_Action.None;

            Vector2 input = stgMove.ReadRawVector2(true).QuantizeToStepSize(45f);
            action = action.SetMatch(STG_Frame_Action.Right, input.x > 0f);
            action = action.SetMatch(STG_Frame_Action.Left, input.x < 0f);
            action = action.SetMatch(STG_Frame_Action.Up, input.y > 0f);
            action = action.SetMatch(STG_Frame_Action.Down, input.y < 0f);

            bool shooting = shoot.IsPressed();
            action = action.SetMatch(STG_Frame_Action.ShootHeld, shooting);
            action = action.SetMatch(STG_Frame_Action.ShootStarted, shooting && !wasShooting);

            if (!shooting && wasShooting)
                action = action.SetMatch(STG_Frame_Action.ShootEnd, true);
            wasShooting = shooting;

            bool focused = focus.PressedLongerThan(0.15f);
            action = action.SetMatch(STG_Frame_Action.FocusHeld, focus.IsPressed());
            action = action.SetMatch(STG_Frame_Action.FocusProcessed, focused);
            action = action.SetMatch(STG_Frame_Action.FocusStarted, !wasFocused && focused);

            if (!focused && wasFocused)
                action = action.SetMatch(STG_Frame_Action.FocusEnd, true);
            wasFocused = focused;

            action = action.SetMatch(STG_Frame_Action.BombPressed, bomb.IsPressed());
            action = action.SetMatch(STG_Frame_Action.HyperPressed, hyper.IsPressed());
            action = action.SetMatch(STG_Frame_Action.Extra1, extra1.IsPressed());
            action = action.SetMatch(STG_Frame_Action.Extra2, extra2.IsPressed());
            action = action.SetMatch(STG_Frame_Action.Extra3, extra3.IsPressed());

            frame = Frame;
        }
    }
}