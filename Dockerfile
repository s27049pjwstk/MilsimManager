FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . /app/.
RUN dotnet restore
RUN dotnet publish MilsimManager/MilsimManager.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
EXPOSE 8080
ENTRYPOINT ["dotnet", "MilsimManager.dll"]
