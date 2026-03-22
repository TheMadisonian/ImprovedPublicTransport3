namespace AutoLineColor.Coloring
{
    public sealed class CategorisedColorStrategy : ColorStrategyBase
    {
        protected override IColorSetProvider GetColorSetProvider()
        {
            return ColorSetProvider.Categorised;
        }

        protected override IColorSelector GetColorSelector()
        {
            return ColorSelector.LeastUsed;
        }
    }
}
