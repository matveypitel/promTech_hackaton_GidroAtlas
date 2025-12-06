# Docker Configuration для GidroAtlas

Эта папка содержит все необходимые файлы для развертывания приложения GidroAtlas с помощью Docker.

## 📁 Структура

- `docker-compose.yml` - Оркестрация всех сервисов
- `.dockerignore` - Исключения для Docker build context

## 🚀 Быстрый старт

### Запуск всех сервисов

```bash
# Из корневой папки проекта
cd docker
docker-compose up --build

# Или в фоновом режиме
docker-compose up -d --build
```

### Остановка сервисов

```bash
# Остановить контейнеры
docker-compose down

# Остановить и удалить volumes (очистить БД)
docker-compose down -v
```

## 🌐 Доступ к приложениям

После запуска сервисы будут доступны по следующим адресам:

| Сервис | URL | Описание |
|--------|-----|----------|
| **Web приложение** | http://localhost:5000 | Blazor интерфейс |
| **API** | http://localhost:5001 | REST API |
| **Swagger** | http://localhost:5001/swagger | API документация |
| **PostgreSQL** | localhost:5432 | База данных с pgvector |
| **Ollama** | http://localhost:11434 | LLM API |

## 🗄️ База данных

**Параметры подключения:**
- Host: `localhost`
- Port: `5432`
- Database: `gidroatlas_db`
- Username: `postgres`
- Password: `postgres`

**Расширения:**
- `pgvector` - для векторного поиска (RAG)

## 📦 Сервисы

### 1. PostgreSQL + pgvector (postgres)
- Образ: `pgvector/pgvector:pg16`
- Порт: `5432`
- Volume: `postgres_data` для персистентности данных
- Health check: автоматическая проверка готовности
- Расширение pgvector для векторного поиска

### 2. Ollama (ollama)
- Образ: `ollama/ollama:latest`
- Порт: `11434`
- Volume: `ollama_data` для хранения моделей
- Модели: 
  - `qwen3:4b` - для генерации ответов
  - `nomic-embed-text` - для эмбеддингов

### 3. Ollama Setup (ollama-setup)
- Автоматически загружает необходимые модели при первом запуске

### 4. API (api)
- Порт: `5001`
- Зависит от: `postgres`, `ollama`
- Автоматическая миграция БД при старте

### 5. Web (web)
- Порт: `5000`
- Зависит от: `api`
- Blazor Server приложение

## 🤖 RAG Чатбот

После первого запуска необходимо проиндексировать данные:

```bash
# Авторизоваться как эксперт и вызвать endpoint индексации
curl -X POST http://localhost:5001/api/chat/index \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Или через Swagger UI → POST /api/chat/index

## 🔧 Полезные команды

```bash
# Просмотр логов
docker-compose logs -f

# Просмотр логов конкретного сервиса
docker-compose logs -f api
docker-compose logs -f web
docker-compose logs -f postgres
docker-compose logs -f ollama

# Перезапуск конкретного сервиса
docker-compose restart api

# Пересборка без кэша
docker-compose build --no-cache

# Просмотр запущенных контейнеров
docker-compose ps

# Выполнение команды в контейнере
docker-compose exec api bash
docker-compose exec postgres psql -U postgres -d gidroatlas_db

# Проверить загруженные модели Ollama
docker-compose exec ollama ollama list

# Загрузить дополнительную модель
docker-compose exec ollama ollama pull MODEL_NAME
```

## 🐛 Troubleshooting

### Порты заняты
Если порты 5000, 5001, 5432 или 11434 уже используются, измените их в `docker-compose.yml`:
```yaml
ports:
  - "НОВЫЙ_ПОРТ:8080"  # для api и web
  - "НОВЫЙ_ПОРТ:5432"  # для postgres
  - "НОВЫЙ_ПОРТ:11434" # для ollama
```

### Проблемы с БД
```bash
# Полная очистка и пересоздание
docker-compose down -v
docker-compose up --build
```

### Модели Ollama не загружаются
```bash
# Ручная загрузка моделей
docker-compose exec ollama ollama pull qwen3:4b
docker-compose exec ollama ollama pull nomic-embed-text
```

### GPU поддержка для Ollama
Раскомментируйте секцию deploy в docker-compose.yml для ollama сервиса:
```yaml
deploy:
  resources:
    reservations:
      devices:
        - driver: nvidia
          count: all
          capabilities: [gpu]
```

### Просмотр логов ошибок
```bash
docker-compose logs --tail=100 api
docker-compose logs --tail=100 ollama
```

## 📊 Мониторинг

### Проверка статуса чата
```bash
curl http://localhost:5001/api/chat/status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Пример запроса к чату
```bash
curl -X POST http://localhost:5001/api/chat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"message": "Какие объекты в плохом состоянии?"}'
```
