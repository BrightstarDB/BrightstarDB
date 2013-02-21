namespace BrightstarDB
{
    public class Tuple<T1, T2>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public Tuple(T1 first, T2 second)
        {
            Item1 = first;
            Item2 = second;
        }
    }

    public class Tuple<T1,T2,T3,T4>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public T3 Item3 { get; private set; }
        public T4 Item4 { get; private set; }

        public Tuple(T1 first, T2 second, T3 third, T4 fourth)
        {
            Item1 = first;
            Item2 = second;
            Item3 = third;
            Item4 = fourth;
        }
    }
}
