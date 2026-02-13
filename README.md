# MilsimManager

> Praca inżynierska - Hubert Płociński s27049

Web app for managing Arma 3 milsim communities.
The goal is to replace scattered spreadsheets/tools with a single, consistent system for admins and members.

#### [Documentation(PL)](https://docs.google.com/document/d/1jBuzZ__5g5zW6iylK1n6S802bRqPrOCK/edit?usp=sharing&ouid=105549434782914588857&rtpof=true&sd=true)

#### [Figma (Wireframe)](https://www.figma.com/design/vFjFr9PPCNHk5uJG9aM232/s27049-In%C5%BCynierka?node-id=0-1&t=cROPkyiHY8S6ULIx-1)

#### [Figma (Prototype)](https://www.figma.com/proto/vFjFr9PPCNHk5uJG9aM232/s27049-In%C5%BCynierka?node-id=0-1&t=cROPkyiHY8S6ULIx-1)

## Tech stack

- .NET 8
- Blazor Server (interactive server rendering)
- Entity Framework Core + Npgsql (PostgreSQL provider)
- PostgreSQL
- MudBlazor (UI components)

## Project structure

- `MilsimManager/Models` - EF Core entities (domain model)
- `MilsimManager/Context.cs` - EF Core DbContext
- `MilsimManager/Migrations` - EF Core migrations
- `MilsimManager/Services` - application services (business operations)
- `MilsimManager/Pages` - routeable pages (views)
- `MilsimManager/Components` - reusable UI components and dialogs
- `MilsimManager/Layout` - layout and navigation
- `MilsimManager/wwwroot` - static files (CSS, images)

## Database

PostgreSQL schema is managed by EF Core migrations.

###### Schema

[![Database schema](Schema.svg)](Schema.svg)

## How to run (local development)

- The Rider run configuration `MilsimManager: http` includes Docker Compose (PostgreSQL) and runs the development database seed.
- The `Compose Database` configuration is bundled only with `MilsimManager: http`. If you run `MilsimManager: https` or `MilsimManager: IIS Express`, start `Compose Database` separately first.
- Docker Engine must be running (Docker Desktop open).
