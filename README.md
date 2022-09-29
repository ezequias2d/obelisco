# Obelisco
Obelisco is a blockchain based poll system.

## Prerequisites

* .NET 6

## Building Obelisco and start server

```
git clone https://github.com/ezequias2d/Obelisco
cd obelisco
dotnet build
cd Obelisco.App/bin/Debug/net6.0
```
for server uses
```
./Obelisco.App -s -p 1234
```
or for client (if on same machine, use ip as 127.0.0.1)
```
./Obelisco.App
connect ip:1234
```