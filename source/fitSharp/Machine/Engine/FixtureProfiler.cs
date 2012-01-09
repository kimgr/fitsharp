using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using fitSharp.Machine.Model;

namespace fitSharp.Machine.Engine {
    public class FixtureProfiler {
        public bool Enable { get; set; }
        public string ConsoleOutputPrefix { get; set; }

        public void Configure(bool enable, string prefix) {
            this.Enable = enable;
            this.ConsoleOutputPrefix = prefix;
        }
    }
}
