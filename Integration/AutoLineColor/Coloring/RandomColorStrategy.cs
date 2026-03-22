namespace AutoLineColor.Coloring
{
    public sealed class RandomColorStrategy : ColorStrategyBase
    {
        protected override IColorSetProvider GetColorSetProvider()
        {
            return ColorSetProvider.Random;
        }

        protected override IColorSelector GetColorSelector()
        {
            return ColorSelector.DifferenceThreshold;
        }
    }
}