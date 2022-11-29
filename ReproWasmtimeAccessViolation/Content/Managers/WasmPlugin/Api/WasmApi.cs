using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Wasmtime;

namespace ReproWasmtime.Api
{
    internal partial class WasmApi
    {
        private readonly WasmPluginHost host;

        private bool isDisposed;

        private bool isActive;

        private volatile bool isCancelled = false;

        private List<long>? pendingCallbacksRefData;

        private int lastErrorCode;

        private Exception? lastHostErrorException;

        private string? lastWasmErrorMessage;

        private bool wasmInstantiantionCompleted;

        private Memory? wasmMemory;

        private Func<long, int, int>? wasmHandleCallback;

        private Action? wasmWasiStartOrInitialize;

        private Func<int>? wasmHandleStart;

        private Func<int>? wasmHandleStop;

        public WasmApi(WasmPluginHost host)
            : base()
        {
            this.host = host;

            this.isActive = true;
        }

        public WasmPluginHost Host
        {
            get => this.host;
        }

        public string? LastWasmErrorMessage
        {
            get => this.lastWasmErrorMessage;
            set => this.lastWasmErrorMessage = value;
        }

        public Action? WasmWasiStartOrInitialize
        {
            get => this.wasmWasiStartOrInitialize;
        }

        public void PopulateWasmImports(Linker linker)
        {
        }

        public void PopulateWasmExports(Instance instance)
        {
            this.wasmWasiStartOrInitialize = instance.GetAction("_initialize") ?? instance.GetAction("_start");

            this.wasmInstantiantionCompleted = true;
        }

        public void WrapWasmCall(
                Func<int> wasmInvokeHandler,
                bool allowReturnError = false)
        {
            Debug.Assert(
                    Monitor.IsEntered(this.Host.SyncRoot),
                    "A lock on the WasmHost's SyncRoot is required.");

            bool localAllowReturnErrors = allowReturnError;

            bool isFirst = !this.Host.IsFirstTransition;
            if (isFirst)
                this.Host.IsFirstTransition = true;

            try {
                var invokeStatusCode = 0;
                string? wasmErrorMessage = null;

                bool ctsCanceled = false;

                try {
                    using var cts = new CancellationTokenSource();
                    object? placeholder2 = null;

                    invokeStatusCode = wasmInvokeHandler();

                    if (invokeStatusCode is not 0) {
                        wasmErrorMessage = this.LastWasmErrorMessage;
                        this.LastWasmErrorMessage = null;
                    }
                }
                finally {
                    if (Volatile.Read(ref ctsCanceled)) {
                        throw new TimeoutException();
                    }
                }
            }
            catch (Exception) {
                throw;
            }
            finally {
                if (isFirst)
                    this.Host.IsFirstTransition = false;
            }

            void ExecutePendingCallbacksWithinWasm()
            {
                if (this.pendingCallbacksRefData?.Count > 0) {
                }
            }
        }
    }
}
