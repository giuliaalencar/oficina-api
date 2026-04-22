# 🔄 Reset Completo do Banco de Dados

## 🎯 Objetivo
Recriar o banco de dados do zero com o schema correto (Guid em vez de int).

## 📋 Passo a Passo

### 1️⃣ Deletar o Banco de Dados

**Opção A - Via SQL Server Object Explorer (Visual Studio):**
1. Abra **View** → **SQL Server Object Explorer**
2. Expanda **SQL Server** → **(localdb)\MSSQLLocalDB** → **Databases**
3. Clique com botão direito no banco **Oficina**
4. Selecione **Delete**
5. Marque ✅ **Close existing connections**
6. Clique **OK**

**Opção B - Via Query SQL:**
```sql
USE master;
GO

-- Fechar todas as conexões
ALTER DATABASE [Oficina] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

-- Deletar banco
DROP DATABASE [Oficina];
GO
```

### 2️⃣ Remover Todas as Migrations

No **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console):

```powershell
# Remover todas as migrations antigas
Remove-Item -Path "Migrations" -Recurse -Force
```

**OU** no **Terminal Integrado**:
```bash
rm -r Oficina.API/Migrations
```

### 3️⃣ Criar Migration Inicial

No **Package Manager Console**:
```powershell
Add-Migration InitialCreate
```

**OU** no **Terminal**:
```bash
dotnet ef migrations add InitialCreate --project Oficina.API
```

### 4️⃣ Aplicar ao Banco

No **Package Manager Console**:
```powershell
Update-Database
```

**OU** no **Terminal**:
```bash
dotnet ef database update --project Oficina.API
```

## ✅ Resultado Esperado

Após esses passos:
- ✅ Banco de dados recriado do zero
- ✅ Tabela `Veiculos` com coluna `Id` como `UNIQUEIDENTIFIER` (Guid)
- ✅ Tabela `OrdensServico` com coluna `VeiculoId` como `UNIQUEIDENTIFIER` (Guid)
- ✅ Todas as Foreign Keys e índices corretos
- ✅ **Erro "Operand type clash" resolvido!**

## 🧪 Testar a Correção

1. **Criar um veículo:**
```http
POST /api/veiculos
Content-Type: application/json

{
  "clienteId": "00000000-0000-0000-0000-000000000000",
  "placa": "ABC-1234",
  "marca": "Ford",
  "modelo": "Fiesta",
  "ano": 2020
}
```

2. **Copiar o GUID retornado** (exemplo: `550e8400-e29b-41d4-a716-446655440000`)

3. **Criar ordem de serviço:**
```http
POST /api/ordens-servico
Content-Type: application/json

{
  "veiculoId": "550e8400-e29b-41d4-a716-446655440000"
}
```

4. ✅ **Deve funcionar sem erros!**

## 🔍 Verificar Schema no Banco

Execute no SQL Server:
```sql
-- Verificar tipo da coluna Id na tabela Veiculos
SELECT 
    TABLE_NAME,
    COLUMN_NAME, 
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME IN ('Veiculos', 'OrdensServico')
  AND COLUMN_NAME IN ('Id', 'VeiculoId')
ORDER BY TABLE_NAME, COLUMN_NAME;
```

**Resultado esperado:**
```
TABLE_NAME       | COLUMN_NAME  | DATA_TYPE        | IS_NULLABLE
OrdensServico    | VeiculoId    | uniqueidentifier | NO
Veiculos         | Id           | uniqueidentifier | NO
```

## 📝 Observações

- ⚠️ **Todos os dados serão perdidos** - Use apenas em desenvolvimento
- ✅ Esta é a solução **mais limpa** para o problema
- ✅ Evita complexidade de migrations de conversão de tipos
- ✅ O banco ficará perfeitamente alinhado com os modelos C#

## 🎉 Pronto!

Agora seu banco está sincronizado e você não terá mais o erro de incompatibilidade de tipos.
