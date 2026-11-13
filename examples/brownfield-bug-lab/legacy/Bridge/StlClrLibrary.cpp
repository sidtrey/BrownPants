namespace Native { void BubbleSort(); }
void Native::BubbleSort() { }



namespace StlClrLibrary
{
    public ref class Tools
    {
    public:
        void BubbleSortNative()
        {
            Native::BubbleSort();
        }
    };
}
