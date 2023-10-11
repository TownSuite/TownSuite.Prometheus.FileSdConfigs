
[![.NET](https://github.com/TownSuite/TownSuite.Prometheus.FileSdConfigs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/TownSuite/TownSuite.Prometheus.FileSdConfigs/actions/workflows/dotnet.yml)

[![Dotnet Test Report](https://github.com/TownSuite/TownSuite.Prometheus.FileSdConfigs/actions/workflows/test-report.yml/badge.svg)](https://github.com/TownSuite/TownSuite.Prometheus.FileSdConfigs/actions/workflows/test-report.yml)

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
  "AppSettings": {
    "DelayInSeconds": 1800,
    "UserAgent": "TownSuiteSD/1.0 Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:107.0) Gecko/20100101 Firefox/107.0",
    "OutputPath": "targets.json",
    "OutputPathV2": "targets_v2.json"
  },
  "Settings": [
    {
      "LookupUrl": "http host address and path or filepath",
      "AuthHeader": "[If SrcPath is a url and auth this field should be set to basic auth or a bearer token]",
      "AppendPaths": [
        "/metrics",
        "/api/status"
      ],
      "Labels": {
        "job": "test",
        "env": "dev"
      },
      "IgnoreList": [
        "https://example.townsuite.com"
      ]
    }
  ],
  "SettingsV2": [
    {
      "ServiceListUrl": "http host address and path or filepath that lists services",
      "ServiceDiscoverUrl": "http host address and path or filepath to lookup details for specific services",
      "AuthHeader": "[If SrcPath is a url and auth this field should be set to basic auth or a bearer token]",
      "IgnoreList": [
        "https://example.townsuite.com"
      ],
      "LowercaseLabels": true
    }
  ]
}
```


# Setup as a windows service.

Download the executable


1. Copy it to a folder such as C:\promethues\TownSuite.Prometheus.FileSdConfigs 
2. Edit appsettings.json
3. Use [nssm](https://nssm.cc/) to configure it as a windows service.


```powershell
nssm install TownSuite.Prometheus.FileSdConfigs C:\prometheus\TownSuite.Prometheus.FileSdConfigs\TownSuite.Prometheus.FileSdConfigs.exe
nssm set TownSuite.Prometheus.FileSdConfigs AppDirectory C:\prometheus\TownSuite.Prometheus.FileSdConfigs
net start TownSuite.Prometheus.FileSdConfigs
```


# Setup as a Linux service

Extract the release and copy it to /opt/TownSuite.Prometheus.FileSdConfigs.

```
[Unit]
Description=The description of your service
# How to install:
# Copy YourProgramName binary to /opt/TownSuite.Prometheus.FileSdConfigs/TownSuite.Prometheus.FileSdConfigs
# Copy /etc/systemd/system/townsuite-prometheus-filesdconfigs.service
# systemctl enable townsuite-prometheus-filesdconfigs.service
# systemctl start townsuite-prometheus-filesdconfigs.service

[Service]
ExecStart=/opt/TownSuite.Prometheus.FileSdConfigs/TownSuite.Prometheus.FileSdConfigs
Restart=always
RestartSec=10                       # Restart service after 10 seconds if node service crashes
StandardOutput=syslog               # Output to syslog" >> /etc/systemd/system/your-program-service-name.service
StandardError=syslog                # Output to syslog" >> /etc/systemd/system/your-program-service-name.service
SyslogIdentifier=TownSuite.Prometheus.FileSdConfigs
WorkingDirectory=/opt/TownSuite.Prometheus.FileSdConfigs/

[Install]
WantedBy=multi-user.target
```

