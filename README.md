# SqlDatabaseDump

Command line utility to dump SQL Server database structure to files. Built with .NET 8. Multi-threading by default.

## Building

Publish the app in release mode with dependencies included:

```
Publish.cmd
```

## Usage

```
C:\> SqlDatabaseDump.exe --instance <instance> --database <db> --dir <dir>

..eg..

C:\> SqlDatabaseDump.exe --instance (local) --database MyDB --dir c:\MyDB
```

Options:

```
Required:
  -i, --instance <instance>  SQL Server instance to connect to  (or DB_INSTANCE environment variable)
  -d, --database <db>        Database to process                (or DB_DATABASE environment variable)
  -o, --dir <dir>            Output directory                   (or DB_DIR environment variable)

Options:
  -r, --replace              Replace existing files (default is to fail if file exists)
  -s, --singlethread         Single thread processing
  -p, --parallel <n>         Maximum parallel tasks 1..16 (default is 8)
  -h, --help, -?             Help information
```