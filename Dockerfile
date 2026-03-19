FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ChimQuiz/ChimQuiz.csproj ChimQuiz/
RUN dotnet restore ChimQuiz/ChimQuiz.csproj
COPY . .
RUN dotnet publish ChimQuiz/ChimQuiz.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Run as non-root user (DS-0002 best practice)
RUN addgroup --system chimquiz && adduser --system --ingroup chimquiz chimquiz \
    && mkdir -p /data && chown chimquiz:chimquiz /data

COPY --from=build --chown=chimquiz:chimquiz /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DatabasePath=/data/chimquiz.db

USER chimquiz
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChimQuiz.dll"]
