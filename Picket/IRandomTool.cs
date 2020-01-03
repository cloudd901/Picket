using System.Collections.Generic;
using System.Drawing;

namespace Picket
{
    public enum TextType
    {
        NameAndID = 0,
        Name = 1,
        ID = 2,
        None = 3
    }
    public enum ArrowLocation
    {
        Top = 0,
        Right = 1,
        Bottom = 2,
        Left = 3
    }
    public enum PowerType
    {
        Manual = 0,
        Weak = 1,
        Average = 2,
        Strong = 3,
        Super = 4,
        Random = 5,
        Infinite = 6
    }
    public enum Direction
    {
        Clockwise = 0,
        CounterClockwise = 1
    }
    public enum ShadowPosition
    {
        BottomRight = 0,
        BottomLeft = 1,
        TopLeft = 2,
        TopRight = 3
    }
}
