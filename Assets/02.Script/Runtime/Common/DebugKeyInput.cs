using UnityEngine;
using UnityEngine.InputSystem;

public static class DebugKeyInput
{
    public static bool GetKey(KeyCode keyCode)
    {
        Key? mappedKey = MapKey(keyCode);
        if (mappedKey == null || Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current[mappedKey.Value].isPressed;
    }

    public static bool GetKeyDown(KeyCode keyCode)
    {
        Key? mappedKey = MapKey(keyCode);
        if (mappedKey == null || Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current[mappedKey.Value].wasPressedThisFrame;
    }

    private static Key? MapKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Alpha0: return Key.Digit0;
            case KeyCode.Alpha1: return Key.Digit1;
            case KeyCode.Alpha2: return Key.Digit2;
            case KeyCode.Alpha3: return Key.Digit3;
            case KeyCode.Alpha4: return Key.Digit4;
            case KeyCode.Alpha5: return Key.Digit5;
            case KeyCode.Alpha6: return Key.Digit6;
            case KeyCode.Alpha7: return Key.Digit7;
            case KeyCode.Alpha8: return Key.Digit8;
            case KeyCode.Alpha9: return Key.Digit9;
            case KeyCode.Q: return Key.Q;
            case KeyCode.W: return Key.W;
            case KeyCode.E: return Key.E;
            case KeyCode.R: return Key.R;
            case KeyCode.T: return Key.T;
            case KeyCode.Minus: return Key.Minus;
            case KeyCode.LeftShift: return Key.LeftShift;
            case KeyCode.F1: return Key.F1;
            case KeyCode.F2: return Key.F2;
            case KeyCode.F3: return Key.F3;
            case KeyCode.F4: return Key.F4;
            case KeyCode.F5: return Key.F5;
            case KeyCode.F6: return Key.F6;
            case KeyCode.F7: return Key.F7;
            case KeyCode.F8: return Key.F8;
            case KeyCode.F9: return Key.F9;
            case KeyCode.F10: return Key.F10;
            case KeyCode.F11: return Key.F11;
            case KeyCode.F12: return Key.F12;
            default: return null;
        }
    }
}
