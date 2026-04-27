# Oficina API

API REST do Sistema Integrado de Atendimento e Execução de Serviços para uma oficina mecânica.

## Links

Swagger publicado: https://oficina-api-10.onrender.com/swagger

Front publicado: https://oficina-front.vercel.app

## Tecnologias

- .NET 8
- ASP.NET Core
- Entity Framework Core
- SQL Server
- Azure SQL Database
- JWT
- Swagger
- Docker
- Render

## Arquitetura

A API segue uma estrutura em camadas:

API: Controllers REST, autenticação e documentação Swagger.

Business: Services com regras de negócio.

DAL: DbContext, entidades e acesso ao banco via Entity Framework Core.

## Funcionalidades

- Autenticação via JWT
- Perfis de acesso: ADMIN e CLIENTE
- CRUD de clientes
- CRUD de veículos
- CRUD de peças e serviços
- Criação de ordens de serviço
- Inclusão de serviços e peças na OS
- Cálculo automático do valor total da OS
- Validação de fluxo de status
- Reserva de estoque ao ir para Aguardando Aprovação
- Baixa de estoque ao ir para Em Execução
- Listagem e detalhamento de ordens
- Resumo de ordens com tempo médio
- Proteção de rotas autenticadas
- Documentação via Swagger

## Usuários de teste

Administrador:

Email: giulia.sia@hotmail.com  
Senha: 123456

Cliente:

Email: cliente@teste.com  
Senha: 123456

## Regras de negócio

- RN01: Validação de CPF/CNPJ
- RN02: Validação de placa no padrão antigo ou Mercosul
- RN03: Valor total calculado automaticamente
- RN04: Não é permitido pular ou voltar status
- RN05: Toda OS inicia com status Recebida
- RN06: Ao ir para Aguardando Aprovação, o estoque é reservado
- RN07: Ao ir para Em Execução, o estoque é baixado

## Mensagens de erro

```txt
ERR_001 - CPF/CNPJ inválido
ERR_002 - Placa inválida
ERR_003 - Sem estoque
ERR_004 - Status inválido
ERR_005 - Não autorizado
Principais endpoints
Auth:

POST /api/auth/login
Clientes:

GET    /api/clientes
GET    /api/clientes/{id}
POST   /api/clientes
PUT    /api/clientes/{id}
DELETE /api/clientes/{id}
Veículos:

GET    /api/veiculos
GET    /api/veiculos?clienteId={id}
GET    /api/veiculos/{id}
POST   /api/veiculos
PUT    /api/veiculos/{id}
DELETE /api/veiculos/{id}
Itens:

GET    /api/itens
GET    /api/itens/{id}
POST   /api/itens
PUT    /api/itens/{id}
DELETE /api/itens/{id}
Ordens de Serviço:

GET  /api/ordens-servico
GET  /api/ordens-servico/{id}
POST /api/ordens-servico
POST /api/ordens-servico/{id}/itens
PUT  /api/ordens-servico/{id}/status
GET  /api/ordens-servico/resumo
Como rodar localmente
Restaure as dependências:

dotnet restore
Compile:

dotnet build Oficina.API.csproj
Execute:

dotnet run --project Oficina.API.csproj
Acesse o Swagger na URL exibida no terminal.

Banco de dados
O projeto utiliza SQL Server em produção, com banco hospedado no Azure SQL Database.

A connection string deve ser configurada por variável de ambiente:

ConnectionStrings__DefaultConnection=Server=tcp:SERVIDOR.database.windows.net,1433;Initial Catalog=NOME_BANCO;Persist Security Info=False;User ID=USUARIO;Password=SENHA;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
Variáveis de ambiente
ASPNETCORE_ENVIRONMENT=Production
Jwt__Key=minha-chave-super-secreta-com-mais-de-32-caracteres
Jwt__Issuer=OficinaAPI
Jwt__Audience=OficinaAPIUsers
ConnectionStrings__DefaultConnection=SUA_CONNECTION_STRING
Deploy
A API está publicada no Render via Docker.

Fluxo de deploy:

git add .
git commit -m "Mensagem do commit"
git push origin main
Depois, no Render:

Manual Deploy > Deploy latest commit
Observações
Esta API foi desenvolvida conforme a especificação técnica e funcional do projeto de conclusão de treinamento, contemplando autenticação, autorização, regras de negócio, controle de estoque, fluxo de status da OS, documentação Swagger e integração com front-end Angular.
