using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFLandCOMP.Services
{
    public class SimpleObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;

        public SimpleObserver(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnNext(T value) => _onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
