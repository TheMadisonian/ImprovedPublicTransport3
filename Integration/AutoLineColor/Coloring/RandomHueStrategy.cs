namespace AutoLineColor.Coloring
{
    public sealed class RandomHueStrategy : ColorStrategyBase
    {
        protected override IColorSetProvider GetColorSetProvider()
        {
            return ColorSetProvider.RandomHue;
        }

        protected override IColorSelector GetColorSelector()
        {
            return ColorSelector.DifferenceThreshold;
        }
    }
}