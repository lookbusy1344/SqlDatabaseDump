using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDatabaseDump;

internal sealed record class Config(string Instance, string Database, string Dir, int MaxParallel, bool SingleThread, bool Replace);
