using System;
using System.IO;
using System.Threading;

namespace ReproWasmtime
{
    internal partial class WasmPluginManager
    {
        public WasmPluginManager(object? pluginHost)
            : base()
        {
        }

        public void Start()
        {
            var thread = new Thread(this.RunWasmInitializerThread);
            thread.Start();
        }

        public void Stop()
        {           
        }

        private void RunWasmInitializerThread()
        {
            byte[] wasmContent = File.ReadAllBytes(@"dotnet-codabix-wasm-build.wasm");
            var tmpHost = new WasmPluginHost(this, null, "x", "", wasmContent, false, "");

            lock (tmpHost.SyncRoot)
                tmpHost.Start();

            GC.KeepAlive(tmpHost);

            Console.WriteLine("wasmtime_func_call completed! This should not be displayed if the Access Violation occured.");
        }
    }
}