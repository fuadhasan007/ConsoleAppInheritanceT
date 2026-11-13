using System;

double[][] jaggedArray = new double[][]
{
    new double[] { 1.5, 2.5, 3.5 },
    new double[] { 4.3, 5.8 },
    new double[] { 6.2, 7.7, 8.2, 9.4 }
};

Console.WriteLine("Jagged Array:");
for (int i = 0; i < jaggedArray.Length; i++)
{
    for (int j = 0; j < jaggedArray[i].Length; j++)
    {
        Console.Write(jaggedArray[i][j] + " ");
    }
    Console.WriteLine();
}

double element1 = jaggedArray[0][0]; // 1.5
double element2 = jaggedArray[1][1]; // 5.8
double element3 = jaggedArray[2][3]; // 9.4

Console.WriteLine("Accessed Elements:");
Console.WriteLine("\nElement1: " + element1);
Console.Write("Element2: " + element2);
Console.WriteLine("Element3: " + element3);

Console.WriteLine(jaggedArray[1][1] - jaggedArray[0][1]); // 5.8 - 2.5 = 3.3
Console.Write((int)jaggedArray[2][1]);                   // (int)7.7 = 7
Console.WriteLine("The end");