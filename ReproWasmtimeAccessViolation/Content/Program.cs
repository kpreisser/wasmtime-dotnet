using System;
using System.Runtime.InteropServices;
using System.Threading;

using ReproWasmtime;

var manager = new WasmPluginManager(null);
manager.Start();
try {
    Console.WriteLine(".NET Version: " + RuntimeInformation.FrameworkDescription);
    Console.WriteLine("WasmPluginManager started. Please wait...");
    Thread.Sleep(-1);
}
finally {
    manager.Stop();
}