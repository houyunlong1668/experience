# experience

CICD 实验：简易 .NET Web API + GitHub Actions CI/CD + **Docker 镜像 + 阿里云 ACR/ECS** 部署。

> 推送到 `main` 后：GitHub Actions 构建应用与 Docker 镜像 → 推送到**阿里云容器镜像服务（ACR 杭州）** →
> SSH 到你的 ECS 从国内 ACR 拉取镜像并以容器运行 → 通过 `http://<公网IP>:8080` 访问。
> 除首次外，每次部署只传输几 MB 的应用层（基础镜像层已缓存），不再有大文件跨境传输。

## 接口列表

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/` | 服务信息 |
| GET | `/api/hello?name=xxx` | 问候接口，返回 JSON |
| GET | `/api/time` | 服务器时间（验证线上是动态进程） |
| GET | `/health` | 健康检查（部署后自动探测） |
| GET | `/weatherforecast` | 模板自带示例 |

## 本地运行

```bash
dotnet run
# 默认 http://localhost:5131
curl "http://localhost:5131/api/hello?name=dev"
```

## 本地打镜像（可选）

```bash
dotnet publish Experience.Api.csproj -c Release -o publish
docker build -t experience-api:local .
docker run --rm -p 8080:8080 experience-api:local
curl "http://localhost:8080/api/hello?name=docker"
```

## CI/CD 流程

[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml)：

1. **build**：restore → build → publish → `docker build`（打 `sha` 和 `latest` 双标签）
   → push 到 ACR（仅 main 的 push，PR 只构建验证）
2. **deploy**（仅 main）：SSH 到 ECS → 确保 Docker 已安装（缺失自动装，阿里源）
   → `docker login` ACR → `docker pull` 本次 sha 镜像 → `docker run`
   （`--restart=always` 崩溃/重启自动拉起；顺带停用旧 systemd 部署）
   → 从 runner curl `/health` 验证

任一配置缺失时打印具体缺什么并跳过，构建不红。

## 一次性配置

### 1. 阿里云 ACR（容器镜像服务）

打开 [容器镜像服务控制台](https://cr.console.aliyun.com) → 开通**个人版**（免费）：

- **命名空间**：查看或创建一个（记下来，比如 `houyunlong`）
- **访问凭证 → 设置登录密码**：这是**镜像仓库密码**（不是阿里云账号密码，也没设过就设一个）
- 仓库无需手动创建，推送时自动创建

把两样东西配成**仓库级** secret：
仓库 → **Settings → Secrets and variables → 第一个 `Actions` 标签 → `Repository secrets` 区域**
（注意：这次不是 Environments，是主页面的 Repository secrets）→ New repository secret：

| 名称 | 值 |
|------|-----|
| `ACR_USERNAME` | `houyunlong` |
| `ACR_PASSWORD` | 刚设置的镜像仓库登录密码 |

然后把命名空间填到 [.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) 顶部的
`ACR_NAMESPACE: REPLACE_ME_NAMESPACE`。

### 2. SSH 密钥（deploy 环境，已配过）

**Settings → Environments → deploy** 下的三个 secret：`HOST`（公网 IP）、`SSH_USER`、`SSH_PRIVATE_KEY`。

### 3. 安全组放行 8080

ECS 控制台 → 安全组 → 入方向：TCP 8080，源 `0.0.0.0/0`（22 端口保持默认放行）。

### 4. 触发

配置齐后向 `main` 推送，或 Actions 页 **Run workflow**。

## 访问线上接口

```text
http://<公网IP>:8080/
http://<公网IP>:8080/api/hello?name=github
http://<公网IP>:8080/api/time
http://<公网IP>:8080/health
```

## 服务器上的运维命令

```bash
sudo docker ps                              # 查看容器
sudo docker logs -f experience-api          # 实时日志
sudo docker restart experience-api          # 手动重启
sudo docker rm -f experience-api            # 停删容器（下次部署会重建）
```

## 说明

- 镜像 = `registry.cn-hangzhou.aliyuncs.com/<命名空间>/experience-api`，标签为提交 sha 和 `latest`
- 镜像为框架依赖发布 + 官方 `aspnet:10.0` 运行时，服务器无需安装 .NET
- 纯 HTTP + IP 直连；要域名/HTTPS 可在 ECS 上加 Nginx 反代
