# experience

CICD 实验：简易 .NET Web API + GitHub Actions CI/CD + Azure App Service 部署。

> **关于域名**：GitHub 自带的 `*.github.io`（Pages）只能托管静态文件，无法常驻运行 .NET 进程。
> 因此本项目用 **GitHub Actions 做 CI/CD**，把应用部署到 **Azure App Service**（免费 F1 层），
> 通过 Azure 提供的公网域名访问：<https://<应用名>.azurewebsites.net>

## 接口列表

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/` | 服务信息 |
| GET | `/api/hello?name=xxx` | 问候接口，返回 JSON |
| GET | `/api/time` | 服务器时间（验证线上是动态进程） |
| GET | `/health` | 健康检查 |
| GET | `/weatherforecast` | 模板自带示例 |

## 本地运行

```bash
dotnet run
# 默认 http://localhost:5131
curl "http://localhost:5131/api/hello?name=dev"
```

## CI/CD 流程

推送到 `main` 分支后，GitHub Actions（[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml)）自动执行：

1. **build**：restore → build → publish，产物上传为 artifact（PR 也会执行，用于验证构建）
2. **deploy**：下载产物 → `azure/login` 登录 → `azure/webapps-deploy` 部署到 Azure App Service
   （未配置密钥时会自动跳过部署并给出警告，构建仍为绿色）

## Azure 侧准备（一次性）

### 1. 创建 Web App

在 [Azure 门户](https://portal.azure.com) 创建资源 → Web App：

- **发布**：Code
- **运行时**：.NET 10（Linux，区域选离你近的，如 East Asia）
- 定价层选 **免费 F1** 即可（实验够用）

或用 Azure CLI：

```bash
az login
SUB=$(az account show --query id -o tsv)
RG=rg-experience
APP=experience-api-你的唯一后缀   # 应用名全 Azure 唯一
LOC=eastasia

az group create --name $RG --location $LOC
az appservice plan create --name plan-$APP --resource-group $RG --sku F1 --is-linux
az webapp create --name $APP --resource-group $RG --plan plan-$APP --runtime "DOTNET|10.0"
# 若提示 DOTNET|10.0 不可用，用 az webapp list-runtimes --linux 查最新写法
```

### 2. 创建部署用的服务主体（Service Principal）

```bash
az ad sp create-for-rbac \
  --name "ghactions-$APP" \
  --role contributor \
  --scopes /subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.Web/sites/$APP \
  --sdk-auth
```

命令会输出一段 JSON。如果报错说 `--sdk-auth` 已移除，先执行不带该参数的同样命令，
拿到 `appId` / `password` / `tenant` 后手动拼成：

```json
{
  "clientId": "<appId>",
  "clientSecret": "<password>",
  "subscriptionId": "<SUB>",
  "tenantId": "<tenant>",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com",
  "activeDirectoryGraphApiVersion": "2018-02-01",
  "sqlServerEndpointUrl": "https://database.windows.net",
  "managementEndpointUrl": "https://management.azure.com"
}
```

### 3. 配置 GitHub Secrets

仓库 → **Settings → Secrets and variables → Actions → New repository secret**：

- 名称：`AZURE_CREDENTIALS`
- 值：上一步的整段 JSON

### 4. 改工作流里的应用名

把 [.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) 顶部 `AZURE_WEBAPP_NAME` 改成你的 Web App 名称，然后推送到 `main`。

## 访问线上接口

部署成功后：

```text
https://<应用名>.azurewebsites.net/
https://<应用名>.azurewebsites.net/api/hello?name=github
https://<应用名>.azurewebsites.net/api/time
https://<应用名>.azurewebsites.net/health
```
