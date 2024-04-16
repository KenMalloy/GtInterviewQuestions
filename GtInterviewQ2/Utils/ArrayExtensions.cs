namespace GtInterviewQ2.Utils
{
    public static class ArrayExtensions
    {
        public static void GenerateSpan(this int[] array, int index, int length, int valueOffset)
        {
            int endIndex = index + length;
            for (int i = index, j = index + valueOffset; i < endIndex; i++, j++)
                array[i] = j;
        }
    }
}
