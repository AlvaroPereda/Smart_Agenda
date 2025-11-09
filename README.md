# Smart Agenda (En Progreso)

⚠️ **Aviso:** Esta aplicación está en progreso y no se encuentra finalizada.

## Descripción

Esta es una aplicación desarrollada en **.NET Core 8.0** que utiliza **MySQL** como base de datos. La aplicación está preparada para ejecutarse dentro de contenedores Docker usando Docker Compose.


## Ejecución

Para levantar la aplicación y la base de datos navegue hasta la carpeta Smart_Agenda y ejecuta:

```bash
docker compose up -d
```

Cuando finalice la ejecución del docker compose entre en la url  [http://localhost:5025/](http://localhost:5025/) 

## Detener y limpiar

Para detener los contenedores y eliminar los volúmenes de datos:

```bash
docker compose down -v 
```
```bash
docker rmi smart_agenda-net
```
```bash
docker rmi mysql:8.0
```
