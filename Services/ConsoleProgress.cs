public static class ConsoleProgress
{
    public static void DisplayProgressBar(int current, int total)
    {
        int barWidth = 50;
        double progress = (double)current / total;
        int filledLength = (int)(progress * barWidth);
        
        Console.CursorLeft = 0;
        Console.Write("["); 

        for (int i = 0; i < filledLength; i++) Console.Write("█");
        for (int i = filledLength; i < barWidth; i++) Console.Write(" ");
        
        Console.Write($"] {current}/{total} ({progress * 100:F1}%)"); 
    }
}
