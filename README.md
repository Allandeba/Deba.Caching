# Deba.Caching

![CI Status](https://github.com/allandeba/deba.Caching/actions/workflows/publish.yml/badge.svg)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/allandeba/deba.Caching/blob/main/LICENSE)

Contém solução para memory caching em aplicações server e também localStorageCaching para aplicações web.

## Build, testes e pacote

```powershell
dotnet restore Deba.Caching.sln
dotnet build Deba.Caching.sln --configuration Release --no-restore
dotnet test Deba.Caching.sln --configuration Release --no-build --no-restore -p:GeneratePackageOnBuild=false
dotnet pack Deba.Caching/Deba.Caching.csproj --configuration Release --no-build --no-restore --output artifacts/packages -p:PackageVersion=0.0.0-net10-validation
```

O build em `Release` não gera pacotes nem escreve em `C:/NugetLocal`; nessa configuração, o pack é explícito. Em `Debug` (padrão de `dotnet build`), o projeto gera pacotes automaticamente em `C:/NugetLocal`. Para desativar essa geração em um build Debug, use `-p:GeneratePackageOnBuild=false`. Pull requests executam validação e armazenam artefatos. Pushes na `main` publicam automaticamente após validação bem-sucedida. Execução manual na `main`, com `publish=true`, também permite publicar.

## Versionamento

Para adicionar minor ou major no package deve ser feito um commit com a seguinte nomenclatura:

- Major: `+semver: breaking` ou `+semver: major`
- Minor: `+semver: feature` ou `+semver: minor`
- Patch: `+semver: fix` ou `+semver: patch`

## Instalação

```bash
dotnet add package Deba.Caching
```

## Utilização

- MemoryCaching `IMemoryCacheService`
```bash
builder.Services.AddDebaCaching(ECachingType.MemoryCache);
```

- LocalStorage `ILocalStorageCacheService`
```bash
builder.Services.AddDebaCaching(ECachingType.LocalStorage);
```
