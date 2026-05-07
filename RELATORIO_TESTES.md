# Relatório de Testes - Oficina API

## 1. Identificação

**Projeto:** Oficina API  
**Tipo de aplicação:** API REST para gestão de oficina mecânica  
**Tecnologia:** .NET 8, ASP.NET Core, Entity Framework Core  
**Framework de testes:** xUnit  
**Data da execução:** 05/05/2026  

## 2. Objetivo dos Testes

O objetivo dos testes unitários é validar as principais regras de negócio e o comportamento básico da camada de acesso a dados da API, garantindo que funcionalidades essenciais do sistema funcionem corretamente de forma isolada, sem depender do banco de dados real ou do ambiente de produção.

Os testes foram criados para verificar:

- autenticação de usuários;
- cadastro de usuários por perfil;
- validação de usuário duplicado;
- reset de senha;
- cadastro, listagem, atualização e exclusão de clientes;
- persistência básica de dados no contexto da aplicação.

## 3. Estrutura dos Testes

Os testes foram organizados no projeto:

```text
tests
```

A estrutura foi separada de acordo com as camadas do backend, usando dois projetos de teste:

```text
tests
  Business.UnitTests
    AuthServiceTests.cs
    ClienteServiceTests.cs
    TestHelpers.cs

  DAL.UnitTests
    AppDbContextTests.cs
    TestHelpers.cs
```

## 4. Camadas Testadas

### 4.1 Business

O projeto `Business.UnitTests` contém os testes das regras de negócio da aplicação.

Foram testados os seguintes serviços:

- `AuthService`
- `ClienteService`

### 4.2 DAL

O projeto `DAL.UnitTests` contém os testes relacionados ao acesso a dados.

Foi testado:

- `AppDbContext`

Para os testes do DAL foi utilizado banco em memória, evitando alterações no banco real da aplicação.

## 5. Ambiente de Teste

Os testes foram executados localmente com:

```text
.NET 8
xUnit
Entity Framework Core InMemory
```

O banco utilizado nos testes foi criado em memória para cada execução, garantindo isolamento entre os casos de teste.

## 6. Casos de Teste Executados

### 6.1 Testes do AuthService

| Caso de teste | Resultado esperado | Status |
|---|---|---|
| Login com email e senha válidos | Deve retornar um token JWT | Aprovado |
| Login com senha inválida | Deve retornar nulo | Aprovado |
| Cadastro de usuário com perfil 1 | Deve cadastrar usuário ADMIN | Aprovado |
| Cadastro de usuário com perfil 2 | Deve cadastrar usuário CLIENTE | Aprovado |
| Cadastro de usuário com perfil 3 | Deve cadastrar usuário FUNCIONARIO | Aprovado |
| Cadastro com email já existente | Deve retornar erro | Aprovado |
| Reset de senha de usuário existente | Deve alterar a senha | Aprovado |

### 6.2 Testes do ClienteService

| Caso de teste | Resultado esperado | Status |
|---|---|---|
| Cadastro de cliente com documento válido | Deve cadastrar o cliente | Aprovado |
| Cadastro de cliente com documento inválido | Deve retornar erro | Aprovado |
| Listagem de clientes cadastrados | Deve retornar os clientes salvos | Aprovado |
| Atualização de cliente existente | Deve alterar os dados do cliente | Aprovado |
| Exclusão de cliente existente | Deve remover o cliente | Aprovado |

### 6.3 Testes do AppDbContext

| Caso de teste | Resultado esperado | Status |
|---|---|---|
| Salvar e consultar cliente | Deve persistir e recuperar o cliente | Aprovado |
| Salvar e consultar usuário | Deve persistir e recuperar o usuário | Aprovado |

## 7. Resultado da Execução

Comando utilizado para execução:

```powershell
dotnet test oficina-api.sln
```

Resultado obtido:

```text
Business.UnitTests: Aprovado! - Com falha: 0, Aprovado: 12, Ignorado: 0, Total: 12`r`nDAL.UnitTests: Aprovado! - Com falha: 0, Aprovado: 2, Ignorado: 0, Total: 2
```

Resumo:

| Métrica | Quantidade |
|---|---:|
| Testes executados | 14 |
| Testes aprovados | 14 |
| Testes com falha | 0 |
| Testes ignorados | 0 |

## 8. Conclusão

Os testes unitários foram executados com sucesso. Todos os 14 testes passaram, indicando que as principais regras de negócio testadas e a persistência básica do contexto da aplicação estão funcionando corretamente.

A criação dos testes contribui para aumentar a confiabilidade do sistema e facilitar futuras manutenções, pois permite identificar rapidamente possíveis regressões quando novas alterações forem feitas no backend.


