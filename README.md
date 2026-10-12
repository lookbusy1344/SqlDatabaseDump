# SqlDatabaseDump

Command line utility to dump SQL Server database structure to files. Built with .NET 10. Multi-threading by default.

## Building

Publish the app in release mode with dependencies included:

```
Publish.cmd
```

On macOS or Linux, publish for the local runtime, e.g. `dotnet publish -c Release -r osx-arm64 --self-contained false /p:PublishSingleFile=true`.

## Usage

```
SqlDatabaseDump --instance <instance> --database <db> --dir <dir>

..eg..

SqlDatabaseDump --instance (local) --database MyDB --dir c:\MyDB
SqlDatabaseDump --instance localhost,1433 --database MyDB --dir ~/MyDB --user sa --trust-server-certificate
```

Windows authentication is the default. On macOS and Linux, use a SQL Server login with `--user`. The password comes from the `DB_PASSWORD` environment variable, or a prompt.

Options:

```
Required:
  -i, --instance <instance>  SQL Server instance to connect to  (or DB_INSTANCE environment variable)
  -d, --database <db>        Database to process                (or DB_DATABASE environment variable)
  -o, --dir <dir>            Output directory                   (or DB_DIR environment variable)

Authentication (default is Windows authentication):
  -u, --user <login>         SQL Server login                   (or DB_USER environment variable)
                             Password from DB_PASSWORD environment variable, or prompted
  -t, --trust-server-certificate  Accept a self-signed server certificate

Options:
  -e, --extended-properties  Include extended properties
  -w, --with-dependencies    Include dependencies
  -a, --all                  Include all extras (extended properties and dependencies)

  -r, --replace              Replace existing files (default is to fail if file exists)
  -s, --single-thread        Single thread processing
  -p, --parallel <n>         Maximum parallel tasks 1..16 (default is 8)
  -k, --skip-errors          Skip errors without writing to file
  -h, --help, -?             Help information
```
