

# Mac publish
```bash
dotnet publish -c Release -r osx-x64 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

# Linux publish
```bash
dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

# Windows publish
```powershell
dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

# Runtime identifiers
If a different target is required, review https://docs.microsoft.com/en-us/dotnet/core/rid-catalog for a full listing.

# 

Produces a single executable file __TownSuite.Prometheus.FileSdConfigs__.   Configure in appsettings.json file in the same folder as the executable.

__appsettings.json__ example
```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Debug",
            "System": "Information",
            "Microsoft": "Information"
        }
    },
    "DelayInSeconds": 1800,
    "OutputPath": "targets.json",
    "Settings": [{
        "LookupUrl": "http host address and path or filepath",
        "AuthHeader": "[If SrcPath is a url and auth this field should be set to basic auth or a bearer token]",
        "AppendPaths": [
            "/metrics",
            "/api/status"
        ]
    }],
    "Labels": {
        "job": "test",
        "env": "dev"
    }
}
```
