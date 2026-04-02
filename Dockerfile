FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish ClaudeWebApp/ClaudeWebApp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /src
COPY . .
RUN dotnet restore
ENTRYPOINT ["dotnet", "test", "ClaudeWebApp.Tests/", "--logger", "console;verbosity=normal"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ClaudeWebApp.dll"]
