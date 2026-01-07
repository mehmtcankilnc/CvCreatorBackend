FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-jammy AS build
WORKDIR /src

COPY ["CvCreator.API/CvCreator.API.csproj", "CvCreator.API/"]
COPY ["CvCreator.Application/CvCreator.Application.csproj", "CvCreator.Application/"]
COPY ["CvCreator.Domain/CvCreator.Domain.csproj", "CvCreator.Domain/"]
COPY ["CvCreator.Infrastructure/CvCreator.Infrastructure.csproj", "CvCreator.Infrastructure/"]

RUN dotnet restore "CvCreator.API/CvCreator.API.csproj"
COPY . .

WORKDIR "/src/CvCreator.API"
RUN dotnet publish "CvCreator.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-jammy AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CvCreator.API.dll"]