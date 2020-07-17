using System;
using Konsole;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.ConsoleColor;

namespace ShowPCUsage
{
	class Program
	{
		static void Main(string[] args)
		{
            // show how you can mix and match System.Console with Konsole
            Console.WriteLine("line one");

            // create an inline Box window at the current cursor position 
            // 20 characters wide, by 12 tall.
            // returns a Window that implements IConsole 
            // that you can use to write to the window 
            // and create new windows inside that window.

            var nyse = Window.OpenBox("Transcoder", 20, 12, new BoxStyle()
            {
                ThickNess = LineThickNess.Single,
                Title = new Colors(White, Red)
            });

            Console.WriteLine("line two");

            decimal amazon = 84;

            int i = 9;
            while (i>0)
            {
                Tick(nyse, "AMZ", amazon -= 0.04M, Red, '-', 4.1M);
                i--;
            }

            

            // simple method that takes a window and prints a stock price 
            // to that window in color
            void Tick(IConsole con, string sym, decimal newPrice,
               ConsoleColor color, char sign, decimal perc)
            {
                con.Write(White, $"{sym,-10}");
                con.WriteLine(color, $"{newPrice:0.00}");
                con.WriteLine(color, $"  ({sign}{newPrice}, {perc}%)");
                con.WriteLine("");
            }
        }
	}
}
