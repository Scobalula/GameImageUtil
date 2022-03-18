using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil.Collections
{
    internal class DisposableDictionary<T, V> : Dictionary<T, V> where V : IDisposable where T : notnull
    {
        public DisposableDictionary()
        {

        }
    }
}
