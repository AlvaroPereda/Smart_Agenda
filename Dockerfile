FROM mcr.microsoft.com/dotnet/sdk:8.0 

WORKDIR /Calendar

COPY Calendar .

RUN dotnet restore

EXPOSE 5025

CMD [ "dotnet", "run" ]