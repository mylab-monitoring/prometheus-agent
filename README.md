# MyLab.PrometheusAgent
[![Docker image](https://img.shields.io/docker/v/mylabtools/prometheus-agent?label=Docker%20image)](https://hub.docker.com/r/mylabtools/prometheus-agent)

Ознакомьтесь с последними изменениями в [журнале изменений](/changelog.md).

## Обзор 

Собирает, аггрегирует и предоставляет метрики других сервисов в формате [Prometheus](https://prometheus.io/docs/concepts/data_model/). 

Метрики собираются в момент запроса параллельно со всех целевых сервисов.

## Конфигурация

### ScrapeConfig

Конфигурация сборки метрик. Путь к файлу с настройками задаётся переменной окружения `PROMETHEUS_AGENT__CONFIG`.

Формат файла соответствует урезанной спецификации конфигурационного файла Prometheus в части [scrape_config](https://prometheus.io/docs/prometheus/latest/configuration/configuration/#scrape_config).

Поддерживаемые узлы:

| Путь                                      | Описание                                                     |
| ----------------------------------------- | ------------------------------------------------------------ |
| scrape_configs                            | Секции, описывающие работы по сборке метрик: подключения и параметры |
| scrape_configs[]/job_name                 | Наименование работы, ассоциированное со сборкой метрик       |
| scrape_configs[]/static_configs           | Указывает список подключений и меток                         |
| scrape_configs[]/static_configs[]/targets | Целевые подключения (хост+порт)                              |
| scrape_configs[]/static_configs[]/labels  | Метки, которые будут добавляться ко всем метрикам, полученным из указанных в работе целевых подключений |

Пример конфигурационного файла:

```yaml
scrape_configs:

- job_name: job1
  static_configs:
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 1
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 2
      
- job_name: job2
  static_configs:
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 1

```

### Фиксированные настройки

* таймаут сбора всех метрик - 20 сек
* таймаут сбора метрик с одного подключения - 15 сек
* путь сборки метрик - `/metrics`

## Развёртывание

Сервис адаптирован для развёртывания на `docker`. При этом потребуется:

* подклчить конфиг сбора метрик;
* указать путь к нему через переменную окружения `PROMETHEUS_AGENT__CONFIG`.

Пример docker-compose файла:

```yaml
version: '3.2'

services:
  prometheus-agent:
    image: mylabtools/prometheus-agent
	container_name: prometheus-agent
    volumes:
    - ./scrape_config.yml:/scrape_config.yml
    environment:
    - PROMETHEUS_AGENT__CONFIG=/scrape_config.yml
    
```

