call yarn

cd src\Client
dotnet restore
dotnet fable webpack -- -p

cd ..\Server
dotnet build
cd ..\..