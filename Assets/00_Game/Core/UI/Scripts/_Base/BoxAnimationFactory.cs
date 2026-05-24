public enum BoxAnimationType
{
    Scale,
    NoAnim
}

public static class BoxAnimationFactory
{
    public static readonly IShowAnimation Scale = new ScaleAnim();
    public static readonly IShowAnimation NoAnim = new NoAnim();
    public static readonly IShowAnimation SlideFromLeft  = new SlideAnim(SlideAnim.Direction.FromLeft);
    public static readonly IShowAnimation SlideFromRight = new SlideAnim(SlideAnim.Direction.FromRight);
    public static readonly IShowAnimation SlideToLeft    = new SlideAnim(SlideAnim.Direction.ToLeft);
    public static readonly IShowAnimation SlideToRight   = new SlideAnim(SlideAnim.Direction.ToRight);

    public static IShowAnimation Get(BoxAnimationType type) => type switch
    {
        BoxAnimationType.Scale  => Scale,
        BoxAnimationType.NoAnim => NoAnim,
        _ => Scale
    };
}