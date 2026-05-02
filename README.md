<div align="center">

# 🤖 QA Assistant

**AI-powered корпоративный помощник для IT-поддержки**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-REST_API-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/aspnet/core/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Render](https://img.shields.io/badge/Deploy-Render-46E3B7?style=for-the-badge&logo=render&logoColor=white)](https://render.com/)

<br/>

> Умный ассистент для ответов на корпоративные вопросы: политики компании, IT-поддержка, онбординг сотрудников — всё в одном API.

<br/>

![Architecture](https://img.shields.io/badge/Architecture-REST_API-orange?style=flat-square)
![Auth](https://img.shields.io/badge/Auth-JWT-green?style=flat-square)
![AI](https://img.shields.io/badge/AI-OpenRouter-blueviolet?style=flat-square)
![EF Core](https://img.shields.io/badge/ORM-EF_Core-blue?style=flat-square)

</div>

---

## 📋 Содержание

- [О проекте](#-о-проекте)
- [Архитектура](#-архитектура)
- [Технологии](#-технологии)
- [Возможности](#-возможности)
- [Быстрый старт](#-быстрый-старт)
- [Конфигурация](#-конфигурация)
- [API Reference](#-api-reference)
- [Деплой](#-деплой)
- [Команда](#-команда)

---

## 💡 О проекте

**QA Assistant** — это REST API бэкенд, который превращает корпоративную базу знаний в умный чат-ассистент. Сотрудник задаёт вопрос на естественном языке, система находит релевантный раздел из базы знаний и формирует ответ с помощью LLM.

**Это студенческий проект**, разработанный командой из 5 человек в рамках учебного курса. Проект прошёл путь от консольного прототипа до полноценного production-ready HTTP API.

### Как это работает

```
Вопрос пользователя
        │
        ▼
┌───────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Валидация    │────▶│  Поиск по базе   │────▶│   AI-генерация  │
│  запроса      │     │  знаний (TF-IDF) │     │  ответа (LLM)   │
└───────────────┘     └──────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
                                              Структурированный
                                              текстовый ответ
```

---

## 🏗 Архитектура

```
QA-Assistant/
├── QA.Backend/
│   ├── Controllers/          # HTTP-контроллеры (6 шт.)
│   │   ├── QuestionsController   # Основной Q&A endpoint
│   │   ├── AuthController        # Регистрация / JWT-логин
│   │   ├── ConversationsController  # История чатов
│   │   ├── AiSettingsController  # Настройки AI-модели
│   │   ├── KnowledgeController   # Управление базой знаний
│   │   └── HealthController      # Health check
│   ├── Services/             # Бизнес-логика (9 сервисов)
│   │   ├── QaService             # Главный оркестратор
│   │   ├── KnowledgeBaseService  # Загрузка и чанкинг KB
│   │   ├── SearchService         # Поиск по ключевым словам
│   │   ├── OpenAiService         # Интеграция с OpenRouter
│   │   └── AuraModelService      # Кастомная AI-модель
│   ├── Data/                 # EF Core + MySQL
│   ├── Models/               # DTO запросов/ответов
│   ├── Options/              # Типизированная конфигурация
│   └── Migrations/           # Миграции базы данных
├── knowledge_base.txt        # База знаний в формате Markdown
├── Dockerfile                # Multi-stage Docker build
└── render.yaml               # Конфигурация деплоя
```

---

## 🛠 Технологии

| Категория | Стек |
|-----------|------|
| **Runtime** | .NET 8.0, C# |
| **Framework** | ASP.NET Core (REST API) |
| **База данных** | MySQL 8.0 + Entity Framework Core 8 |
| **Аутентификация** | JWT Bearer (HS256) |
| **AI-провайдер** | OpenRouter API (GPT, бесплатный тир) |
| **Документация API** | Swagger / Swashbuckle |
| **Контейнеризация** | Docker (multi-stage build) |
| **Деплой** | Render.com (free tier) |
| **CI/CD** | GitHub Actions |

---

## ✨ Возможности

### 🔍 Умный Q&A
- Ответы на вопросы на естественном языке
- Keyword-based поиск по базе знаний с релевантным скорингом
- Генерация ответов через LLM (OpenRouter / кастомная Aura-модель)
- Ответы в чистом тексте — без лишней разметки

### 📚 База знаний
- Загрузка из текстового файла при старте
- Разбивка на чанки (настраиваемый размер)
- Горячая перезагрузка без рестарта сервиса (`/api/knowledge/reload`)
- Мониторинг состояния (`/api/knowledge/status`)

### 👤 Аутентификация
- Регистрация по email + пароль (bcrypt-хеширование)
- JWT-токены с настраиваемым TTL (по умолчанию 24 часа)
- Защищённые endpoint'ы через `[Authorize]`

### 💬 История разговоров
- Создание и хранение диалогов в БД
- Постраничная история сообщений
- Привязка к конкретному пользователю

### ⚙️ Персонализация AI
- Индивидуальные настройки AI-модели на пользователя
- Кастомный system prompt
- Переключение между провайдерами (OpenRouter / Aura)

---

## 🚀 Быстрый старт

### Требования

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- [MySQL 8.0](https://dev.mysql.com/downloads/)
- API-ключ [OpenRouter](https://openrouter.ai/) (бесплатный тир доступен)

### Локальный запуск

```bash
# 1. Клонировать репозиторий
git clone https://github.com/dmitriidrugov/qa-assistant.git
cd qa-assistant

# 2. Восстановить зависимости
dotnet restore

# 3. Задать переменные окружения
export ASPNETCORE_ENVIRONMENT=Development
export AI__APIKEY="sk-or-xxxxxxxx"          # OpenRouter API key
export DATABASE__CONNECTIONSTRING="Server=localhost;Database=qa_db;User=root;Password=yourpassword;"

# 4. Применить миграции базы данных
dotnet ef database update --project QA.Backend

# 5. Запустить
dotnet run --project QA.Backend
```

После запуска:
- **API** → `https://localhost:7172`
- **Swagger UI** → `https://localhost:7172/swagger`
- **Health check** → `https://localhost:7172/api/health`

### Docker

```bash
# Собрать образ
docker build -t qa-assistant .

# Запустить контейнер
docker run -p 10000:10000 \
  -e AI__APIKEY="sk-or-xxxxxxxx" \
  -e DATABASE__CONNECTIONSTRING="Server=host.docker.internal;..." \
  qa-assistant
```

---

## ⚙️ Конфигурация

Настройки хранятся в `appsettings.json`. Секреты передаются через переменные окружения (переопределяют конфиг).

| Переменная окружения | Описание | Обязательно |
|---------------------|----------|:-----------:|
| `AI__APIKEY` | API-ключ OpenRouter | ✅ |
| `DATABASE__CONNECTIONSTRING` | Строка подключения к MySQL | ✅ |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` | — |
| `JWT__KEY` | Секретный ключ подписи токенов | — |

<details>
<summary>Полный пример appsettings.json</summary>

```json
{
  "KnowledgeBase": {
    "FilePath": "knowledge_base.txt",
    "ChunkSize": 800,
    "MaxQuestionLength": 500
  },
  "Ai": {
    "Provider": "OpenRouter",
    "BaseUrl": "https://openrouter.ai/api",
    "Model": "openai/gpt-oss-20b:free",
    "ApiKey": "",
    "TimeoutSeconds": 60
  },
  "Database": {
    "ConnectionString": ""
  },
  "Jwt": {
    "Issuer": "QA.Backend",
    "Audience": "AURA.Frontend",
    "AccessTokenExpiresHours": 24
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  }
}
```

</details>

---

## 📖 API Reference

### Основные endpoint'ы

| Метод | Путь | Описание | Auth |
|-------|------|----------|:----:|
| `POST` | `/api/questions/ask` | Задать вопрос ассистенту | — |
| `POST` | `/auth/register` | Регистрация нового пользователя | — |
| `POST` | `/auth/login` | Получить JWT-токен | — |
| `GET` | `/auth/me` | Информация о текущем пользователе | 🔒 |
| `GET` | `/api/knowledge/status` | Статус базы знаний | — |
| `POST` | `/api/knowledge/reload` | Перезагрузить базу знаний | — |
| `GET` | `/api/conversations/{id}` | Получить историю диалога | 🔒 |
| `POST` | `/api/conversations/{id}/messages` | Добавить сообщение | 🔒 |
| `GET/PUT` | `/api/ai-settings` | Настройки AI-модели | 🔒 |
| `GET` | `/api/health` | Health check | — |

### Пример запроса

```bash
curl -X POST https://your-app.onrender.com/api/questions/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "Как сбросить пароль от корпоративной почты?"}'
```

```json
{
  "success": true,
  "question": "Как сбросить пароль от корпоративной почты?",
  "answer": "Для сброса пароля зайдите на портал HR по адресу hr.company.com, выберите раздел «Безопасность» и нажмите «Сбросить пароль». Вам придёт письмо со ссылкой.",
  "matchedChunk": "...",
  "source": "knowledge_base"
}
```

> Полная документация доступна в Swagger UI по пути `/swagger` после запуска.

---

## 🌐 Деплой

Проект настроен для деплоя на [Render.com](https://render.com/) (free tier).

```yaml
# render.yaml
services:
  - type: web
    runtime: docker
    region: frankfurt
    healthCheckPath: /api/health
```

**Шаги:**
1. Форкнуть репозиторий
2. Создать сервис на Render, указать репозиторий
3. Добавить переменные окружения: `AI__APIKEY`, `DATABASE__CONNECTIONSTRING`
4. Render автоматически соберёт Docker-образ и задеплоит

---

## 👥 Команда

Проект разработан студенческой командой из 5 человек:

<table>
  <tr>
    <td align="center">
      <b>Участник 1</b><br/>
      <sub>Backend, Архитектура</sub>
    </td>
    <td align="center">
      <b>Участник 2</b><br/>
      <sub>AI-интеграция, Сервисы</sub>
    </td>
    <td align="center">
      <b>Участник 3</b><br/>
      <sub>База данных, EF Core</sub>
    </td>
    <td align="center">
      <b>Участник 4</b><br/>
      <sub>Аутентификация, JWT</sub>
    </td>
    <td align="center">
      <b>Участник 5</b><br/>
      <sub>DevOps, Docker, CI/CD</sub>
    </td>
  </tr>
</table>

---

## 📄 Лицензия

Этот проект создан в образовательных целях. MIT License.

---

<div align="center">
  <sub>Сделано с ❤️ студенческой командой · 2026</sub>
</div>
