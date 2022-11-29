using System;
using System.IO;
using System.Threading;

namespace ReproWasmtime
{
    internal partial class WasmPluginManager
    {
        private Thread? thread;

        public WasmPluginManager(object? pluginHost)
            : base()
        {
        }

        public void Start()
        {
            this.thread = new Thread(this.RunWasmInitializerThread, 8 * 1024 * 1024);
            thread.Start();
        }

        public void Stop()
        {
            this.thread!.Join();
        }

        private void RunWasmInitializerThread()
        {
            byte[] wasmContent = File.ReadAllBytes(@"dotnet-codabix-wasm-build.wasm");
            using var tmpHost = new WasmPluginHost(this, null, "x", "", wasmContent, false, "");

            lock (tmpHost.SyncRoot)
                tmpHost.Start();

            Console.WriteLine("wasmtime_func_call completed! This should not be displayed if the Access Violation occured.");
        }
    }
}