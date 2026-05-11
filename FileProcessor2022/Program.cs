using FileProcessor2022;

//Name with some randomness to prevent accidental collisions.
const string appGuid = "FileProcessor.BK3NGX2C86TR9HP0DQ73UWS";

// See https://aka.ms/new-console-template for more information
// Record the start time to calculate total runtime on exit.
var start = DateTime.Now;
Console.WriteLine("Hello, World!");

// Use a global mutex to ensure only one instance of this application runs at a time.
using (var mutex = new Mutex(false, "Global\\" + appGuid))
{
    // WaitOne(0) returns immediately: true if we acquired the mutex, false if another instance holds it.
    if (!mutex.WaitOne(0, false))
        Console.WriteLine("Application is already Running. Exiting.");
    else
        ProgramClass.MainProgram();
}

// Print total elapsed runtime before the process exits.
Console.WriteLine($"Goodbye, World! RunTime {DateTime.Now - start}");