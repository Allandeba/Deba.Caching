# Deba.Caching

![CI Status](https://github.com/allandeba/deba.Caching/actions/workflows/publish.yml/badge.svg)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/allandeba/deba.Caching/blob/main/LICENSE)

Contém solução para memory caching em aplicações server e também localStorageCaching para aplicações web.

## Versionamento

Para adicionar minor ou major no package deve ser feito um commit com a seguinte nomenclatura:

- Major: `+semver: breaking` ou `+semver: major`
- Minor: `+semver: feature` ou `+semver: minor`

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