using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreciseAlign.Core.Interfaces
{
    internal interface ILoggerService
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception? ex = null);
        void Debug(string message);
    }
}
