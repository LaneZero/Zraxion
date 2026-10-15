using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    internal sealed class GridAnalyticsDisplayOptions
    {
        public bool ShowSum { get; set; } = true;

        public bool ShowCount { get; set; } = true;

        public bool ShowAverage { get; set; } = true;

        public bool ShowMin { get; set; }

        public bool ShowMax { get; set; }

        public bool ShowNullOrEmpty { get; set; }

        public bool ShowNonNumeric { get; set; }

        public bool AutoRefresh { get; set; } = true;
    }
}