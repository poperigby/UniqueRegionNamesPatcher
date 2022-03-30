using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniqueRegionNamesPatcher
{
    internal class Settings
    {
        public Settings()
        {
        }

        [SettingName("Verbose Log")]
        public bool verbose = true;
    }
}
