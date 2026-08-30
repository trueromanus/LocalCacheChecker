# LocalCacheChecker
Application for create cache files from nextAPI.

# Documentation
[English Documentation](https://github.com/trueromanus/LocalCacheChecker/wiki/English-Documentation)  
[Russian Documentation](https://github.com/trueromanus/LocalCacheChecker/wiki/Russian-Documentation)

# Build Requirements
- DotNet 10.0+
# Build Instructions
First you need to install [dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

How to build executable:
```shell
dotnet dotnet publish -r <platform-indetifier> -c Release --self-contained true src/LocalCacheChecker.csproj
```
How to build library:
```shell
dotnet publish -r <platform-indetifier> -c Release --self-contained true LocalCacheCheckerLibrary/LocalCacheCheckerLibrary.csproj
```

`platform-identifier` can be:
- osx-x64 (macOS with intel processor)
- osx-arm64 (macOS with M1+ processor)
- win-arm64
- win-x64
- linux-arm64
- linux-x64
