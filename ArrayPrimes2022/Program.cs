using ArrayPrimes2022;

//Name with some randomness to prevent accidental collisions.
const string appGuid = "ArrayPrimes.4R2VY2PE7UTZ9FML6QA3CN5D";

// See https://aka.ms/new-console-template for more information
var start = DateTime.Now;
Console.WriteLine("Hello, World!");

using (var mutex = new Mutex(false, "Global\\" + appGuid))
{
    if (!mutex.WaitOne(0, false))
        Console.WriteLine("Application is already Running. Exiting.");
    else
        ProgramClass.MainProgram();
}

Console.WriteLine($"Goodbye, World! RunTime {DateTime.Now - start}");