using Microsoft.UI.Input;

namespace SalmonEgg.Controls;

public sealed partial class ResizeGrip
{
    partial void ApplyPlatformCursor()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
