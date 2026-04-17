# PATLITE client tool

A .NET library and Global tool for controlling [PATLITE](https://www.patlite.com/) signal tower lights over TCP/UDP.

[![NuGet](https://img.shields.io/nuget/v/Patlite.Client.svg)](https://www.nuget.org/packages/Patlite.Client/)

# Tools

```
dotnet tool install -g PatliteTool
```

# Usage

## write

```
patlite write -h 192.168.1.101 -c g
```

```
patlite write -h 192.168.1.101 -c gyr
```

```
patlite write -h 192.168.1.101 -c r -b
```
