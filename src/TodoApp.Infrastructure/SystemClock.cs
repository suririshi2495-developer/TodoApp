using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Core;

namespace TodoApp.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
