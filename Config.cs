using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDatabaseDump;

internal sealed record class Config(string InstanceName, string DatabaseName, string OutputDirectory, int MaxParallel,
	bool SingleThread, bool ReplaceExistingFiles, bool SkipErrors);
