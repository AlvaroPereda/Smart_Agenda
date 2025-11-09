FROM mcr.microsoft.com/dotnet/sdk:8.0 

WORKDIR /Calendar

COPY Calendar .

RUN dotnet add package Microsoft.EntityFrameworkCore \
    && dotnet add package Pomelo.EntityFrameworkCore.MySql

EXPOSE 5025

CMD [ "dotnet", "run" ]