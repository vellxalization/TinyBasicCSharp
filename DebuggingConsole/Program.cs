using DebuggingConsole;

var listener = new PipeListener();
await listener.Listen();
Console.WriteLine("Execution is over. Press any key to exit...");
Console.ReadKey();