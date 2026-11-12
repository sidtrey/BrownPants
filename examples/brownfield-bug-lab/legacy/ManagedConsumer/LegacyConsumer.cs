namespace StlClr_Sample
{
    public sealed class LegacyConsumer
    {
        public void Sort()
        {
            StlClrLibrary.Tools tools = new StlClrLibrary.Tools();
            tools.BubbleSortNative();
        }
    }
}
