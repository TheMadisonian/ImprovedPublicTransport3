using PublicTransportUnstucker;

namespace PublicTransportUnstucker
{
    internal static class PublicTransportUnstuckerIntegration
    {
        public static void Activate()
        {
            RoguePassengerTable.EnsureTableExists();
            PatchController.Activate();
        }

        public static void Deactivate()
        {
            PatchController.Deactivate();
            RoguePassengerTable.WipeTable();
        }
    }
}
