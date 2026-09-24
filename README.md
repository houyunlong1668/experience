# experience

CICD 实验：简易 .NET Web API + GitHub Actions CI/CD + **阿里云 ECS** 部署。

> 推送到 `main` 后，GitHub Actions 自动构建并 SSH 部署到你的阿里云 ECS 公网服务器，
> 通过 `http://<公网IP>:8080` 访问。应用由 systemd 托管，崩溃自动重启。

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

## CI/CD 流程

推送到 `main` 分支后，[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) 自动执行：

1. **build**：restore → build → publish（自包含 linux-x64 单文件，服务器无需安装 .NET）→ 上传 artifact
   （PR 也执行构建，用于验证）
2. **deploy**（仅 main push）：下载产物 → SCP 上传到 ECS `/tmp` → 远程脚本替换
   `/opt/experience-api` → 注册/重启 systemd 服务 `experience-api` → 必要时放行 firewalld/ufw →
   从 runner 访问 `/health` 和 `/api/hello` 验证部署

未配置 SSH 密钥时，部署步骤自动跳过并给出警告，构建仍为绿色。

## 阿里云 ECS 侧准备（一次性）

### 1. 安全组放行 8080

ECS 控制台 → **安全组** → 配置规则 → 入方向添加：

- 协议：**TCP**，端口：**8080**，源：`0.0.0.0/0`

> SSH（22 端口）入方向需对 GitHub runner 可达（一般默认已放行 0.0.0.0/0）。

### 2. 准备 SSH 登录

用你平时登录 ECS 的账号（`root` 或有免密 `sudo` 的用户均可）。
没有密钥的话本地生成一对：

```bash
ssh-keygen -t ed25519 -f ~/.ssh/experience_deploy -C "github-actions"
# 把公钥装到服务器
ssh-copy-id -i ~/.ssh/experience_deploy.pub <用户>@<公网IP>
```

### 3. 配置 GitHub Secrets

仓库 → **Settings → Secrets and variables → Actions → New repository secret**，添加 4 个：

| 名称 | 值 |
|------|-----|
| `HOST` | ECS 公网 IP |
| `SSH_USER` | 登录用户名，如 `root` |
| `SSH_PORT` | SSH 端口，一般 `22`（可省略） |
| `SSH_PRIVATE_KEY` | 私钥**完整多行**内容（`~/.ssh/experience_deploy` 或你现有私钥，含 `-----BEGIN/END-----` 行） |

> 粘贴时保持原始多行格式，不要把换行替换成 `\n` 字面量。

### 4. 推送触发部署

配置好后向 `main` 推送任意提交（或在 Actions 页 **Run workflow** 手动触发）即可。

## 访问线上接口

部署成功后（Actions 日志末尾也会自动 curl 验证）：

```text
http://<公网IP>:8080/
http://<公网IP>:8080/api/hello?name=github
http://<公网IP>:8080/api/time
http://<公网IP>:8080/health
```

## 服务器上的运维命令

```bash
sudo systemctl status experience-api     # 查看状态
sudo systemctl restart experience-api    # 手动重启
journalctl -u experience-api -f          # 看实时日志
ls /opt/experience-api                   # 应用发布目录
```

## 说明

- 目前是纯 HTTP + IP 直连（实验够用）。如需域名 + HTTPS，可在 ECS 上加 Nginx 反代并配证书。
- 服务器无需安装 .NET SDK/运行时：发布产物为自包含单文件（约 97MB），每次部署全量上传。
