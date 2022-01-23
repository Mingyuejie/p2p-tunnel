using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.messengers.punchHole
{
    public interface IPunchHole
    {
        PunchHoleTypes Type { get; }
        void Execute(OnPunchHoleArg arg);
    }
}
