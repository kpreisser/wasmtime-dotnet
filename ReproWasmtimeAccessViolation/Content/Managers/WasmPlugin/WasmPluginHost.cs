using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

using ReproWasmtime.Api;

using Wasmtime;

using WasmModule = Wasmtime.Module;
using WasmEngine = Wasmtime.Engine;

namespace ReproWasmtime
{
    internal class WasmPluginHost : IDisposable
    {
        private readonly object syncRoot = new();

        private readonly WasmPluginManager manager;

        private readonly object? config;

        private readonly string wasmName;

        private readonly string? displayName;

        private readonly Memory<byte> wasmContent;

        private readonly bool wasmContentIsWat;

        private readonly string wasmContentPathWithoutFileExtension;

        private readonly int wasmTimeout;

        private (
            Config config,
            WasmEngine engine,
            WasmModule module,
            Linker linker,
            Store store
        )? wasm;

        private WasmApi? api;

        private volatile int state;

        private bool isFirstTransition;


        public WasmPluginHost(
                WasmPluginManager manager,
                object? wasmLogger,
                string wasmName,
                string? displayName,
                Memory<byte> wasmContent,
                bool wasmContentIsWat,
                string wasmContentPathWithoutFileExtension)
            : base()
        {
            this.manager = manager;
            this.wasmName = wasmName;
            this.displayName = displayName;
            this.wasmContent = wasmContent;
            this.wasmContentIsWat = wasmContentIsWat;
            this.wasmContentPathWithoutFileExtension = wasmContentPathWithoutFileExtension;

            this.wasmTimeout = -1;
        }

        public object SyncRoot
        {
            get => this.syncRoot;
        }

        public WasmPluginManager Manager
        {
            get => this.manager;
        }

        public object? WasmLogger
        {
            get => null;
        }

        public WasmEngine? WasmEngine
        {
            get => this.wasm?.engine;
        }

        public int WasmTimeout
        {
            get => this.wasmTimeout;
        }

        public bool IsFirstTransition
        {
            get => this.isFirstTransition;
            set => this.isFirstTransition = value;
        }

        public int State
        {
            get => this.state;
            private set => this.state = value;
        }

        public bool IsRunning
        {
            get => this.State is 1;
        }

        public int CyclesToWaitBeforeRestart
        {
            get;
            set;
        }

        private static (Config, WasmEngine, WasmModule, Linker, Store) CreateWasmComponents(
                ReadOnlySpan<byte> content,
                bool contentIsWat)
        {
            const string mainModuleName = "Main";

            var config = default(Config);
            var engine = default(WasmEngine);
            var module = default(WasmModule);
            var linker = default(Linker);
            var store = default(Store);

            try {
                config = new Config()
                        .WithEpochInterruption(true)
                        .WithMaximumStackSize(2 * 1024 * 1024);

                engine = new WasmEngine(config);

                if (!contentIsWat) {
                    module = WasmModule.FromBytes(engine, mainModuleName, content);
                }
                else {
                    var textContent = Encoding.UTF8.GetString(content);
                    module = WasmModule.FromText(engine, mainModuleName, textContent);
                }

                linker = new Linker(engine);
                store = new Store(engine);

                linker.AllowShadowing = true;

                store.SetEpochDeadline(1);

                linker.DefineWasi();

                var wasiConfig = new WasiConfiguration();
                store.SetWasiConfiguration(wasiConfig);

                return (config, engine, module, linker, store);
            }
            catch {
                store?.Dispose();
                linker?.Dispose();
                module?.Dispose();
                engine?.Dispose();
                config?.Dispose();

                throw;
            }
        }

        public void Start()
        {
            Debug.Assert(Monitor.IsEntered(this.syncRoot));

            if (this.State == default) {
                this.State = 1;

                try {
                    this.wasm = CreateWasmComponents(
                            this.wasmContent.Span,
                            this.wasmContentIsWat);
                }
                catch (Exception ex) {
                    this.State = 2;
                    Console.WriteLine(ex.ToString());
                    return;
                }

                this.api = new WasmApi(this);
                this.api.PopulateWasmImports(this.wasm.Value.linker);

                var moduleInstance = default(Instance);
                this.api.WrapWasmCall(() => {
                    moduleInstance = this.wasm.Value.linker.Instantiate(
                            this.wasm.Value.store,
                            this.wasm.Value.module);

                    this.api.PopulateWasmExports(moduleInstance!);

                    this.api.WasmWasiStartOrInitialize?.Invoke();
                    return 0;
                });
            }
        }

        public void Dispose()
        {
            if (this.wasm != null)
            {
                this.wasm.Value.store.Dispose();
                this.wasm.Value.linker.Dispose();
                this.wasm.Value.module.Dispose();
                this.wasm.Value.engine.Dispose();
                this.wasm.Value.config.Dispose();
            }
        }
    }
}
