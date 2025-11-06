using System;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    public abstract class InteropObjectInstance : IDisposable
    {
        internal IntPtr Pointer { get; set; }

        private bool disposedValue;

        protected InteropObjectInstance(IntPtr pointer)
        {
            Pointer = pointer;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                Pointer = IntPtr.Zero;
                disposedValue = true;
            }
        }

        ~InteropObjectInstance()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override bool Equals(object obj)
        {
            var instance = obj as InteropObjectInstance;
            if (instance != null)
                return instance.Pointer == Pointer;
            return false;
        }

        public override int GetHashCode()
        {
            return Pointer.ToInt32();
        }

        public static bool operator ==(InteropObjectInstance a, InteropObjectInstance b)
        {
            if (Equals(a, null) || Equals(b, null))
                return Equals(a, b);

            return a.Pointer.Equals(b.Pointer);
        }

        public static bool operator !=(InteropObjectInstance a, InteropObjectInstance b)
        {
            return !(a == b);
        }
    }
}