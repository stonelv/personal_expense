# Personal Expense - 个人记账/预算系统

一个基于 .NET 10 WebAPI + SQLite 的轻量级个人记账和预算管理系统。

## 技术栈

- **框架**: ASP.NET Core WebAPI (.NET 10)
- **数据库**: SQLite
- **ORM**: Entity Framework Core
- **身份认证**: ASP.NET Core Identity + JWT
- **文档**: Swagger/OpenAPI

## 项目结构

```
PersonalExpense/
├── PersonalExpense.API/          # API层
│   ├── Controllers/              # API控制器
│   │   ├── AuthController.cs     # 认证接口
│   │   ├── AccountsController.cs # 账户管理
│   │   ├── CategoriesController.cs # 分类管理
│   │   ├── TransactionsController.cs # 交易记录
│   │   └── BudgetsController.cs  # 预算管理
│   ├── Program.cs                # 应用入口
│   └── appsettings.json          # 配置文件
├── PersonalExpense.Domain/       # 领域层
│   └── Entities/                 # 领域模型
│       ├── User.cs               # 用户实体
│       ├── Account.cs            # 账户实体
│       ├── Category.cs           # 分类实体
│       ├── Transaction.cs        # 交易记录实体
│       └── Budget.cs             # 预算实体
└── PersonalExpense.Infrastructure/ # 基础设施层
    └── Data/
        ├── ApplicationDbContext.cs # 数据库上下文
        └── DbInitializer.cs      # 数据库初始化器
```

## 功能特性

### 1. 身份认证
- 用户注册
- 用户登录（JWT Token）
- 基于角色的访问控制

### 2. 账户管理
- 支持多种账户类型：现金、银行卡、信用卡、投资、其他
- 账户CRUD操作
- 账户余额自动更新

### 3. 分类管理
- 收入/支出分类
- 支持图标和描述
- 分类CRUD操作

### 4. 记账流水
- 支持三种交易类型：收入、支出、转账
- 支持附件URL和备注
- 交易记录CRUD操作
- 按年月筛选查询

### 5. 预算管理
- 月度预算设置（按总额或分类）
- 预算超支提示
- 预算状态查询
- 预算CRUD操作

### 6. 数据隔离
- 多用户支持
- 用户只能访问自己的数据

## 快速开始

### 前置要求

- .NET 10 SDK
- SQLite

### 安装步骤

1. 克隆仓库
```bash
git clone <repository-url>
cd personal_expense_2
```

2. 恢复依赖
```bash
dotnet restore
```

3. 更新数据库
```bash
cd PersonalExpense.API
dotnet ef database update
```

4. 运行应用
```bash
dotnet run
```

5. 访问 Swagger UI
```
https://localhost:5001/swagger
```

## API 文档

### 认证接口

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | `/api/auth/register` | 用户注册 |
| POST | `/api/auth/login` | 用户登录 |

### 账户接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/accounts` | 获取所有账户 |
| GET | `/api/accounts/{id}` | 获取单个账户 |
| POST | `/api/accounts` | 创建账户 |
| PUT | `/api/accounts/{id}` | 更新账户 |
| DELETE | `/api/accounts/{id}` | 删除账户 |

### 分类接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/categories` | 获取所有分类 |
| GET | `/api/categories/{id}` | 获取单个分类 |
| POST | `/api/categories` | 创建分类 |
| PUT | `/api/categories/{id}` | 更新分类 |
| DELETE | `/api/categories/{id}` | 删除分类 |

### 交易记录接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/transactions` | 获取交易记录（支持筛选） |
| GET | `/api/transactions/{id}` | 获取单个交易记录 |
| POST | `/api/transactions` | 创建交易记录 |
| PUT | `/api/transactions/{id}` | 更新交易记录 |
| DELETE | `/api/transactions/{id}` | 删除交易记录 |

### 预算接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/budgets` | 获取预算 |
| GET | `/api/budgets/{id}` | 获取单个预算 |
| GET | `/api/budgets/status` | 获取预算状态（含超支提示） |
| POST | `/api/budgets` | 创建预算 |
| PUT | `/api/budgets/{id}` | 更新预算 |
| DELETE | `/api/budgets/{id}` | 删除预算 |

## 数据模型

### 用户 (User)
- Id: Guid
- UserName: string
- Email: string
- CreatedAt: DateTime
- UpdatedAt: DateTime?

### 账户 (Account)
- Id: Guid
- Name: string
- Type: AccountType (Cash=1, BankCard=2, CreditCard=3, Investment=4, Other=5)
- Balance: decimal
- Description: string?
- IsActive: bool
- CreatedAt: DateTime
- UpdatedAt: DateTime?
- UserId: Guid

### 分类 (Category)
- Id: Guid
- Name: string
- Type: CategoryType (Income=1, Expense=2)
- Icon: string?
- Description: string?
- IsActive: bool
- CreatedAt: DateTime
- UpdatedAt: DateTime?
- UserId: Guid

### 交易记录 (Transaction)
- Id: Guid
- Type: TransactionType (Income=1, Expense=2, Transfer=3)
- Amount: decimal
- TransactionDate: DateTime
- Description: string?
- AttachmentUrl: string?
- CreatedAt: DateTime
- UpdatedAt: DateTime?
- UserId: Guid
- AccountId: Guid
- CategoryId: Guid?
- TransferToAccountId: Guid?

### 预算 (Budget)
- Id: Guid
- Type: BudgetType (Total=1, ByCategory=2)
- Amount: decimal
- Year: int
- Month: int
- Description: string?
- CreatedAt: DateTime
- UpdatedAt: DateTime?
- UserId: Guid
- CategoryId: Guid?

## 配置说明

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=PersonalExpense.db"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyForJWTTokenMustBeAtLeast32Characters",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001"
  }
}
```

## 开发指南

### 添加迁移

```bash
dotnet ef migrations add <MigrationName> -p ../PersonalExpense.Infrastructure/PersonalExpense.Infrastructure.csproj
```

### 应用迁移

```bash
dotnet ef database update
```

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
